namespace Project_Chronicles.Player
{
    public class WalkState : IState
    {
        private PlayerAnimator _animator;
        public void Initialize(PlayerAnimator animator)
        {
            _animator = animator;
        }
        public void Enter()
        {
            _animator.SetMovement(isWalking: true, isRunning: false);
        }

        public void Exit()
        {

        }
        public void Update()
        {

        }
    }
}