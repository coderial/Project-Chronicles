namespace Project_Chronicles.Character.StateMachine
{
    public sealed class FallState : StateBase
    {
        public FallState(CharacterController controller) : base(controller)
        {
            Priority = 1;
            StateName = "Fall";
        }

        public override CharacterState StateType => CharacterState.FALL;
    }
}
