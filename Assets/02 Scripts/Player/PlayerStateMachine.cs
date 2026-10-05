using UnityEngine;

namespace Project_Chronicles.Player
{
    [RequireComponent(typeof(PlayerHorizontalMovement))]
    public sealed class PlayerStateMachine : MonoBehaviour
    {
        private PlayerHorizontalMovement _movement;

        public PlayerState CurrentState { get; private set; } = PlayerState.IDLE;

        private void Awake()
        {
            _movement = GetComponent<PlayerHorizontalMovement>();
        }

        private void Update()
        {
            CurrentState = ResolveState(_movement.HorizontalVelocity, _movement.IsRunning);
        }

        private void OnDisable()
        {
            CurrentState = PlayerState.IDLE;
        }

        public static PlayerState ResolveState(float horizontalVelocity, bool isRunning)
        {
            if (Mathf.Approximately(horizontalVelocity, 0f))
            {
                return PlayerState.IDLE;
            }

            return isRunning ? PlayerState.RUN : PlayerState.WALK;
        }
    }
}
