using System;
using System.Reflection;
using Project_Chronicles.Character;
using Project_Chronicles.Character.Combat;
using Project_Chronicles.Character.StateMachine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

// Menu or batch mode: -executeMethod CombatAnimationValidation.Run (without -quit).
[InitializeOnLoad]
public static class CombatAnimationValidation
{
    private const string PendingValidation = "Chronicles.StateMachineValidation.Pending";
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private static CharacterStateMachine _machine;
    private static CharacterAttackController _attacks;
    private static PlayerInputListener _input;
    private static Rigidbody2D _body;
    private static GameObject _character;
    private static Keyboard _keyboard;
    private static int _checks;

    static CombatAnimationValidation()
    {
        if (SessionState.GetBool(PendingValidation, false))
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    [MenuItem("Tools/Chronicles/Validate State Machine and Combat")]
    public static void Run()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Run validation from Edit Mode.");
        SessionState.SetBool(PendingValidation, true);
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.EnterPlaymode();
    }

    private static void OnPlayModeChanged(PlayModeStateChange change)
    {
        if (change != PlayModeStateChange.EnteredPlayMode) return;
        SessionState.SetBool(PendingValidation, false);
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        Scene scene = default;
        GameObject floor = null;
        var previousUpdateMode = InputSystem.settings.updateMode;
        var previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        var previousBackgroundBehavior = InputSystem.settings.backgroundBehavior;
        int exitCode = 0;
        try
        {
            _checks = 0;
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            _keyboard = InputSystem.AddDevice<Keyboard>();
            scene = SceneManager.CreateScene("StateMachineValidation", new CreateSceneParameters(LocalPhysicsMode.Physics2D));
            floor = new GameObject("Validation ground") { layer = 3 };
            SceneManager.MoveGameObjectToScene(floor, scene);
            floor.transform.position = new Vector3(1000f, -0.5f, 0f);
            floor.AddComponent<BoxCollider2D>().size = new Vector2(100f, 1f);
            _character = UnityEngine.Object.Instantiate(
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03 Prefabs/Theodore.prefab"));
            SceneManager.MoveGameObjectToScene(_character, scene);
            _machine = _character.GetComponent<CharacterStateMachine>();
            _attacks = _character.GetComponent<CharacterAttackController>();
            _input = _character.GetComponent<PlayerInputListener>();
            _body = _character.GetComponent<Rigidbody2D>();
            Ground();
            _machine.Tick(0f);
            Require(_machine.CurrentState == CharacterState.IDLE, "Initial state is not Idle");
            Require(_character.GetComponent<Project_Chronicles.Character.CharacterController>().GetState<IdleState>()
                == _machine.CurrentStateInstance, "Character and FSM do not share state instances");

            ValidateMovement();
            ValidateAirDashTransitions();
            ValidateCombos(false);
            ValidateCombos(true);
            ValidateCancelAndHitStun();
            ValidateAirAttack();
            ValidateAirAttackMovement();
            ValidateAttackDistance();
            ValidateDisable();
            Debug.Log($"STATE_MACHINE_VALIDATION_PASSED: {_checks} checks; movement, dash, jump/fall, ZZZZ/ZZZX, animation sampling, cancels, hit priority, landing, disable/re-enable.");
        }
        catch (Exception exception)
        {
            exitCode = 1;
            Debug.LogException(exception);
        }
        finally
        {
            if (_character != null) UnityEngine.Object.DestroyImmediate(_character);
            if (floor != null) UnityEngine.Object.DestroyImmediate(floor);
            if (_keyboard != null) InputSystem.RemoveDevice(_keyboard);
            InputSystem.settings.updateMode = previousUpdateMode;
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput;
            InputSystem.settings.backgroundBehavior = previousBackgroundBehavior;
            if (scene.IsValid()) SceneManager.UnloadSceneAsync(scene);
            if (Application.isBatchMode) EditorApplication.Exit(exitCode);
            else EditorApplication.ExitPlaymode();
        }
    }

    private static void ValidateMovement()
    {
        SetKeys(Key.RightArrow);
        _machine.Tick(0f);
        Require(_machine.CurrentState == CharacterState.WALK,
            $"Right input did not enter Walk: state={_machine.CurrentState}, input={_input.HorizontalInput}, grounded={_machine.IsGrounded}, velocity={_body.linearVelocity}, key={_keyboard.rightArrowKey.isPressed}");
        PhysicsTick();
        Require(Mathf.Approximately(_body.linearVelocity.x, _character.GetComponent<CharacterHorizontalMovement>().WalkSpeed), "Walk speed was not applied");
        SetKeys();
        _machine.Tick(0f);
        Require(_machine.CurrentState == CharacterState.IDLE, "Walk did not return to lower-priority Idle");
        PhysicsTick();
        Require(Mathf.Approximately(_body.linearVelocity.x, 0f), "Released movement did not stop");
        _input.CancelDash();
        SetKeys(Key.RightArrow);
        SetKeys();
        SetKeys(Key.RightArrow);
        _machine.Tick(0f);
        Require(_machine.CurrentState == CharacterState.RUN, "Double tap did not enter Dash/Run");
        PhysicsTick();
        Require(Mathf.Approximately(_body.linearVelocity.x, _character.GetComponent<CharacterHorizontalMovement>().RunSpeed), "Dash speed was not applied");
        SetKeys(Key.LeftArrow, Key.RightArrow);
        _machine.Tick(0f);
        Require(_machine.CurrentState == CharacterState.IDLE, "Opposing directions did not cancel dash");
        SetKeys();
        PhysicsTick();
        SetKeys(Key.UpArrow);
        PhysicsTick();
        Require(_body.linearVelocity.y > 0f && _machine.CurrentState == CharacterState.JUMP, "Jump was not applied and entered through the FSM");
        AssertAnimation("Base Layer.Theodore_Jump");
        _character.GetComponent<Animator>().Update(0.5f);
        AssertAnimation("Base Layer.Theodore_Jump");
        SetKeys();
        Ground();
        _machine.Tick(0f);
        PhysicsTick();
    }

    private static void ValidateAirDashTransitions()
    {
        var vertical = _character.GetComponent<CharacterVerticalMovement>();
        var horizontal = _character.GetComponent<CharacterHorizontalMovement>();
        var dashAttackField = typeof(CharacterAttackController).GetField("_dashJumpZ1", PrivateInstance);
        var originalDashAttack = (AttackData)dashAttackField.GetValue(_attacks);
        var dashAttack = UnityEngine.Object.Instantiate(
            AssetDatabase.LoadAssetAtPath<AttackData>("Assets/05 Data/Theodore_Jump_Z1.asset"));
        dashAttackField.SetValue(_attacks, dashAttack);
        try
        {
            foreach (float verticalSpeed in new[] { 2f, -2f })
            foreach (int direction in new[] { 1, -1 })
            {
                SetKeys();
                _input.CancelDash();
                Ground();
                _machine.Tick(0f);
                PhysicsTick();
                _character.transform.position += Vector3.up * 3f;
                Physics2D.SyncTransforms();
                _body.linearVelocity = new Vector2(0f, verticalSpeed);
                Key forward = direction > 0 ? Key.RightArrow : Key.LeftArrow;
                Key reverse = direction > 0 ? Key.LeftArrow : Key.RightArrow;
                SetKeys(forward);
                SetKeys();
                SetKeys(forward);
                // Physics must also update the context when it runs before Update.
                PhysicsTick();
                Require(vertical.WasDashJump, "Air dash did not promote jump/fall to dash jump");
                Require(_machine.CurrentState == (verticalSpeed > 0f ? CharacterState.DASH_JUMP : CharacterState.FALL),
                    "Air dash lost the rising/falling phase");
                Require(Mathf.Approximately(_body.linearVelocity.y, verticalSpeed), "Air dash reapplied a jump impulse");
                Require(Mathf.Approximately(_body.linearVelocity.x, direction * horizontal.RunSpeed), "Air dash did not apply run speed");
                _machine.RequestAttack(AttackInput.LIGHT);
                _machine.Tick(0f);
                Require(_attacks.CurrentAttack == dashAttack, "Air dash did not update the opening attack context");
                _machine.RequestHitStun(0.01f);
                _machine.Tick(0.02f);

                SetKeys(reverse);
                _machine.Tick(0f);
                PhysicsTick();
                Require(!vertical.WasDashJump, "Reverse input did not clear dash jump");
                Require(_machine.GetLocomotionState() == (verticalSpeed > 0f ? CharacterState.JUMP : CharacterState.FALL),
                    "Reverse input did not restore normal jump/fall");
                Require(Mathf.Approximately(_body.linearVelocity.x, -direction * horizontal.WalkSpeed), "Reverse input did not restore walk speed");
                Require(Mathf.Approximately(_body.linearVelocity.y, verticalSpeed), "Reversing air dash changed vertical velocity");
                _machine.RequestAttack(AttackInput.LIGHT);
                _machine.Tick(0f);
                Require(_attacks.CurrentAttack != dashAttack, "Reverse input retained the dash attack context");
                Ground();
                SetKeys();
                _machine.Tick(0f);
                PhysicsTick();
                Require(!vertical.WasDashJump, "Landing retained the air dash context");
            }
        }
        finally
        {
            dashAttackField.SetValue(_attacks, originalDashAttack);
            UnityEngine.Object.DestroyImmediate(dashAttack);
        }
    }

    private static void ValidateCombos(bool branchToX)
    {
        SetKeys();
        Ground();
        _machine.Tick(0f);
        SetKeys(Key.Z);
        _machine.Tick(0f);
        SetKeys();
        Require(_machine.CurrentState == CharacterState.ATTACK && _attacks.IsAttacking, "Input event did not enter Attack");
        StateBase attackState = _machine.CurrentStateInstance;
        Require(_machine.RefreshState() == CharacterState.ATTACK, "Read-only query overwrote Attack");
        Require(_machine.GetLocomotionState() == CharacterState.IDLE, "Movement context was lost during Attack");
        _machine.RequestAttack(AttackInput.LIGHT);
        _machine.Tick(0f);
        Require(Timeline.BufferInput == AttackInput.NONE, "Input before combo window was accepted");

        for (int i = 1; i <= (branchToX ? 3 : 4); i++)
        {
            AttackData data = AssetDatabase.LoadAssetAtPath<AttackData>($"Assets/05 Data/Theodore_Ground_Z{i}.asset");
            Require(_attacks.CurrentAttack == data, $"Expected Z{i}");
            Require(_machine.CurrentStateInstance == attackState, "Combo unnecessarily re-entered AttackState");
            AssertAnimation(data.Animation);
            AssertSpriteSample(data);
            _machine.Tick(data.ComboOpen + 0.001f);
            if (i < 3 || (!branchToX && i == 3))
            {
                _machine.RequestAttack(AttackInput.LIGHT);
                _machine.Tick(0f);
                Require(Timeline.BufferInput == AttackInput.LIGHT, "Valid combo input was not buffered");
                _machine.RequestAttack(AttackInput.HEAVY);
                _machine.Tick(0f);
                Require(Timeline.BufferInput == AttackInput.LIGHT, "Later input replaced the first queued branch");
            }
            else if (branchToX)
            {
                _machine.RequestAttack(AttackInput.HEAVY);
                _machine.Tick(0f);
                Require(Timeline.BufferInput == AttackInput.HEAVY, "Z3X branch was not buffered");
            }
            _machine.Tick(data.Duration);
        }
        if (branchToX)
        {
            AttackData finisher = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/05 Data/Theodore_Ground_Z3X.asset");
            Require(_attacks.CurrentAttack == finisher, "ZZZX selected the wrong finisher");
            AssertAnimation(finisher.Animation);
            _machine.Tick(finisher.Duration);
        }
        Require(_machine.CurrentState == CharacterState.IDLE && !_attacks.IsAttacking, "Combo did not return to Idle");
        Require(_attacks.CurrentAttack == null && Timeline.BufferInput == AttackInput.NONE, "Combo completion retained stale attack input");
        AssertAnimation("Base Layer.Theodore_Idle");
    }

    private static void ValidateCancelAndHitStun()
    {
        _machine.RequestAttack(AttackInput.LIGHT);
        _machine.Tick(0f);
        SetKeys(Key.RightArrow, Key.UpArrow);
        _machine.Tick(0.1f);
        PhysicsTick();
        Require(_machine.CurrentState == CharacterState.ATTACK, "Movement canceled Attack before CancelOpen");
        Require(Mathf.Approximately(_body.linearVelocity.x, 0f) && Mathf.Approximately(_body.linearVelocity.y, 0f), "Locked attack allowed movement or jump");
        _machine.Tick(0.11f);
        Require(_machine.CurrentState == CharacterState.WALK && !_attacks.IsAttacking, "Movement did not cancel Attack after CancelOpen");
        PhysicsTick();
        Require(Mathf.Approximately(_body.linearVelocity.y, 0f), "A jump pressed during lock replayed after cancel");
        AssertAnimation("Base Layer.Theodore_Walk");
        SetKeys();
        _machine.Tick(0f);
        PhysicsTick();

        _machine.RequestAttack(AttackInput.LIGHT);
        _machine.RequestHitStun(0.2f);
        _machine.Tick(0f);
        Require(_machine.CurrentState == CharacterState.HITSTUN && !_attacks.IsAttacking, "Hit did not win simultaneous Attack request");
        _machine.RequestHitStun(0.5f);
        _machine.Tick(0f);
        Require(Mathf.Approximately(_machine.GetState<HitStunState>().RemainingTime, 0.5f), "Repeated hit did not extend stun");
        SetKeys(Key.RightArrow, Key.UpArrow, Key.Z);
        _machine.Tick(0.1f);
        PhysicsTick();
        Require(!_machine.CanMove && !_machine.CanJump && !_attacks.IsAttacking, "HitStun accepted an action");
        Require(Mathf.Approximately(_body.linearVelocity.x, 0f) && Mathf.Approximately(_body.linearVelocity.y, 0f), "HitStun allowed movement or jump");
        SetKeys();
        _machine.Tick(0.5f);
        Require(_machine.CurrentState == CharacterState.IDLE, "Expired HitStun did not return to Idle");
        _machine.Tick(0f);
        Require(!_attacks.IsAttacking, "Rejected input replayed after HitStun");

        _machine.RequestAttack(AttackInput.LIGHT);
        _machine.Tick(0.1f);
        _machine.RequestAttack(AttackInput.LIGHT);
        _machine.Tick(0f);
        Require(Timeline.BufferInput == AttackInput.LIGHT, "Interrupt setup did not queue a combo");
        _machine.RequestHitStun(0.1f);
        PhysicsTick();
        Require(_machine.CurrentState == CharacterState.HITSTUN && Timeline.Attack == null
            && Timeline.BufferInput == AttackInput.NONE, "Physics did not interrupt and clear an active combo");
        _machine.Tick(0.2f);
        Require(_machine.CurrentState == CharacterState.IDLE, "Interrupted combo restarted after HitStun");
    }

    private static void ValidateAirAttack()
    {
        _character.transform.position += Vector3.up * 3f;
        Physics2D.SyncTransforms();
        _body.linearVelocity = Vector2.down;
        _machine.Tick(0f);
        Require(_machine.CurrentState == CharacterState.FALL, "Downward motion was classified as Jump");
        AssertAnimation("Base Layer.Theodore_Jump");
        _character.GetComponent<Animator>().Update(0.5f);
        AssertAnimation("Base Layer.Theodore_Jump");
        _machine.RequestAttack(AttackInput.LIGHT);
        _machine.Tick(0f);
        Require(_machine.CurrentState == CharacterState.ATTACK, "Falling prevented an air attack");
        Require(_attacks.CurrentAttack == AssetDatabase.LoadAssetAtPath<AttackData>("Assets/05 Data/Theodore_Jump_Z1.asset"), "Wrong air attack selected");
        _machine.Tick(1f);
        Require(_attacks.IsAttacking, "Landing-bound attack ended before landing");
        Ground();
        _machine.Tick(0f);
        Require(_machine.IsGrounded && _machine.CurrentState == CharacterState.IDLE
            && !_attacks.IsAttacking, "Landing was inferred from Attack state rather than physics");
    }

    private static void ValidateAirAttackMovement()
    {
        var horizontal = _character.GetComponent<CharacterHorizontalMovement>();
        var attackField = typeof(CharacterAttackController).GetField("_jumpZ1", PrivateInstance);
        var originalAttack = (AttackData)attackField.GetValue(_attacks);
        var attack = UnityEngine.Object.Instantiate(originalAttack);
        attackField.SetValue(_attacks, attack);
        try
        {
            foreach (bool locked in new[] { false, true })
            foreach (int direction in new[] { 1, -1 })
            {
                typeof(AttackData).GetField("_lockUntilLanding", PrivateInstance).SetValue(attack, locked);
                SetKeys();
                _input.CancelDash();
                Ground();
                _machine.Tick(0f);
                PhysicsTick();
                Key forward = direction > 0 ? Key.RightArrow : Key.LeftArrow;
                Key reverse = direction > 0 ? Key.LeftArrow : Key.RightArrow;
                SetKeys(forward);
                _machine.Tick(0f);
                SetKeys();
                _input.CancelDash();
                _character.transform.position += Vector3.up * 3f;
                Physics2D.SyncTransforms();
                _body.linearVelocity = Vector2.down;
                _machine.RequestAttack(AttackInput.LIGHT);
                _machine.Tick(0f);
                SetKeys(forward);
                _machine.Tick(0.01f);
                PhysicsTick();
                Require(_machine.CurrentState == CharacterState.ATTACK && _attacks.CurrentAttack == attack,
                    "Forward movement canceled the air attack");
                Require(Mathf.Approximately(_body.linearVelocity.x, locked ? 0f : direction * horizontal.WalkSpeed),
                    "Air attack movement did not respect Lock Until Landing");
                AssertAnimation(attack.Animation);
                SetKeys(reverse);
                _machine.Tick(0f);
                PhysicsTick();
                Require(Mathf.Approximately(_body.linearVelocity.x, 0f) && horizontal.FacingDirection == direction,
                    "Reverse input moved or turned an air attack");
                Require(_attacks.IsAttacking, "Reverse input canceled the air attack");
                SetKeys();
                PhysicsTick();
                Require(Mathf.Approximately(_body.linearVelocity.x, 0f), "Released air attack movement did not stop");
                Ground();
                _machine.Tick(0f);
                Require(!_attacks.IsAttacking, "Moving air attack did not finish on landing");
            }
        }
        finally
        {
            attackField.SetValue(_attacks, originalAttack);
            UnityEngine.Object.DestroyImmediate(attack);
        }
    }

    private static void ValidateAttackDistance()
    {
        var attackField = typeof(CharacterAttackController).GetField("_groundZ1", PrivateInstance);
        var originalAttack = (AttackData)attackField.GetValue(_attacks);
        var attack = UnityEngine.Object.Instantiate(originalAttack);
        typeof(AttackData).GetField("_nextZ", PrivateInstance).SetValue(attack, null);
        typeof(AttackData).GetField("_nextX", PrivateInstance).SetValue(attack, null);
        attackField.SetValue(_attacks, attack);
        try
        {
            foreach (float distance in new[] { 2f, -2f, 0f })
            foreach (int direction in new[] { 1, -1 })
            {
                typeof(AttackData).GetField("_moveDistance", PrivateInstance).SetValue(attack, distance);
                SetKeys();
                _input.CancelDash();
                Ground();
                _machine.Tick(0f);
                SetKeys(direction > 0 ? Key.RightArrow : Key.LeftArrow);
                _machine.Tick(0f);
                SetKeys();
                _input.CancelDash();
                _machine.Tick(0f);
                _machine.RequestAttack(AttackInput.LIGHT);
                _machine.Tick(0f);
                Require(_attacks.CurrentAttack == attack, "Distance validation selected the wrong attack");

                // Several render updates accumulate, but each displacement is applied only once.
                _machine.Tick(attack.Duration * 0.25f);
                _machine.Tick(attack.Duration * 0.25f);
                PhysicsTick();
                float expectedHalfDistance = direction * distance * 0.5f;
                Require(Mathf.Abs(_body.linearVelocity.x * Time.fixedDeltaTime - expectedHalfDistance) < 0.0001f,
                    "Attack distance did not track half of the total attack duration");
                PhysicsTick();
                Require(Mathf.Approximately(_body.linearVelocity.x, 0f), "Attack displacement was applied twice");
                _machine.Tick(attack.Duration);
                Require(!_attacks.IsAttacking, "Distance validation attack did not finish");
                PhysicsTick();
                Require(Mathf.Abs(_body.linearVelocity.x * Time.fixedDeltaTime - expectedHalfDistance) < 0.0001f,
                    "Attack completion lost or overshot its final displacement");
                PhysicsTick();
                Require(Mathf.Approximately(_body.linearVelocity.x, 0f), "Completed attack continued moving");

                _machine.RequestAttack(AttackInput.LIGHT);
                _machine.Tick(attack.Duration * 0.25f);
                _machine.RequestHitStun(0.1f);
                PhysicsTick();
                Require(Mathf.Approximately(_body.linearVelocity.x, 0f), "HitStun applied queued attack movement");
                _machine.Tick(0.2f);
            }
        }
        finally
        {
            attackField.SetValue(_attacks, originalAttack);
            UnityEngine.Object.DestroyImmediate(attack);
        }
    }

    private static void ValidateDisable()
    {
        _machine.RequestAttack(AttackInput.LIGHT);
        _machine.Tick(0.1f);
        _machine.RequestAttack(AttackInput.LIGHT);
        _machine.Tick(0f);
        _machine.enabled = false;
        Require(!_attacks.IsAttacking && Timeline.BufferInput == AttackInput.NONE
            && _machine.CurrentStateInstance == null, "FSM disable did not exit and clear Attack");
        _machine.enabled = true;
        SetKeys(Key.Z);
        _machine.Tick(0f);
        Require(_machine.CurrentState == CharacterState.ATTACK, "FSM re-enable did not reconnect input");
        _character.SetActive(false);
        Require(!_attacks.IsAttacking && Timeline.Attack == null, "GameObject disable did not reset combat");
        _character.SetActive(true);
        SetKeys();
        _machine.Tick(0f);
        Require(_machine.CurrentState == CharacterState.IDLE && !_attacks.IsAttacking, "GameObject re-enable replayed stale input");
    }

    private static AttackTimeline Timeline => (AttackTimeline)typeof(CharacterAttackController)
        .GetField("_timeline", PrivateInstance).GetValue(_attacks);

    private static void Ground()
    {
        _body.linearVelocity = Vector2.zero;
        _character.transform.position = new Vector3(1000f, 5f, 0f);
        Physics2D.SyncTransforms();
        _character.transform.position += Vector3.up * (0.01f - _character.GetComponent<Collider2D>().bounds.min.y);
        Physics2D.SyncTransforms();
        _character.scene.GetPhysicsScene2D().Simulate(0.001f);
        _body.linearVelocity = Vector2.zero;
    }

    private static void PhysicsTick() => _machine.SendMessage("FixedUpdate", SendMessageOptions.RequireReceiver);

    private static void SetKeys(params Key[] keys)
    {
        InputSystem.QueueStateEvent(_keyboard, new KeyboardState(keys));
        InputSystem.Update();
    }

    private static void AssertAnimation(string path)
    {
        var animator = _character.GetComponent<Animator>();
        animator.Update(0f);
        Require(animator.GetCurrentAnimatorStateInfo(0).fullPathHash == Animator.StringToHash(path), $"Expected animation {path}");
    }

    private static void AssertSpriteSample(AttackData attack)
    {
        var animator = _character.GetComponent<Animator>();
        _character.GetComponent<CharacterAnimator>().SetAttackProgress(0.5f);
        animator.Update(0f);
        AnimationClip clip = animator.GetCurrentAnimatorClipInfo(0)[0].clip;
        var keys = AnimationUtility.GetObjectReferenceCurve(clip,
            EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"));
        Require(keys != null && keys.Length > 0, $"Missing sprite animation for {attack.AttackId}");
        UnityEngine.Object expected = keys[0].value;
        foreach (var key in keys)
            if (key.time <= clip.length * 0.5f) expected = key.value;
        Require(_character.GetComponent<SpriteRenderer>().sprite == expected, "Combat progress sampled the wrong sprite");
    }

    private static void Require(bool condition, string message)
    {
        _checks++;
        if (!condition) throw new InvalidOperationException(message);
    }
}
