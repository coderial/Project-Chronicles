#if UNITY_EDITOR
using System.Reflection;
using NUnit.Framework;
using Project_Chronicles.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Project_Chronicles.Tests
{
    public class PlayerMovementTests
    {
        private GameObject _player;
        private Keyboard _keyboard;
        private PlayerMovementInput _input;
        private PlayerHorizontalMovement _movement;
        private PlayerStateMachine _stateMachine;
        private PlayerAnimator _presentation;
        private Rigidbody2D _body;
        private Animator _animator;
        private InputSettings.UpdateMode _previousUpdateMode;
        private InputSettings.BackgroundBehavior _previousBackgroundBehavior;
        private InputSettings.EditorInputBehaviorInPlayMode _previousEditorBehavior;
        private double _eventTime;

        [SetUp]
        public void SetUp()
        {
            _previousUpdateMode = InputSystem.settings.updateMode;
            _previousBackgroundBehavior = InputSystem.settings.backgroundBehavior;
            _previousEditorBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _eventTime = InputState.currentTime;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03 Prefabs/Theodore.prefab");
            _player = Object.Instantiate(prefab);
            _input = _player.GetComponent<PlayerMovementInput>();
            _movement = _player.GetComponent<PlayerHorizontalMovement>();
            _stateMachine = _player.GetComponent<PlayerStateMachine>();
            _presentation = _player.GetComponent<PlayerAnimator>();
            _body = _player.GetComponent<Rigidbody2D>();
            _animator = _player.GetComponent<Animator>();

            // PlayMode initializes components; tests drive frame phases explicitly.
            _animator.Rebind();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_player);
            if (_keyboard != null)
            {
                InputSystem.RemoveDevice(_keyboard);
            }
            InputSystem.settings.updateMode = _previousUpdateMode;
            InputSystem.settings.backgroundBehavior = _previousBackgroundBehavior;
            InputSystem.settings.editorInputBehaviorInPlayMode = _previousEditorBehavior;
        }

        [TestCase(Key.LeftArrow, -6f, true)]
        [TestCase(Key.RightArrow, 6f, false)]
        public void SinglePressWalksAndUpdatesPresentation(Key key, float speed, bool flipX)
        {
            SetKeys(0.01, key);
            Tick();
            Assert.That(_body.linearVelocity.x, Is.EqualTo(speed));
            Assert.That(_stateMachine.CurrentState, Is.EqualTo(PlayerState.WALK));
            Assert.That(_animator.GetBool("IsWalk"), Is.True);
            Assert.That(_animator.GetBool("IsRun"), Is.False);
            Assert.That(_player.GetComponent<SpriteRenderer>().flipX, Is.EqualTo(flipX));
        }

        [TestCase(Key.LeftArrow, -12f)]
        [TestCase(Key.RightArrow, 12f)]
        public void DoubleTapRunsUntilReleased(Key key, float speed)
        {
            DoubleTap(key);
            Tick();
            Assert.That(_body.linearVelocity.x, Is.EqualTo(speed));
            Assert.That(_stateMachine.CurrentState, Is.EqualTo(PlayerState.RUN));
            Assert.That(_animator.GetBool("IsRun"), Is.True);
            Assert.That(_animator.GetBool("IsWalk"), Is.False);

            SetKeys(1.0, key);
            Tick();
            Assert.That(_body.linearVelocity.x, Is.EqualTo(speed), "Holding sustains running beyond the tap window.");
            SetKeys(0.01);
            Tick();
            Assert.That(_body.linearVelocity.x, Is.Zero);
            Assert.That(_stateMachine.CurrentState, Is.EqualTo(PlayerState.IDLE));
            Assert.That(_animator.GetBool("IsRun"), Is.False);
        }

        [Test]
        public void LateSecondTapRemainsWalking()
        {
            SetKeys(0.01, Key.RightArrow);
            SetKeys(0.01);
            SetKeys(0.3, Key.RightArrow);
            Tick();
            Assert.That(_body.linearVelocity.x, Is.EqualTo(6f));
        }

        [Test]
        public void OpposingKeysStopAndCancelRun()
        {
            DoubleTap(Key.RightArrow);
            SetKeys(0.01, Key.LeftArrow, Key.RightArrow);
            Tick();
            Assert.That(_body.linearVelocity.x, Is.Zero);
            Assert.That(_input.IsRunning, Is.False);
            SetKeys(0.01, Key.LeftArrow);
            Tick();
            Assert.That(_body.linearVelocity.x, Is.EqualTo(-6f));
        }

        [Test]
        public void VerticalVelocityIsPreservedWithoutWalkingAnimation()
        {
            _body.linearVelocity = new Vector2(0f, -3f);
            Tick();
            Assert.That(_body.linearVelocity.y, Is.EqualTo(-3f));
            Assert.That(_stateMachine.CurrentState, Is.EqualTo(PlayerState.IDLE));
            SetKeys(0.01, Key.RightArrow);
            Tick();
            Assert.That(_body.linearVelocity, Is.EqualTo(new Vector2(6f, -3f)));
        }

        [Test]
        public void ReenableRestoresInputWithoutStaleRunOrDuplicateCallbacks()
        {
            DoubleTap(Key.RightArrow);
            _input.enabled = false;
            SetKeys(0.01);
            _input.enabled = true;
            SetKeys(0.01, Key.RightArrow);
            Tick();
            Assert.That(_body.linearVelocity.x, Is.EqualTo(6f));
            SetKeys(0.01);
            SetKeys(0.05, Key.RightArrow);
            Tick();
            Assert.That(_body.linearVelocity.x, Is.EqualTo(12f));
        }

        [Test]
        public void DisablingMovementStopsOnlyHorizontalVelocity()
        {
            DoubleTap(Key.RightArrow);
            _body.linearVelocity = new Vector2(12f, -3f);
            _movement.enabled = false;
            Assert.That(_body.linearVelocity, Is.EqualTo(new Vector2(0f, -3f)));
            Assert.That(_input.IsRunning, Is.False);
        }

        [Test]
        public void TapWindowIncludesBoundaryAndRejectsInvalidIntervals()
        {
            Assert.That(PlayerMovementInput.IsSecondTapWithinWindow(0.2f, 0.2f), Is.True);
            Assert.That(PlayerMovementInput.IsSecondTapWithinWindow(0.201, 0.2f), Is.False);
            Assert.That(PlayerMovementInput.IsSecondTapWithinWindow(-0.01, 0.2f), Is.False);
            Assert.That(PlayerMovementInput.IsSecondTapWithinWindow(double.PositiveInfinity, 0.2f), Is.False);
        }

        private void DoubleTap(Key key)
        {
            SetKeys(0.01, key);
            SetKeys(0.01);
            SetKeys(0.05, key);
        }

        private void SetKeys(double elapsed, params Key[] keys)
        {
            _eventTime += elapsed;
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(keys), _eventTime);
            InputSystem.Update();
            foreach (Key key in keys)
            {
                Assert.That(_keyboard[key].isPressed, Is.True, "Synthetic keyboard event was not processed.");
            }
        }

        private void Tick()
        {
            Invoke(_movement, "FixedUpdate");
            Invoke(_movement, "Update");
            Invoke(_stateMachine, "Update");
            Invoke(_presentation, "LateUpdate");
        }

        private static void Invoke(object target, string method)
        {
            Assert.That(target, Is.Not.Null, "Required player component is missing.");
            target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
        }
    }
}
#endif
