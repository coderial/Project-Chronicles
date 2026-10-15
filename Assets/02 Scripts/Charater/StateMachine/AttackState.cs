namespace Project_Chronicles.Character.StateMachine
{
    public sealed class AttackState : StateBase
    {
        public AttackState(CharacterController controller) : base(controller)
        {
            Priority = 2;
            StateName = "Attack";
        }

        public override CharacterState StateType => CharacterState.ATTACK;
        public override bool AllowsMovement => Controller.AttackController.IsAttacking
            && !Controller.StateMachine.IsGrounded
            && !Controller.AttackController.CurrentAttack.LockUntilLanding
            && Controller.HorizontalMovement.IsMovingForward;
        public override bool AllowsJump => false;

        private bool WantsMovementCancel => Controller.AttackController.CanCancel
            && Controller.StateMachine.IsGrounded
            && (Controller.StateMachine.GetLocomotionState() == CharacterState.WALK
                || Controller.StateMachine.GetLocomotionState() == CharacterState.RUN);

        public override bool CanEnter() => Controller.AttackController.HasPreparedAttack
            || (Controller.AttackController.IsAttacking && !WantsMovementCancel);

        public override bool CanExit(StateBase next) => !Controller.AttackController.IsAttacking
            || next.Priority > Priority
            || (WantsMovementCancel && (next.StateType == CharacterState.WALK || next.StateType == CharacterState.RUN));

        public override void Enter() => Controller.AttackController.BeginPreparedAttack();
        public override void Update() => Controller.AttackController.TickAttack(Controller.StateMachine.DeltaTime);
        public override void Exit() => Controller.AttackController.CancelAttack(preserveCompletedMovement: true);
    }
}
