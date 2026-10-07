using UnityEngine;

namespace Project_Chronicles.Character
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterHorizontalMovement))]
    public sealed class CharacterStateMachine : MonoBehaviour
    {
        private CharacterHorizontalMovement _movement;

        public CharacterState CurrentState { get; private set; } = CharacterState.IDLE;

        private void Awake()
        {
            _movement = GetComponent<CharacterHorizontalMovement>();
        }

        private void Update()
        {
            CurrentState = ResolveState(_movement.HorizontalVelocity, _movement.IsRunning);
        }

        private void OnDisable()
        {
            CurrentState = CharacterState.IDLE;
        }

        public static CharacterState ResolveState(float horizontalVelocity, bool isRunning)
        {
            if (Mathf.Approximately(horizontalVelocity, 0f))
            {
                return CharacterState.IDLE;
            }

            return isRunning ? CharacterState.RUN : CharacterState.WALK;
        }
    }
}
