using UnityEngine;

namespace Project_Chronicles.Character
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterHorizontalMovement), typeof(CharacterVerticalMovement))]
    public sealed class CharacterStateMachine : MonoBehaviour
    {
        private CharacterHorizontalMovement _movement;
        private CharacterVerticalMovement _verticalMovement;
        private Rigidbody2D _body;

        public CharacterState CurrentState { get; private set; } = CharacterState.IDLE;

        private void Awake()
        {
            _movement = GetComponent<CharacterHorizontalMovement>();
            _verticalMovement = GetComponent<CharacterVerticalMovement>();
            _body = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            RefreshState();
        }

        public CharacterState RefreshState()
        {
            // An upward-moving character has already jumped even if the ground cast still hits.
            if (_body.linearVelocity.y > 0f || !_verticalMovement.IsGrounded)
            {
                CurrentState = _verticalMovement.WasDashJump
                    ? CharacterState.DASH_JUMP : CharacterState.JUMP;
            }
            else
            {
                CurrentState = ResolveState(_movement.HorizontalVelocity, _movement.IsRunning);
            }

            return CurrentState;
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
