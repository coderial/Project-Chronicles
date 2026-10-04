namespace Project_Chronicles.Player
{
    public interface IState
    {
        public void Initialize(PlayerAnimator animator);
        public void Enter();
        public void Update();
        public void Exit();
    }
}