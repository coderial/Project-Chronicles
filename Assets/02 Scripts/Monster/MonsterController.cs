using UnityEngine;
using Project_Chronicles.Monster.StateMachine;

namespace Project_Chronicles.Monster
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(WizardAnimator), typeof(MonsterStateMachine))]
    [RequireComponent(typeof(MonsterMovement))]
    public sealed class MonsterController : MonoBehaviour
    {
        // 테스트용 애니메이터
        public WizardAnimator Animator;
        public MonsterStateMachine StateMachine;
        public MonsterMovement Movement;

        private void Awake() => InitializeComponents();

        internal void InitializeComponents()
        {
            Animator = GetComponent<WizardAnimator>();
            StateMachine = GetComponent<MonsterStateMachine>();
            Movement = GetComponent<MonsterMovement>();
        }
    }
}