using UnityEngine;

namespace Project_Chronicles.Character
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(PlayerInputListener))]
    public sealed class CharacterHorizontalMovement : MonoBehaviour
    {
        public const float PrototypeWalkSpeed = 6f;
        public const float PrototypeRunSpeed = 12f;

        [SerializeField, Min(0f)]
        private float _walkSpeed = PrototypeWalkSpeed;

        [SerializeField, Min(0f)]
        private float _runSpeed = PrototypeRunSpeed;

        private PlayerInputListener _input;
        private Rigidbody2D _body;

        public float WalkSpeed => _walkSpeed;
        public float RunSpeed => _runSpeed;
        public float HorizontalVelocity => _body.linearVelocity.x;
        public float FacingDirection { get; private set; } = 1f;
        public bool IsRunning => isActiveAndEnabled && _input.IsDashing;

        private void Awake()
        {
            _input = GetComponent<PlayerInputListener>();
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
            _input.CancelDash();
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
