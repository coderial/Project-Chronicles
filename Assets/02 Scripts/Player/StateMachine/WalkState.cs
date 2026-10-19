namespace Project_Chronicles.Player.StateMachine
{
    public sealed class WalkState : StateBase
    {
        public WalkState(PlayerController controller) : base(controller)
        {
            Priority = 1;
            StateName = "Walk";
        }

        public override PlayerState StateType => PlayerState.WALK;
    }
}
