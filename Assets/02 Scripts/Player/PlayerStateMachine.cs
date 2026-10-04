using UnityEngine;

namespace Project_Chronicles.Player
{
    public sealed class PlayerStateMachine : MonoBehaviour
    {
        public IState CurrentState { get; private set; }

        private IdleState _idle;
        private RunState _run;
        private WalkState _walk;

        private PlayerHorizontalMovement _playerHorizontalMovement;
        private PlayerAnimator _animator;
        private Rigidbody2D _body;

        //public PlayerState CurrentState => _currentState;

        private void Awake()
        {
            _playerHorizontalMovement = GetComponent<PlayerHorizontalMovement>();
            _body = GetComponent<Rigidbody2D>();
            _animator = GetComponent<PlayerAnimator>();
            InstantiateState();
            InitializeStates();
        }

        private void Start()
        {
            ChangeState(_idle);
        }

        private void InstantiateState()
        {
            _idle = new IdleState();
            _run = new RunState();
            _walk = new WalkState();
        }

        private void InitializeStates()
        {
            _idle.Initialize(_animator);
            _run.Initialize(_animator);
            _walk.Initialize(_animator);
        }

        public void ChangeState(IState newState)
        {
            if (newState == null || ReferenceEquals(CurrentState, newState))
            {
                return;
            }
            CurrentState?.Exit();
            CurrentState = newState;
            CurrentState.Enter();
        }

        public void Update()
        {
            if (_body.linearVelocity == Vector2.zero)
            {
                ChangeState(_idle);
            }
            else
            {
                if (_playerHorizontalMovement.IsRuning)
                {
                    ChangeState(_run);
                }
                else
                {
                    ChangeState(_walk);
                }
            }
        }

        public void FixedUpdate()
        {
            CurrentState.Update();
        }
    }
}