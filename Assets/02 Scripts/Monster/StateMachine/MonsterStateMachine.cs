using UnityEngine;

namespace Project_Chronicles.Monster.StateMachine
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MonsterController))]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class MonsterStateMachine : MonoBehaviour
    {
        private MonsterController _controller;
        private Rigidbody2D _body;
        private MonsterState _currentState = MonsterState.IDLE;
        [SerializeField]
        private float _chaseRange = 3f;
        [SerializeField]
        private Transform _target;
        public float DeltaTime { get; private set; }

        private void Awake()
        {
            _controller = GetComponent<MonsterController>();
            _controller.InitializeComponents();
            _body = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            if (Time.timeScale <= 0f)
            {
                return;
            }
            Tick(Time.deltaTime);
        }
        private bool IsTargetInRange(float range)
        {
            if (_target == null) return false;
            if (Vector2.Distance(transform.position, _target.position) < _chaseRange)
            {
                return true;
            }
            return false;
        }
        private void Tick(float deltaTime)
        {
            if (!isActiveAndEnabled || deltaTime < 0f) return;
            DeltaTime = deltaTime;

        }
    }
}