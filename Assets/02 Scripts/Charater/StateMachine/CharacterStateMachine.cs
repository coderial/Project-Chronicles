using System;
using System.Collections.Generic;
using Project_Chronicles.Character.Combat;
using UnityEngine;

namespace Project_Chronicles.Character.StateMachine
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController), typeof(PlayerInputListener))]
    [RequireComponent(typeof(CharacterHorizontalMovement), typeof(CharacterVerticalMovement))]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class CharacterStateMachine : MonoBehaviour
    {
        private readonly Dictionary<Type, StateBase> _states = new Dictionary<Type, StateBase>();
        private readonly Queue<AttackInput> _attackRequests = new Queue<AttackInput>();
        private CharacterController _controller;
        private PlayerInputListener _input;
        private Rigidbody2D _body;
        private StateBase _current;
        private AttackState _attackState;
        private HitStunState _hitStunState;
        private float _requestedHitStun;

        public CharacterState CurrentState => _current?.StateType ?? CharacterState.IDLE;
        public StateBase CurrentStateInstance => _current;
        public float DeltaTime { get; private set; }
        public bool CanMove => isActiveAndEnabled && (_current?.AllowsMovement ?? false);
        public bool CanJump => isActiveAndEnabled && (_current?.AllowsJump ?? false);
        public bool IsGrounded => _body.linearVelocity.y <= 0f && _controller.VerticalMovement.IsGrounded;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _controller.InitializeComponents();
            _input = GetComponent<PlayerInputListener>();
            _body = GetComponent<Rigidbody2D>();
            Register(new IdleState(_controller));
            Register(new WalkState(_controller));
            Register(new DashState(_controller));
            Register(new JumpState(_controller));
            Register(new FallState(_controller));
            _attackState = new AttackState(_controller);
            _hitStunState = new HitStunState(_controller);
            Register(_attackState);
            Register(_hitStunState);
        }

        private void OnEnable()
        {
            _input.LightAttackRequested += OnLightAttack;
            _input.HeavyAttackRequested += OnHeavyAttack;
        }

        private void Start() => EnsureEntered();

        private void Update()
        {
            if (Time.timeScale <= 0f)
            {
                _attackRequests.Clear();
                _input.CancelJump();
                return;
            }
            Tick(Time.deltaTime);
        }

        // Input callbacks enqueue requests; only this coordinator runs state logic.
        public void Tick(float deltaTime)
        {
            if (!isActiveAndEnabled || deltaTime < 0f) return;
            EnsureEntered();
            DeltaTime = deltaTime;
            ApplyPendingHitStun();

            _controller.VerticalMovement.UpdateAirborneDash(CanMove && !_hitStunState.CanEnter(), IsGrounded);
            _controller.HorizontalMovement.UpdateFacingDirection(CanMove && !_hitStunState.CanEnter());
            while (_attackRequests.Count > 0)
            {
                AttackInput input = _attackRequests.Dequeue();
                if (_hitStunState.CanEnter()) continue;
                if (_current == _attackState && _controller.AttackController.IsAttacking)
                    _controller.AttackController.TryBufferInput(input);
                else if (!_controller.AttackController.HasPreparedAttack)
                    _controller.AttackController.PrepareOpeningAttack(input, GetAttackMovementState());
            }

            ResolveTransition();
            _current.Update();
            // Completion permits an immediate return to lower-priority locomotion.
            ResolveTransition();
        }

        private void FixedUpdate()
        {
            EnsureEntered();
            // An interrupt received before physics must block movement immediately.
            ApplyPendingHitStun();
            ResolveTransition();
            _controller.VerticalMovement.UpdateAirborneDash(CanMove, IsGrounded);
            _controller.VerticalMovement.TickMovement(CanJump);
            ResolveTransition();
            _controller.HorizontalMovement.TickMovement(CanMove);
            _current.FixedUpdate();
        }

        public T GetState<T>() where T : StateBase =>
            _states.TryGetValue(typeof(T), out StateBase state) ? (T)state : null;

        public void RequestAttack(AttackInput input)
        {
            if (isActiveAndEnabled && (input == AttackInput.LIGHT || input == AttackInput.HEAVY)
                && _attackRequests.Count < 8)
                _attackRequests.Enqueue(input);
        }

        public void RequestHitStun(float duration)
        {
            if (isActiveAndEnabled && duration > 0f && !float.IsInfinity(duration))
                _requestedHitStun = Mathf.Max(_requestedHitStun, duration);
        }

        // Compatibility query: reading state no longer mutates it.
        public CharacterState RefreshState() => CurrentState;

        public CharacterState GetLocomotionState()
        {
            if (!IsGrounded)
            {
                if (_body.linearVelocity.y <= 0f) return CharacterState.FALL;
                return _controller.VerticalMovement.WasDashJump
                    ? CharacterState.DASH_JUMP : CharacterState.JUMP;
            }
            return ResolveState(_input.HorizontalInput, _controller.HorizontalMovement.IsRunning);
        }

        private CharacterState GetAttackMovementState() =>
            !IsGrounded && _controller.VerticalMovement.WasDashJump
                ? CharacterState.DASH_JUMP : GetLocomotionState();

        private void Register(StateBase state) => _states.Add(state.GetType(), state);

        private void EnsureEntered()
        {
            if (_current != null) return;
            _current = GetLocomotionInstance();
            _current.Enter();
        }

        private StateBase GetLocomotionInstance()
        {
            switch (GetLocomotionState())
            {
                case CharacterState.WALK: return GetState<WalkState>();
                case CharacterState.RUN: return GetState<DashState>();
                case CharacterState.JUMP:
                case CharacterState.DASH_JUMP: return GetState<JumpState>();
                case CharacterState.FALL: return GetState<FallState>();
                default: return GetState<IdleState>();
            }
        }

        private void ApplyPendingHitStun()
        {
            if (_requestedHitStun <= 0f) return;
            _hitStunState.Extend(_requestedHitStun);
            _controller.HorizontalMovement.ClearAttackDisplacement();
            _requestedHitStun = 0f;
            _attackRequests.Clear();
            _controller.AttackController.ClearPreparedAttack();
        }

        private void ResolveTransition()
        {
            StateBase next = null;
            foreach (StateBase candidate in _states.Values)
            {
                if (!candidate.CanEnter() || !_current.CanExit(candidate)) continue;
                if (next == null || candidate.Priority > next.Priority
                    || (candidate.Priority == next.Priority && candidate == _current))
                    next = candidate;
            }
            if (next == null || next == _current) return;
            _current.Exit();
            _current = next;
            _current.Enter();
        }

        private void OnLightAttack() => RequestAttack(AttackInput.LIGHT);
        private void OnHeavyAttack() => RequestAttack(AttackInput.HEAVY);

        private void OnDisable()
        {
            _input.LightAttackRequested -= OnLightAttack;
            _input.HeavyAttackRequested -= OnHeavyAttack;
            _current?.Exit();
            _current = null;
            _hitStunState.Extend(0f);
            _requestedHitStun = 0f;
            _attackRequests.Clear();
            _controller.AttackController.ClearPreparedAttack();
            _input.CancelJump();
            _input.CancelDash();
            _controller.HorizontalMovement.Stop();
            _controller.Animator.PlayLocomotion(CharacterState.IDLE);
        }

        public static CharacterState ResolveState(float horizontalVelocity, bool isRunning)
        {
            if (Mathf.Approximately(horizontalVelocity, 0f)) return CharacterState.IDLE;
            return isRunning ? CharacterState.RUN : CharacterState.WALK;
        }
    }
}
