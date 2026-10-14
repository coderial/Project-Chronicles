namespace Project_Chronicles.Character.StateMachine
{
    public sealed class WalkState : StateBase
    {
        public WalkState(CharacterController controller) : base(controller)
        {
            Priority = 1;
            StateName = "Walk";
        }

        public override CharacterState StateType => CharacterState.WALK;
    }
}
