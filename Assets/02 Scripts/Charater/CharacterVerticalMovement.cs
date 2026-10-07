using UnityEngine;

namespace Project_Chronicles.Character
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInputListener), typeof(Collider2D), typeof(Rigidbody2D))]
    public sealed class CharacterVerticalMovement : MonoBehaviour
    {
        public const float PrototypeJumpHeight = 4f;
        public const float PrototypeTimeToApex = 0.4f;
        public const float PrototypeCoyoteTime = 0.08f;
        public const float PrototypeJumpBufferTime = 0.08f;
        public const float PrototypeFastFallSpeed = 6f;

        private const float GroundCheckDistance = 0.08f;
        private const float MinimumTimeToApex = 0.01f;

        [SerializeField, Min(0f)]
        private float _jumpHeight = PrototypeJumpHeight;

        [SerializeField, Min(MinimumTimeToApex)]
        private float _timeToApex = PrototypeTimeToApex;

        [SerializeField, Min(0f)]
        private float _coyoteTime = PrototypeCoyoteTime;

        [SerializeField, Min(0f)]
        private float _jumpBufferTime = PrototypeJumpBufferTime;

        [SerializeField, Min(0f)]
        private float _fastFallSpeed = PrototypeFastFallSpeed;

        [SerializeField]
        private LayerMask _groundLayers = Physics2D.AllLayers;

        private readonly RaycastHit2D[] _groundHits = new RaycastHit2D[4];
        private PlayerInputListener _input;
        private Rigidbody2D _body;
        private Collider2D _playerCollider;
        private bool _jumpConsumedSinceGrounded;
        private float _lastGroundedTime = float.NegativeInfinity;
        private bool _wasDashJump;

        public float JumpHeight => _jumpHeight;
        public float TimeToApex => _timeToApex;
        public float CoyoteTime => _coyoteTime;
        public float JumpBufferTime => _jumpBufferTime;
        public float FastFallSpeed => _fastFallSpeed;
        public bool IsGrounded => CheckGrounded();
        public bool WasDashJump => _wasDashJump;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _playerCollider = GetComponent<Collider2D>();
            _input = GetComponent<PlayerInputListener>();
            _body.gravityScale = CalculateGravityScale(_jumpHeight, _timeToApex, Physics2D.gravity.y);
        }

        private static float CalculateGravityScale(float hegiht, float apexTime, float worldGravityY)
        {
            float gravityMagnitude = Mathf.Abs(worldGravityY);
            if(gravityMagnitude <= Mathf.Epsilon)
            {
                return 0f;
            }
            float safeHeight = Mathf.Max(0f, hegiht);
            float safeApexTime = Mathf.Max(MinimumTimeToApex, apexTime);
            float jumpGravity = 2f * safeHeight / (safeApexTime * safeApexTime);
            return jumpGravity / gravityMagnitude;
        }

        public void FixedUpdate()
        {
            bool isGrounded = CheckGrounded();
            if (isGrounded && _body.linearVelocity.y <= 0f)
            {
                _lastGroundedTime = Time.fixedTime;
                _jumpConsumedSinceGrounded = false;
                _wasDashJump = false;
            }
            float timeSinceJumpRequested = Time.time - _input.LastJumpRequestedTime;
            bool isJumpHeld = _input.VerticalInput > 0f;
            if (!isJumpHeld && !IsJumpBuffered(timeSinceJumpRequested, _jumpBufferTime))
            {
                return;
            }
            float timeSinceGrounded = Time.fixedTime - _lastGroundedTime;
            if (!CanJump(timeSinceGrounded, _coyoteTime, _jumpConsumedSinceGrounded))
            {
                return;
            }
            _input.CancelJump();
            _jumpConsumedSinceGrounded = true;
            _wasDashJump = _input.IsDashing;
            Vector2 velocity = _body.linearVelocity;
            velocity.y = CalculateJumpVelocity(_jumpHeight, _timeToApex);
            _body.linearVelocity = velocity;
        }

        public static float CalculateJumpVelocity(float height, float apexTime)
        {
            float safeHeight = Mathf.Max(0f, height);
            float safeApexTime = Mathf.Max(MinimumTimeToApex, apexTime);
            return 2f * safeHeight / safeApexTime;
        }

        public static bool IsJumpBuffered(float timeSinceJumpRequested, float allowedBufferTime)
        {
            float safeBufferTime = Mathf.Max(0f, allowedBufferTime);
            return timeSinceJumpRequested >= 0f && timeSinceJumpRequested <= safeBufferTime;
        }

        public static bool CanJump(float timeSinceGrounded, float allowedCoyoteTime, bool jumpAlreadyConsumed)
        {
            if (jumpAlreadyConsumed)
            {
                return false;
            }
            float safeCoyoteTime = Mathf.Max(0f, allowedCoyoteTime);
            return timeSinceGrounded >= 0f && timeSinceGrounded <= safeCoyoteTime;
        }

        private bool CheckGrounded()
        {
            var contactFilter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = _groundLayers,
                useTriggers = false,
            };
            return _playerCollider.Cast(Vector2.down, contactFilter, _groundHits, GroundCheckDistance) > 0;
        }
    }
}
