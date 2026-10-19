namespace Project_Chronicles.Player.StateMachine
{
    public sealed class FallState : StateBase
    {
        public FallState(PlayerController controller) : base(controller)
        {
            Priority = 1;
            StateName = "Fall";
        }

        public override PlayerState StateType => PlayerState.FALL;
    }
}
