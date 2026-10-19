using Project_Chronicles.Player.StateMachine;
using UnityEngine;

namespace Project_Chronicles.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerAnimator), typeof(PlayerAttackController))]
    [RequireComponent(typeof(PlayerHorizontalMovement), typeof(PlayerVerticalMovement))]
    [RequireComponent(typeof(PlayerStateMachine))]
    public sealed class PlayerController : MonoBehaviour
    {
        public PlayerAnimator Animator;
        public PlayerAttackController AttackController;
        public PlayerHorizontalMovement HorizontalMovement;
        public PlayerVerticalMovement VerticalMovement;
        public PlayerStateMachine StateMachine;

        private void Awake() => InitializeComponents();

        internal void InitializeComponents()
        {
            Animator = GetComponent<PlayerAnimator>();
            AttackController = GetComponent<PlayerAttackController>();
            StateMachine = GetComponent<PlayerStateMachine>();
            HorizontalMovement = GetComponent<PlayerHorizontalMovement>();
            VerticalMovement = GetComponent<PlayerVerticalMovement>();
        }

        public T GetState<T>() where T : StateBase => StateMachine.GetState<T>();
    }
}
