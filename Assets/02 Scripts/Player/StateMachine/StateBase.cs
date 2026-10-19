namespace Project_Chronicles.Player.StateMachine
{
    public abstract class StateBase
    {
        public string StateName { get; protected set; }
        public int Priority { get; protected set; }
        public abstract PlayerState StateType { get; }
        public virtual bool AllowsMovement => true;
        public virtual bool AllowsJump => true;
        protected readonly PlayerController Controller;

        protected StateBase(PlayerController controller) => Controller = controller;

        public virtual bool CanEnter() => Controller.StateMachine.GetLocomotionState() == StateType;
        public virtual bool CanExit(StateBase next) => true;
        public virtual void Enter() => Controller.Animator.PlayLocomotion(StateType);
        public virtual void Update() { }
        public virtual void FixedUpdate() { }
        public virtual void Exit() { }
    }
}
