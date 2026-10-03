using UnityEngine;

namespace Project_Chronicles.Player
{
    public sealed class PlayerStateMachine : MonoBehaviour
    {
        public IState CurrentState { get; private set; }

        //private PlayerState _currentState = PlayerState.IDLE;
        private PlayerHorizontalMovement _playerHorizontalMovement;
        private PlayerAnimator _animator;
        private Rigidbody2D _body;

        //public PlayerState CurrentState => _currentState;

        private void Awake()
        {
            _playerHorizontalMovement = GetComponent<PlayerHorizontalMovement>();
            _body = GetComponent<Rigidbody2D>();
            _animator = GetComponent<PlayerAnimator>();
            InitializeStates();
        }

        private void InitializeStates()
        {

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

        public void FixedUpdate()
        {
            CurrentState.Update();
        }
    }
}