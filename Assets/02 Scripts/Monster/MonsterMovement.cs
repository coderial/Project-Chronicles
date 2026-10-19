using UnityEngine;

namespace Project_Chronicles.Monster
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class MonsterMovement : MonoBehaviour
    {
        public const float PrototypeMoveSpeed = 4f;
        public const float PrototypeStoppingDistance = 0.2f;
        public const float PrototypeJumpHeight = 4f;
        public const float PrototypeTimeToApex = 0.4f;
        private const float MinimumTimeToApex = 0.01f;
        private const float GroundCheckDistance = 0.08f;

        [SerializeField, Min(0f)]
        private float _moveSpeed = PrototypeMoveSpeed;

        [SerializeField, Min(0f)]
        private float _jumpHeight = PrototypeJumpHeight;

        [SerializeField, Min(MinimumTimeToApex)]
        private float _timeToApex = PrototypeTimeToApex;

        [SerializeField, Min(0f)]
        private float _stoppingDistanceX = PrototypeStoppingDistance;

        private Rigidbody2D _body;
        private Collider2D _collider;
        private Vector2 _destination = Vector2.zero;

        [SerializeField]
        private LayerMask _groundLayers = Physics2D.AllLayers;

        private readonly RaycastHit2D[] _groundHits = new RaycastHit2D[4];

        public bool IsGrounded => CheckGrounded();


        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _collider = GetComponent<Collider2D>();
            _body.gravityScale = CalculateGravityScale(_jumpHeight, _timeToApex, Physics2D.gravity.y);
        }

        public void SetDestination(Vector2 destination)
        {
            _destination = destination;
        }

        public void TickHorizontalMovement(bool canMove)
        {
            if (!isActiveAndEnabled) return;
            Vector2 velocity = _body.linearVelocity;
            bool canWalk = canMove && velocity.y <= 0f && IsGrounded && !IsArrivedAtPosX();
            float direction = Mathf.Sign(_destination.x - _body.position.x);
            float remainingDistance = Mathf.Abs(_destination.x - _body.position.x);
            float speed = Mathf.Min(_moveSpeed, remainingDistance / Time.fixedDeltaTime);
            velocity.x = canWalk ? CalculateHorizontalVelocity(direction, speed) : 0f;
            _body.linearVelocity = velocity;
        }

        private void FixedUpdate()
        {
            // Keep horizontal movement blocked throughout ascent and descent,
            // even when the movement ticks are not called while airborne.
            if (_body.linearVelocity.y > 0f || !IsGrounded)
            {
                Vector2 velocity = _body.linearVelocity;
                velocity.x = 0f;
                _body.linearVelocity = velocity;
            }
        }

        private bool IsArrivedAtPosX()
        {
            return Mathf.Abs(_destination.x - _body.position.x) <= _stoppingDistanceX;
        }

        public void TickVerticalMovement(bool canJump)
        {
            if (!isActiveAndEnabled) return;
            if (!canJump || !IsGrounded || _body.linearVelocity.y > 0f) return;

            // Clear walking momentum before applying the same impulse as the player.
            _body.linearVelocity = new Vector2(0f, CalculateJumpVelocity(_jumpHeight, _timeToApex));
        }

        public static float CalculateJumpVelocity(float height, float apexTime)
        {
            float safeHeight = Mathf.Max(0f, height);
            float safeApexTime = Mathf.Max(MinimumTimeToApex, apexTime);
            return 2f * safeHeight / safeApexTime;
        }

        public static float CalculateHorizontalVelocity(float destX, float speed)
        {
            return Mathf.Clamp(destX, -1f, 1f) * Mathf.Max(0f, speed);
        }
        private static float CalculateGravityScale(float hegiht, float apexTime, float worldGravityY)
        {
            float gravityMagnitude = Mathf.Abs(worldGravityY);
            if (gravityMagnitude <= Mathf.Epsilon)
            {
                return 0f;
            }
            float safeHeight = Mathf.Max(0f, hegiht);
            float safeApexTime = Mathf.Max(MinimumTimeToApex, apexTime);
            float jumpGravity = 2f * safeHeight / (safeApexTime * safeApexTime);
            return jumpGravity / gravityMagnitude;
        }

        private bool CheckGrounded()
        {
            var contactFilter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = _groundLayers,
                useTriggers = false,
            };
            return _collider.Cast(Vector2.down, contactFilter, _groundHits, GroundCheckDistance) > 0;
        }

        private bool CheckPlatformAboveHead()
        {
            var contactFilter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = _groundLayers,
                useTriggers = false,
            };
            return _collider.Cast(Vector2.up, contactFilter, _groundHits, _jumpHeight / 2) > 0;
        }
    }
}
