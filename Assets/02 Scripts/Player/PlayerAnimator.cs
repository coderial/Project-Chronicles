using UnityEngine;
using Project_Chronicles.Common;

namespace Project_Chronicles.Player
{
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimator : BaseAnimator
    {
        protected readonly int _walkHash = Animator.StringToHash("IsWalk");
        protected readonly int _runHash = Animator.StringToHash("IsRun");
        protected readonly int _jumpHash = Animator.StringToHash("Jump");
        protected override void Awake()
        {
            base.Awake();
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