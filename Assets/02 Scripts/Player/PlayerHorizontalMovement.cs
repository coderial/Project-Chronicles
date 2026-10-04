using UnityEngine;
using UnityEngine.InputSystem;

namespace Project_Chronicles.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PlayerHorizontalMovement : MonoBehaviour
    {
        public const float PrototypeWalkSpeed = 6f;
        public const float PrototypeRunInputWindow = 0.2f;
        public const float PrototypeRunSpeed = 12f;

        [SerializeField, Min(0f)]
        private float _walkSpeed = PrototypeWalkSpeed;

        [SerializeField, Min(0f)]
        private float _runInputWindow = PrototypeRunInputWindow;

        [SerializeField, Min(0f)]
        private float _runSpeed = PrototypeRunSpeed;

        private PlayerInputSystem _inputSystem;
        private InputAction _moveLeftAction;
        private InputAction _moveRightAction;
        private Rigidbody2D _body;
        private SpriteRenderer _sprite;
        private float _horizontalInput;
        private float _facingDirection = 1f;
        private float _runDirection;
        private float _runTimeRemaining;
        private float _lastLeftPressedTime = float.NegativeInfinity;
        private float _lastRightPressedTime = float.NegativeInfinity;

        public float WalkSpeed => _walkSpeed;
        public float RunInputWindow => _runInputWindow;
        public float RunSpeed => _runSpeed;
        public float HorizontalInput => _horizontalInput;
        public float FacingDirection => _facingDirection;
        public bool IsRuning => _runTimeRemaining > 0f ||
            (_runDirection != 0f && Mathf.Approximately(_horizontalInput, _runDirection));

        private void Awake()
        {
            _inputSystem = new PlayerInputSystem();
            _body = GetComponent<Rigidbody2D>();
            _sprite = GetComponent<SpriteRenderer>();
            _moveLeftAction = _inputSystem.Player.MoveLeft;
            _moveRightAction = _inputSystem.Player.MoveRight;
        }
        private void OnEnable()
        {
            _moveLeftAction.performed += OnMoveLeft;
            _moveRightAction.performed += OnMoveRight;

            _moveLeftAction.Enable();
            _moveRightAction.Enable();
        }

        private void OnDisable()
        {
            if (_moveLeftAction != null)
            {
                _moveLeftAction.performed -= OnMoveLeft;
                _moveLeftAction.Disable();
                _moveLeftAction = null;
            }

            if (_moveRightAction != null)
            {
                _moveRightAction.performed -= OnMoveRight;
                _moveRightAction.Disable();
                _moveRightAction = null;
            }
            _horizontalInput = 0f;
        }

        private void Update()
        {
            float left = _inputSystem.Player.MoveLeft.IsPressed() ? -1f : 0f;
            float right = _inputSystem.Player.MoveRight.IsPressed() ? 1f : 0f;
            _horizontalInput = left + right;

            if (_horizontalInput != 0)
            {
                _facingDirection = Mathf.Sign(_horizontalInput);
                _sprite.flipX = _facingDirection < 0;
            }
        }

        private void FixedUpdate()
        {
            Vector2 veloctiy = _body.linearVelocity;
            if (IsRuning)
            {
                veloctiy.x = CalculateRunVelocity(_runDirection, _runSpeed);
                _runTimeRemaining = Mathf.Max(0f, _runTimeRemaining - Time.fixedDeltaTime);
            }
            else
            {
                veloctiy.x = CalculateHorizontalVelocity(_horizontalInput, _walkSpeed);
                _runDirection = 0f;
            }
            _body.linearVelocity = veloctiy;
        }

        public void CancelRun()
        {
            _runDirection = 0f;
            _runTimeRemaining = 0f;
            _lastRightPressedTime = float.NegativeInfinity;
            _lastLeftPressedTime = float.NegativeInfinity;
        }

        public void OnMoveRight(InputAction.CallbackContext context)
        {
            TryStartRun(ref _lastRightPressedTime, 1f);
        }

        public void OnMoveLeft(InputAction.CallbackContext context)
        {
            TryStartRun(ref _lastLeftPressedTime, -1f);

        }

        public static bool IsSecondTapWithinWindow(float timeSincePreviousPress, float inputWindow)
        {
            float safeInputWindow = Mathf.Max(0f, inputWindow);
            return timeSincePreviousPress >= 0f && timeSincePreviousPress <= safeInputWindow;
        }

        public static float CalculateRunVelocity(float direction, float speed)
        {
            return Mathf.Clamp(direction, -1f, 1f) * Mathf.Max(0f, speed);
        }

        public static float CalculateHorizontalVelocity(float input, float speed)
        {
            return Mathf.Clamp(input, -1f, 1f) * Mathf.Max(0f, speed);
        }

        private void TryStartRun(ref float lastPressedTime, float direction)
        {
            float pressedTime = Time.time;
            float timeSincePreviousPress = pressedTime - lastPressedTime;
            if (IsSecondTapWithinWindow(timeSincePreviousPress, _runInputWindow))
            {
                _runDirection = Mathf.Sign(direction);
                _facingDirection = _runDirection;
                lastPressedTime = float.NegativeInfinity;
                return;
            }

            lastPressedTime = pressedTime;
        }
    }
}