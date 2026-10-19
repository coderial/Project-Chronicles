namespace Project_Chronicles.Player.StateMachine
{
    public sealed class DashState : StateBase
    {
        public DashState(PlayerController controller) : base(controller)
        {
            Priority = 1;
            StateName = "Dash";
        }

        public override PlayerState StateType => PlayerState.RUN;
    }
}
