using Project_Chronicles.Character.StateMachine;
using UnityEngine;

namespace Project_Chronicles.Character
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterAnimator), typeof(CharacterAttackController))]
    [RequireComponent(typeof(CharacterHorizontalMovement), typeof(CharacterVerticalMovement))]
    [RequireComponent(typeof(CharacterStateMachine))]
    public sealed class CharacterController : MonoBehaviour
    {
        public CharacterAnimator Animator;
        public CharacterAttackController AttackController;
        public CharacterHorizontalMovement HorizontalMovement;
        public CharacterVerticalMovement VerticalMovement;
        public CharacterStateMachine StateMachine;

        private void Awake() => InitializeComponents();

        internal void InitializeComponents()
        {
            Animator = GetComponent<CharacterAnimator>();
            AttackController = GetComponent<CharacterAttackController>();
            StateMachine = GetComponent<CharacterStateMachine>();
            HorizontalMovement = GetComponent<CharacterHorizontalMovement>();
            VerticalMovement = GetComponent<CharacterVerticalMovement>();
        }

        public T GetState<T>() where T : StateBase => StateMachine.GetState<T>();
    }
}
