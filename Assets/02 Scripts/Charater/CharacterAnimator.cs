using UnityEngine;
using Project_Chronicles.Common;
using Project_Chronicles.Character.Combat;
using Project_Chronicles.Utils;
using Project_Chronicles.Character.StateMachine;

namespace Project_Chronicles.Character
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterStateMachine))]
    [RequireComponent(typeof(CharacterHorizontalMovement))]
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
        [SerializeField] private string _jumpAnimation = "Base Layer.Theodore_Jump";
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
            PlayLocomotion(CharacterState.JUMP);
        }

        public void PlayLocomotion(CharacterState state)
        {
            if (_animator == null) return;
            SetMovement(state == CharacterState.WALK, state == CharacterState.RUN);
            _animator.ResetTrigger(_jumpHash);
            bool airborne = state == CharacterState.JUMP || state == CharacterState.DASH_JUMP
                || state == CharacterState.FALL;
            string animation = airborne ? _jumpAnimation
                : state == CharacterState.RUN ? _runAnimation
                : state == CharacterState.WALK ? _walkAnimation : _idleAnimation;
            int hash = Animator.StringToHash(animation);
            if (_animator.HasState(0, hash))
                _animator.Play(hash, 0, state == CharacterState.FALL ? 1f : 0f);
        }

        internal bool CanPlayAttack(AttackData attack)
        {
            return attack != null && !string.IsNullOrWhiteSpace(attack.Animation)
                && _animator.HasState(0, Animator.StringToHash(attack.Animation));
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

            PlayLocomotion(_stateMachine.GetLocomotionState());
        }
    }
}
