using System.Collections.Generic;
using Project_Chronicles.Player;
using Project_Chronicles.Player.Combat;
using UnityEngine;

namespace Project_Chronicles.Monster.StateMachine
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MonsterController))]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class MonsterStateMachine : MonoBehaviour
    {
        [Header("Awareness")]
        [SerializeField, Min(0f)] private float _awarenessRadius = 5f;
        [SerializeField] private LayerMask _playerLayers = Physics2D.AllLayers;

        [Header("Knockdown")]
        [SerializeField, Min(0f)] private float _knockdownDuration = 1f;

        private readonly List<Collider2D> _visibleColliders = new List<Collider2D>();
        private MonsterController _controller;
        private Rigidbody2D _body;
        private PlayerController _target;
        private float _knockdownRemaining;
        private bool _hasEntered;

        public MonsterState CurrentState { get; private set; } = MonsterState.IDLE;
        public PlayerController Target => _target;
        public bool CanMove => isActiveAndEnabled && CurrentState == MonsterState.CHASE;

        private void Awake()
        {
            _controller = GetComponent<MonsterController>();
            _controller.InitializeComponents();
            _body = GetComponent<Rigidbody2D>();
        }

        private void Start() => EnsureEntered();

        private void Update()
        {
            if (Time.timeScale <= 0f) return;
            Tick(Time.deltaTime);
        }

        public void Tick(float deltaTime)
        {
            if (!isActiveAndEnabled || deltaTime < 0f
                || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return;

            EnsureEntered();
            switch (CurrentState)
            {
                case MonsterState.KNOCKDOWN:
                    _knockdownRemaining = Mathf.Max(0f, _knockdownRemaining - deltaTime);
                    if (_knockdownRemaining <= 0f) ChangeState(MonsterState.IDLE);
                    break;
                case MonsterState.IDLE:
                    _target = FindVisiblePlayer();
                    if (_target != null) ChangeState(MonsterState.AWARENESS);
                    break;
                case MonsterState.AWARENESS:
                    ChangeState(IsTargetVisible() ? MonsterState.CHASE : MonsterState.IDLE);
                    break;
                case MonsterState.CHASE:
                    if (!IsTargetVisible()) ChangeState(MonsterState.IDLE);
                    else _controller.Movement.SetDestination(_target.transform.position);
                    break;
            }
        }

        private void FixedUpdate()
        {
            EnsureEntered();
            // Recheck before physics so a lost target cannot cause another movement step.
            if (CurrentState == MonsterState.CHASE)
            {
                if (!IsTargetVisible()) ChangeState(MonsterState.IDLE);
                else _controller.Movement.SetDestination(_target.transform.position);
            }
            _controller.Movement.TickHorizontalMovement(CanMove);
        }

        // The attack's hit detection calls this once for each confirmed hit.
        public void ReceiveHit(AttackData attackData)
        {
            if (!isActiveAndEnabled || attackData == null || !attackData.ForceKnockDown) return;

            _knockdownRemaining = _knockdownDuration;
            // Repeated knockdown hits refresh the timer without restarting the animation.
            ChangeState(MonsterState.KNOCKDOWN);
        }

        private void EnsureEntered()
        {
            if (!_hasEntered) ChangeState(MonsterState.IDLE);
        }

        private void ChangeState(MonsterState next)
        {
            if (_hasEntered && CurrentState == next) return;
            _hasEntered = true;
            CurrentState = next;

            switch (next)
            {
                case MonsterState.IDLE:
                    _target = null;
                    StopHorizontalMovement();
                    _controller.Animator.Idle();
                    break;
                case MonsterState.AWARENESS:
                    StopHorizontalMovement();
                    break;
                case MonsterState.CHASE:
                    _controller.Movement.SetDestination(_target.transform.position);
                    _controller.Animator.Run();
                    break;
                case MonsterState.KNOCKDOWN:
                    _target = null;
                    StopHorizontalMovement();
                    _controller.Animator.Die();
                    break;
            }
        }

        private PlayerController FindVisiblePlayer()
        {
            var filter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = _playerLayers,
                useTriggers = true,
            };
            Physics2D.OverlapCircle(_body.position, _awarenessRadius, filter, _visibleColliders);

            PlayerController nearest = null;
            float nearestDistance = float.PositiveInfinity;
            foreach (Collider2D candidate in _visibleColliders)
            {
                PlayerController player = candidate.GetComponentInParent<PlayerController>();
                if (player == null || !player.isActiveAndEnabled) continue;
                float distance = ((Vector2)player.transform.position - _body.position).sqrMagnitude;
                if (distance > _awarenessRadius * _awarenessRadius || distance >= nearestDistance) continue;
                nearest = player;
                nearestDistance = distance;
            }
            return nearest;
        }

        private bool IsTargetVisible()
        {
            if (_target == null || !_target.isActiveAndEnabled) return false;
            if (((Vector2)_target.transform.position - _body.position).sqrMagnitude
                > _awarenessRadius * _awarenessRadius) return false;

            // Confirm its collider is still enabled and on one of the configured layers.
            var filter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = _playerLayers,
                useTriggers = true,
            };
            Physics2D.OverlapCircle(_body.position, _awarenessRadius, filter, _visibleColliders);
            foreach (Collider2D candidate in _visibleColliders)
            {
                if (candidate.GetComponentInParent<PlayerController>() == _target) return true;
            }
            return false;
        }

        private void StopHorizontalMovement()
        {
            if (_body == null) return;
            Vector2 velocity = _body.linearVelocity;
            velocity.x = 0f;
            _body.linearVelocity = velocity;
        }

        private void OnDisable()
        {
            StopHorizontalMovement();
            _target = null;
            _knockdownRemaining = 0f;
            _hasEntered = false;
            CurrentState = MonsterState.IDLE;
            _visibleColliders.Clear();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _awarenessRadius);
        }
    }
}
