using UnityEngine;
using Project_Chronicles.Common;

namespace Project_Chronicles.Character
{
    [RequireComponent(typeof(CharacterStateMachine))]
    public class CharacterAnimator : BaseAnimator
    {
        protected readonly int _walkHash = Animator.StringToHash("IsWalk");
        protected readonly int _runHash = Animator.StringToHash("IsRun");
        protected readonly int _jumpHash = Animator.StringToHash("Jump");

        private CharacterStateMachine _stateMachine;
        private CharacterHorizontalMovement _movement;

        protected override void Awake()
        {
            base.Awake();
            _stateMachine = GetComponent<CharacterStateMachine>();
            _movement = GetComponent<CharacterHorizontalMovement>();
        }

        private void LateUpdate()
        {
            CharacterState state = _stateMachine.CurrentState;
            SetMovement(state == CharacterState.WALK, state == CharacterState.RUN);
            _spriteRenderer.flipX = _movement.FacingDirection < 0f;
        }

        public void SetMovement(bool isWalking, bool isRunning)
        {
            _animator.SetBool(_walkHash, isWalking);
            _animator.SetBool(_runHash, isRunning);
        }

        public void Jump()
        {
            _animator.SetTrigger(_jumpHash);
        }
    }
}
