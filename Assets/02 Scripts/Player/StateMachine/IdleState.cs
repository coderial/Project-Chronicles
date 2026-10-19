namespace Project_Chronicles.Player.StateMachine
{
    public sealed class IdleState : StateBase
    {
        public IdleState(PlayerController controller) : base(controller)
        {
            Priority = 0;
            StateName = "Idle";
        }

        public override PlayerState StateType => PlayerState.IDLE;
    }
}
