namespace Project_Chronicles.Character.StateMachine
{
    public sealed class IdleState : StateBase
    {
        public IdleState(CharacterController controller) : base(controller)
        {
            Priority = 0;
            StateName = "Idle";
        }

        public override CharacterState StateType => CharacterState.IDLE;
    }
}
