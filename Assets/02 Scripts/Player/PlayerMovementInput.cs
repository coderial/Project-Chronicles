using UnityEngine;
using UnityEngine.InputSystem;

namespace Project_Chronicles.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerMovementInput : MonoBehaviour
    {
        public const float PrototypeRunInputWindow = 0.2f;

        [SerializeField, Min(0f)]
        private float _runInputWindow = PrototypeRunInputWindow;

        private PlayerInputSystem _inputSystem;
        private InputAction _moveLeftAction;
        private InputAction _moveRightAction;
        private float _runDirection;
        private double _lastLeftPressedTime = double.NegativeInfinity;
        private double _lastRightPressedTime = double.NegativeInfinity;

        public float RunInputWindow => _runInputWindow;
        public float HorizontalInput => !isActiveAndEnabled ? 0f :
            (_moveRightAction.IsPressed() ? 1f : 0f) - (_moveLeftAction.IsPressed() ? 1f : 0f);
        public bool IsRunning => _runDirection != 0f && HorizontalInput == _runDirection;

        private void Awake()
        {
            _inputSystem = new PlayerInputSystem();
            _moveLeftAction = _inputSystem.Player.MoveLeft;
            _moveRightAction = _inputSystem.Player.MoveRight;
        }

        private void OnEnable()
        {
            _moveLeftAction.performed += OnMoveLeft;
            _moveRightAction.performed += OnMoveRight;
            _moveLeftAction.canceled += OnMoveCanceled;
            _moveRightAction.canceled += OnMoveCanceled;
            _moveLeftAction.Enable();
            _moveRightAction.Enable();
        }

        private void OnDisable()
        {
            _moveLeftAction.performed -= OnMoveLeft;
            _moveRightAction.performed -= OnMoveRight;
            _moveLeftAction.canceled -= OnMoveCanceled;
            _moveRightAction.canceled -= OnMoveCanceled;
            _moveLeftAction.Disable();
            _moveRightAction.Disable();
            CancelRun();
        }

        private void OnDestroy()
        {
            _inputSystem?.Dispose();
        }

        public void CancelRun()
        {
            _runDirection = 0f;
            _lastLeftPressedTime = double.NegativeInfinity;
            _lastRightPressedTime = double.NegativeInfinity;
        }

        private void OnMoveLeft(InputAction.CallbackContext context)
        {
            TryStartRun(ref _lastLeftPressedTime, -1f, context.time);
        }

        private void OnMoveRight(InputAction.CallbackContext context)
        {
            TryStartRun(ref _lastRightPressedTime, 1f, context.time);
        }

        private void OnMoveCanceled(InputAction.CallbackContext context)
        {
            StopRunIfDirectionChanged();
        }

        private void TryStartRun(ref double lastPressedTime, float direction, double pressedTime)
        {
            if (IsSecondTapWithinWindow(pressedTime - lastPressedTime, _runInputWindow))
            {
                _runDirection = direction;
                lastPressedTime = double.NegativeInfinity;
            }
            else
            {
                lastPressedTime = pressedTime;
            }

            StopRunIfDirectionChanged();
        }

        private void StopRunIfDirectionChanged()
        {
            if (HorizontalInput != _runDirection)
            {
                _runDirection = 0f;
            }
        }

        public static bool IsSecondTapWithinWindow(double timeSincePreviousPress, float inputWindow)
        {
            return timeSincePreviousPress >= 0d && timeSincePreviousPress <= Mathf.Max(0f, inputWindow);
        }
    }
}
