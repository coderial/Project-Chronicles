using System;
using System.Reflection;
using Project_Chronicles.Character;
using Project_Chronicles.Character.Combat;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Run from Tools/Chronicles or with -executeMethod CombatAnimationValidation.Run.
public static class CombatAnimationValidation
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    [MenuItem("Tools/Chronicles/Validate Combat Animations")]
    public static void Run()
    {
        GameObject instance = null;
        Scene previewScene = default;
        try
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/04 Anim/Theodore.controller");
            var attacks = new AttackData[4];
            var states = controller.layers[0].stateMachine.stateMachines[0].stateMachine.states;
            for (int i = 0; i < attacks.Length; i++)
            {
                attacks[i] = AssetDatabase.LoadAssetAtPath<AttackData>($"Assets/05 Data/Theodore_Z{i + 1}.asset");
                Require(attacks[i] != null, $"Missing Z{i + 1} data");
                Require(attacks[i].Animation == $"Base Layer.GroundAttack.Theodore_Z{i + 1}", "State path mismatch");
                Require(attacks[i].NextZ == (i < 3
                    ? AssetDatabase.LoadAssetAtPath<AttackData>($"Assets/05 Data/Theodore_Z{i + 2}.asset") : null), "Broken combo branch");
                var state = Array.Find(states, s => s.state.name == $"Theodore_Z{i + 1}").state;
                Require(state != null && state.transitions.Length == 0, "Attack must wait for combat logic");
                Require(state.timeParameterActive && state.timeParameter == "AttackProgress", "Motion Time is not configured");
                Require(state.motion is AnimationClip clip && !clip.isLooping, "Attack clip must not loop");
            }

            previewScene = EditorSceneManager.NewPreviewScene();
            instance = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03 Prefabs/Theodore.prefab"), previewScene);
            var input = instance.GetComponent<PlayerInputListener>();
            var characterAnimator = instance.GetComponent<CharacterAnimator>();
            var attackController = instance.GetComponent<AttackController>();
            var stateMachine = instance.GetComponent<CharacterStateMachine>();
            var animator = instance.GetComponent<Animator>();
            Require(attackController != null, "Prefab is missing AttackController");
            Invoke(input, "Awake");
            Invoke(instance.GetComponent<CharacterHorizontalMovement>(), "Awake");
            Invoke(stateMachine, "Awake");
            Invoke(characterAnimator, "Awake");
            Invoke(attackController, "Awake");
            animator.Rebind();
            animator.Update(0f);
            var timeline = (AttackTimeline)typeof(AttackController).GetField("_timeline", PrivateInstance).GetValue(attackController);

            // A press outside the window must not queue another attack.
            Invoke(attackController, "TryStartZ1");
            Invoke(attackController, "TryStartZ1");
            Require(timeline.BufferInput == AttackInput.NONE, "Early input was accepted");
            Finish(attackController, timeline);
            AssertState(animator, "Base Layer.Theodore_Idle");

            Invoke(attackController, "TryStartZ1");
            for (int i = 0; i < attacks.Length; i++)
            {
                Require(timeline.Attack == attacks[i], $"Expected Z{i + 1}");
                AssertState(animator, attacks[i].Animation);
                characterAnimator.SetAttackProgress(0.5f);
                animator.Update(0f);
                var clip = (AnimationClip)Array.Find(states, s => s.state.name == $"Theodore_Z{i + 1}").state.motion;
                var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
                var keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                UnityEngine.Object expectedSprite = keys[0].value;
                foreach (var key in keys)
                    if (key.time <= clip.length * 0.5f) expectedSprite = key.value;
                Require(instance.GetComponent<SpriteRenderer>().sprite == expectedSprite, "Motion Time did not sample the expected sprite");

                timeline.Tick(attacks[i].ComboOpen + 0.001f);
                Invoke(attackController, "TryStartX1");
                Require(timeline.BufferInput == AttackInput.NONE, "Missing X branch consumed the buffer");
                if (i < 3)
                {
                    Invoke(attackController, "TryStartZ1");
                    Require(timeline.BufferInput == AttackInput.LIGHT, "Combo input was not buffered");
                }
                Finish(attackController, timeline);
            }
            Require(!timeline.IsRunning, "Z4 did not finish");
            AssertState(animator, "Base Layer.Theodore_Idle");

            foreach (CharacterState state in new[] { CharacterState.WALK, CharacterState.RUN })
            {
                typeof(CharacterStateMachine).GetField("<CurrentState>k__BackingField", PrivateInstance).SetValue(stateMachine, state);
                Invoke(attackController, "TryStartZ1");
                Finish(attackController, timeline);
                AssertState(animator, state == CharacterState.WALK ? "Base Layer.Theodore_Walk" : "Base Layer.Theodore_Run");
            }
            Invoke(attackController, "TryStartZ1");
            Invoke(attackController, "OnDisable");
            Require(timeline.Attack == null && !timeline.IsRunning, "Disable did not clear attack");
            Debug.Log("COMBAT_ANIMATION_VALIDATION_PASSED: Z1-Z4, input windows, sprite sampling, locomotion return, disable reset.");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            throw;
        }
        finally
        {
            if (instance != null) UnityEngine.Object.DestroyImmediate(instance);
            if (previewScene.IsValid()) EditorSceneManager.ClosePreviewScene(previewScene);
        }
    }

    private static void Finish(AttackController controller, AttackTimeline timeline)
    {
        controller.GetType().GetMethod("TickAttack", PrivateInstance)
            .Invoke(controller, new object[] { timeline.Attack.Duration });
    }

    private static void AssertState(Animator animator, string path)
    {
        animator.Update(0f);
        Require(animator.GetCurrentAnimatorStateInfo(0).fullPathHash == Animator.StringToHash(path), $"Expected animation {path}");
    }

    private static void Invoke(object target, string method)
    {
        target.GetType().GetMethod(method, PrivateInstance).Invoke(target, null);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
