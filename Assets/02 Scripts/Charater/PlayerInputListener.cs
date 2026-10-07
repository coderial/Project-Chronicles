using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project_Chronicles.Character
{
    [DisallowMultipleComponent]
    public sealed class PlayerInputListener : MonoBehaviour
    {
        public const float PrototypeDashInputWindow = 0.2f;

        [SerializeField, Min(0f)]
        private float _dashInputWindow = PrototypeDashInputWindow;
        private PlayerInputSystem _inputSystem;
        private InputAction _moveLeftAction;
        private InputAction _moveRightAction;
        private InputAction _lightAttackAction;
        private InputAction _heavyAttackAction;
        private InputAction _jumpAction;
        private float _dashDirection;
        private double _lastLeftPressedTime = double.NegativeInfinity;
        private double _lastRightPressedTime = double.NegativeInfinity;
        private float _lastJumpRequestedTime = float.NegativeInfinity;

        public event Action LightAttackRequested;
        public event Action HeavyAttackRequested;

        public int LightAttackRequestedCount { get; private set; }
        public int HeavyAttackRequestedCount { get; private set; }

        public float DashInputWindow => _dashInputWindow;
        public float LastJumpRequestedTime => _lastJumpRequestedTime;
        public float HorizontalInput => !isActiveAndEnabled ? 0f :
            (_moveRightAction.IsPressed() ? 1f : 0f) - (_moveLeftAction.IsPressed() ? 1f : 0f);
        public bool IsDashing => _dashDirection != 0f && HorizontalInput == _dashDirection;
        public float VerticalInput => !isActiveAndEnabled ? 0f : (_jumpAction.IsPressed() ? 1f : 0f);

        private void Awake()
        {
            _inputSystem = new PlayerInputSystem();
            _moveLeftAction = _inputSystem.Character.MoveLeft;
            _moveRightAction = _inputSystem.Character.MoveRight;
            _lightAttackAction = _inputSystem.Character.LightAttack;
            _heavyAttackAction = _inputSystem.Character.HeavyAttack;
            _jumpAction = _inputSystem.Character.Jump;
        }

        private void OnEnable()
        {
            _moveLeftAction.performed += OnMoveLeft;
            _moveRightAction.performed += OnMoveRight;
            _jumpAction.performed += OnJump;
            _lightAttackAction.performed += OnLightAttack;
            _heavyAttackAction.performed += OnHeavyAttack;

            _moveLeftAction.canceled += OnMoveCanceled;
            _moveRightAction.canceled += OnMoveCanceled;

            _moveLeftAction.Enable();
            _moveRightAction.Enable();
            _jumpAction.Enable();
            _lightAttackAction.Enable();
            _heavyAttackAction.Enable();
        }

        private void OnDisable()
        {
            _moveLeftAction.performed -= OnMoveLeft;
            _moveRightAction.performed -= OnMoveRight;
            _jumpAction.performed -= OnJump;
            _lightAttackAction.performed -= OnLightAttack;
            _heavyAttackAction.performed -= OnHeavyAttack;
            _moveLeftAction.canceled -= OnMoveCanceled;
            _moveRightAction.canceled -= OnMoveCanceled;

            _moveLeftAction.Disable();
            _moveRightAction.Disable();
            _jumpAction.Disable();
            _lightAttackAction.Disable();
            _heavyAttackAction.Disable();

            CancelDash();
            CancelJump();
        }

        private void OnDestroy()
        {
            _inputSystem?.Dispose();
        }

        public void CancelDash()
        {
            _dashDirection = 0f;
            _lastLeftPressedTime = double.NegativeInfinity;
            _lastRightPressedTime = double.NegativeInfinity;
        }

        public void CancelJump()
        {
            _lastJumpRequestedTime = float.NegativeInfinity;
        }

        private void OnJump(InputAction.CallbackContext context)
        {
            _lastJumpRequestedTime = Time.time;
        }

        private void OnMoveLeft(InputAction.CallbackContext context)
        {
            TryStartDash(ref _lastLeftPressedTime, -1f, context.time);
        }

        private void OnMoveRight(InputAction.CallbackContext context)
        {
            TryStartDash(ref _lastRightPressedTime, 1f, context.time);
        }

        private void OnMoveCanceled(InputAction.CallbackContext context)
        {
            StopDashIfDirectionChanged();
        }

        private void OnLightAttack(InputAction.CallbackContext context)
        {
            LightAttackRequested?.Invoke();
        }

        private void OnHeavyAttack(InputAction.CallbackContext context)
        {
            HeavyAttackRequested?.Invoke();
        }

        private void TryStartDash(ref double lastPressedTime, float direction, double pressedTime)
        {
            if (IsSecondTapWithinWindow(pressedTime - lastPressedTime, _dashInputWindow))
            {
                _dashDirection = direction;
                lastPressedTime = double.NegativeInfinity;
            }
            else
            {
                lastPressedTime = pressedTime;
            }

            StopDashIfDirectionChanged();
        }

        private void StopDashIfDirectionChanged()
        {
            if (HorizontalInput != _dashDirection)
            {
                _dashDirection = 0f;
            }
        }

        public static bool IsSecondTapWithinWindow(double timeSincePreviousPress, float inputWindow)
        {
            return timeSincePreviousPress >= 0d && timeSincePreviousPress <= Mathf.Max(0f, inputWindow);
        }
    }
}
