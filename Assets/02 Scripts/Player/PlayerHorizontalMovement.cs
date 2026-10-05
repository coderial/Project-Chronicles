using UnityEngine;

namespace Project_Chronicles.Player
{
    [RequireComponent(typeof(Rigidbody2D), typeof(PlayerMovementInput))]
    public sealed class PlayerHorizontalMovement : MonoBehaviour
    {
        public const float PrototypeWalkSpeed = 6f;
        public const float PrototypeRunSpeed = 12f;

        [SerializeField, Min(0f)]
        private float _walkSpeed = PrototypeWalkSpeed;

        [SerializeField, Min(0f)]
        private float _runSpeed = PrototypeRunSpeed;

        private PlayerMovementInput _input;
        private Rigidbody2D _body;

        public float WalkSpeed => _walkSpeed;
        public float RunSpeed => _runSpeed;
        public float HorizontalVelocity => _body.linearVelocity.x;
        public float FacingDirection { get; private set; } = 1f;
        public bool IsRunning => isActiveAndEnabled && _input.IsRunning;

        private void Awake()
        {
            _input = GetComponent<PlayerMovementInput>();
            _body = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            float horizontalInput = _input.HorizontalInput;
            if (horizontalInput != 0f)
            {
                FacingDirection = Mathf.Sign(horizontalInput);
            }
        }

        private void FixedUpdate()
        {
            float speed = IsRunning ? _runSpeed : _walkSpeed;
            Vector2 velocity = _body.linearVelocity;
            velocity.x = CalculateHorizontalVelocity(_input.HorizontalInput, speed);
            _body.linearVelocity = velocity;
        }

        private void OnDisable()
        {
            _input.CancelRun();
            Vector2 velocity = _body.linearVelocity;
            velocity.x = 0f;
            _body.linearVelocity = velocity;
        }

        public static float CalculateHorizontalVelocity(float input, float speed)
        {
            return Mathf.Clamp(input, -1f, 1f) * Mathf.Max(0f, speed);
        }
    }
}
