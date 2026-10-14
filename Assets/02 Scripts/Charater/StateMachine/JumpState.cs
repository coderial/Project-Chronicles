namespace Project_Chronicles.Character.StateMachine
{
    public sealed class JumpState : StateBase
    {
        public JumpState(CharacterController controller) : base(controller)
        {
            Priority = 1;
            StateName = "Jump";
        }

        public override CharacterState StateType => Controller.VerticalMovement.WasDashJump
            ? CharacterState.DASH_JUMP : CharacterState.JUMP;
    }
}
