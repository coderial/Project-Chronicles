namespace Project_Chronicles.Player.StateMachine
{
    public sealed class JumpState : StateBase
    {
        public JumpState(PlayerController controller) : base(controller)
        {
            Priority = 1;
            StateName = "Jump";
        }

        public override PlayerState StateType => Controller.VerticalMovement.WasDashJump
            ? PlayerState.DASH_JUMP : PlayerState.JUMP;
    }
}
