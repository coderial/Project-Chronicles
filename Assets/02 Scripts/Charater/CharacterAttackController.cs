using Project_Chronicles.Character.Combat;
using Project_Chronicles.Utils;
using Project_Chronicles.Character.StateMachine;
using UnityEngine;

namespace Project_Chronicles.Character
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInputListener))]
    [RequireComponent(typeof(CharacterAnimator), typeof(CharacterStateMachine))]
    public sealed class CharacterAttackController : MonoBehaviour
    {
        [SerializeField] private AttackData _groundZ1;
        [SerializeField] private AttackData _dashZ1;
        [SerializeField] private AttackData _jumpZ1;
        [SerializeField] private AttackData _dashJumpZ1;
        [SerializeField] private AttackData _groundX1;
        [SerializeField] private AttackData _dashX1;
        [SerializeField] private AttackData _jumpX1;
        [SerializeField] private AttackData _dashJumpX1;

        private readonly AttackTimeline _timeline = new AttackTimeline();
        private readonly ComboController _combo = new ComboController();
        private CharacterAnimator _animator;
        private CharacterStateMachine _stateMachine;
        private CharacterHorizontalMovement _movement;
        private AttackData _preparedAttack;
        private float _attackDirection;

        public bool IsAttacking => isActiveAndEnabled && _timeline.IsRunning;
        public AttackData CurrentAttack => _timeline.Attack;
        public bool HasPreparedAttack => isActiveAndEnabled && _preparedAttack != null;
        public bool CanCancel => IsAttacking && !_timeline.Attack.ActiveUntilLanding
            && !_timeline.Attack.LockUntilLanding && _timeline.ElapsedTime >= _timeline.Attack.CancelOpen;

        private void Awake()
        {
            _animator = GetComponent<CharacterAnimator>();
            _stateMachine = GetComponent<CharacterStateMachine>();
            _movement = GetComponent<CharacterHorizontalMovement>();
        }

        internal void PrepareOpeningAttack(AttackInput input, CharacterState movementState)
        {
            if (!isActiveAndEnabled) return;
            AttackData attack = ResolveOpeningAttack(input, movementState);
            if (attack == null) return;
            if (!_animator.CanPlayAttack(attack))
            {
                Logging.LogError($"[CharacterAttackController] : Missing animation for {attack.AttackId}");
                return;
            }
            _preparedAttack = attack;
        }

        internal void BeginPreparedAttack()
        {
            AttackData attack = _preparedAttack;
            _preparedAttack = null;
            if (isActiveAndEnabled && attack != null) BeginAttack(attack);
        }

        internal void ClearPreparedAttack() => _preparedAttack = null;

        internal bool TryBufferInput(AttackInput input)
        {
            return IsAttacking && _combo.TryGetNextCombo(_timeline.Attack, input) != null
                && _timeline.TryBufferInput(input);
        }

        // Called only by AttackState while the state machine grants attack ownership.
        internal void TickAttack(float deltaTime)
        {
            if (!IsAttacking) return;
            bool hasLanded = (_timeline.Attack.ActiveUntilLanding || _timeline.Attack.LockUntilLanding)
                && _stateMachine.IsGrounded;
            float previousTime = _timeline.ElapsedTime;
            _timeline.Tick(deltaTime, hasLanded);
            float duration = _timeline.Attack.Duration;
            if (duration > 0f)
            {
                float progressDelta = Mathf.Clamp01(_timeline.ElapsedTime / duration)
                    - Mathf.Clamp01(previousTime / duration);
                _movement.QueueAttackDisplacement(_timeline.Attack.MoveDistance * _attackDirection * progressDelta);
            }
            _animator.SetAttackProgress(_timeline.Attack.Duration > 0f
                ? _timeline.ElapsedTime / _timeline.Attack.Duration : 1f);

            if (!_timeline.IsRunning)
            {
                AttackData next = _combo.TryGetNextCombo(_timeline.Attack, _timeline.BufferInput);
                if (next != null && _animator.CanPlayAttack(next))
                    BeginAttack(next);
                else
                    Logging.Log($"[CharacterAttackController] : {_timeline.Attack.AttackId} 콤보 종료");
            }
        }

        private void BeginAttack(AttackData attack)
        {
            _timeline.Begin(attack);
            _attackDirection = _movement.FacingDirection;
            _animator.PlayAttack(attack);
            Logging.Log($"[CharacterAttackController] : {attack.AttackId} 실행");
        }

        internal void CancelAttack(bool preserveCompletedMovement = false)
        {
            // Natural completion keeps the final frame's displacement for the next physics step.
            // Cancels, interrupts and disables discard movement that has not been applied yet.
            if (!preserveCompletedMovement || _timeline.Phase != AttackPhase.COMPLETE)
                _movement?.ClearAttackDisplacement();
            _preparedAttack = null;
            _timeline.Reset();
            if (_animator != null) _animator.EndAttack();
        }

        private void OnDisable() => CancelAttack();

        private AttackData ResolveOpeningAttack(AttackInput input, CharacterState state)
        {
            bool isLightAttack = input == AttackInput.LIGHT;
            switch (state)
            {
                case CharacterState.IDLE:
                case CharacterState.WALK:
                    return isLightAttack ? _groundZ1 : _groundX1;
                case CharacterState.RUN:
                    return isLightAttack ? _dashZ1 : _dashX1;
                case CharacterState.DASH_JUMP:
                    return isLightAttack ? _dashJumpZ1 : _dashJumpX1;
                case CharacterState.JUMP:
                case CharacterState.FALL:
                    return isLightAttack ? _jumpZ1 : _jumpX1;
                default:
                    return null;
            }
        }
    }
}
