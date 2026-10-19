using UnityEngine;

namespace Project_Chronicles.Player.StateMachine
{
    public sealed class HitStunState : StateBase
    {
        public HitStunState(PlayerController controller) : base(controller)
        {
            Priority = 3;
            StateName = "HitStun";
        }

        public override PlayerState StateType => PlayerState.HITSTUN;
        public override bool AllowsMovement => false;
        public override bool AllowsJump => false;
        public float RemainingTime { get; private set; }

        internal void Extend(float duration) =>
            RemainingTime = duration <= 0f ? 0f : Mathf.Max(RemainingTime, duration);
        public override bool CanEnter() => RemainingTime > 0f;
        public override bool CanExit(StateBase next) => RemainingTime <= 0f || next.Priority > Priority;

        public override void Enter()
        {
            var input = Controller.GetComponent<PlayerInputListener>();
            input.CancelJump();
            input.CancelDash();
            Controller.HorizontalMovement.Stop();
            base.Enter();
        }

        public override void Update() =>
            RemainingTime = Mathf.Max(0f, RemainingTime - Controller.StateMachine.DeltaTime);
        public override void Exit() => RemainingTime = 0f;
    }
}
