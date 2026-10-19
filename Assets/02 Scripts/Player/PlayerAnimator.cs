using UnityEngine;
using Project_Chronicles.Common;
using Project_Chronicles.Player.Combat;
using Project_Chronicles.Utils;
using Project_Chronicles.Player.StateMachine;

namespace Project_Chronicles.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerStateMachine))]
    [RequireComponent(typeof(PlayerHorizontalMovement))]
    public class PlayerAnimator : BaseAnimator
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

        private PlayerStateMachine _stateMachine;
        private PlayerHorizontalMovement _movement;

        protected override void Awake()
        {
            base.Awake();
            _stateMachine = GetComponent<PlayerStateMachine>();
            _movement = GetComponent<PlayerHorizontalMovement>();
        }

        private void LateUpdate()
        {
            PlayerState state = _stateMachine.CurrentState;
            SetMovement(state == PlayerState.WALK, state == PlayerState.RUN);
            _spriteRenderer.flipX = _movement.FacingDirection < 0f;
        }

        public void SetMovement(bool isWalking, bool isRunning)
        {
            _animator.SetBool(_walkHash, isWalking);
            _animator.SetBool(_runHash, isRunning);
        }

        public void Jump()
        {
            PlayLocomotion(PlayerState.JUMP);
        }

        public void PlayLocomotion(PlayerState state)
        {
            if (_animator == null) return;
            SetMovement(state == PlayerState.WALK, state == PlayerState.RUN);
            _animator.ResetTrigger(_jumpHash);
            bool airborne = state == PlayerState.JUMP || state == PlayerState.DASH_JUMP
                || state == PlayerState.FALL;
            string animation = airborne ? _jumpAnimation
                : state == PlayerState.RUN ? _runAnimation
                : state == PlayerState.WALK ? _walkAnimation : _idleAnimation;
            int hash = Animator.StringToHash(animation);
            if (_animator.HasState(0, hash))
                _animator.Play(hash, 0, state == PlayerState.FALL ? 1f : 0f);
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
