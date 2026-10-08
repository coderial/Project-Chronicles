using UnityEngine;
using Project_Chronicles.Common;
using Project_Chronicles.Character.Combat;
using Project_Chronicles.Utils;

namespace Project_Chronicles.Character
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterStateMachine))]
    public class CharacterAnimator : BaseAnimator
    {
        protected readonly int _walkHash = Animator.StringToHash("IsWalk");
        protected readonly int _runHash = Animator.StringToHash("IsRun");
        protected readonly int _jumpHash = Animator.StringToHash("Jump");
        private readonly int _attackProgressHash = Animator.StringToHash("AttackProgress");

        [Header("Locomotion Animator States")]
        [SerializeField] private string _idleAnimation = "Base Layer.Theodore_Idle";
        [SerializeField] private string _walkAnimation = "Base Layer.Theodore_Walk";
        [SerializeField] private string _runAnimation = "Base Layer.Theodore_Run";
        private bool _isPlayingAttack;

        private CharacterStateMachine _stateMachine;
        private CharacterHorizontalMovement _movement;

        protected override void Awake()
        {
            base.Awake();
            _stateMachine = GetComponent<CharacterStateMachine>();
            _movement = GetComponent<CharacterHorizontalMovement>();
        }

        private void LateUpdate()
        {
            CharacterState state = _stateMachine.CurrentState;
            SetMovement(state == CharacterState.WALK, state == CharacterState.RUN);
            _spriteRenderer.flipX = _movement.FacingDirection < 0f;
        }

        public void SetMovement(bool isWalking, bool isRunning)
        {
            _animator.SetBool(_walkHash, isWalking);
            _animator.SetBool(_runHash, isRunning);
        }

        public void Jump()
        {
            _animator.SetTrigger(_jumpHash);
        }

        public void PlayAttack(AttackData attack)
        {
            int stateHash = Animator.StringToHash(attack.Animation);
            if (string.IsNullOrWhiteSpace(attack.Animation) || !_animator.HasState(0, stateHash))
            {
                Logging.LogError($"[CharacterAnimator] : Missing attack state '{attack.Animation}' for {attack.AttackId}");
                return;
            }

            _isPlayingAttack = true;
            SetAttackProgress(0f);
            _animator.Play(stateHash, 0, 0f);
        }

        public void SetAttackProgress(float normalizedTime)
        {
            if (_isPlayingAttack)
            {
                // Attack states use Motion Time = AttackProgress; the combat clock owns playback.
                _animator.SetFloat(_attackProgressHash, Mathf.Clamp01(normalizedTime));
            }
        }

        public void EndAttack()
        {
            if (!_isPlayingAttack) return;
            _isPlayingAttack = false;

            CharacterState state = _stateMachine.CurrentState;
            SetMovement(state == CharacterState.WALK, state == CharacterState.RUN);
            string animation = state == CharacterState.RUN ? _runAnimation
                : state == CharacterState.WALK ? _walkAnimation : _idleAnimation;
            _animator.Play(Animator.StringToHash(animation), 0, 0f);
        }
    }
}
