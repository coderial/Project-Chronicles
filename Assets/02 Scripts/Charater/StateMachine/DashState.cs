namespace Project_Chronicles.Character.StateMachine
{
    public sealed class DashState : StateBase
    {
        public DashState(CharacterController controller) : base(controller)
        {
            Priority = 1;
            StateName = "Dash";
        }

        public override CharacterState StateType => CharacterState.RUN;
    }
}
