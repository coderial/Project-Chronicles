namespace Project_Chronicles.Player
{
    public class RunState : IState
    {
        private PlayerAnimator _animator;
        public void Initialize(PlayerAnimator animator)
        {
            _animator = animator;
        }
        public void Enter()
        {
            _animator.SetMovement(isWalking: false, isRunning: true);
        }

        public void Exit()
        {

        }
        public void Update()
        {

        }
    }
}