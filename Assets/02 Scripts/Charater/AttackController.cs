using Project_Chronicles.Character.Combat;
using Project_Chronicles.Utils;
using UnityEngine;

namespace Project_Chronicles.Character
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInputListener), typeof(ComboController))]
    [RequireComponent(typeof(CharacterAnimator), typeof(CharacterStateMachine))]
    public sealed class AttackController : MonoBehaviour
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
        private PlayerInputListener _input;
        private ComboController _combo;
        private CharacterAnimator _animator;
        private CharacterStateMachine _stateMachine;

        private void Awake()
        {
            _input = GetComponent<PlayerInputListener>();
            _combo = GetComponent<ComboController>();
            _animator = GetComponent<CharacterAnimator>();
            _stateMachine = GetComponent<CharacterStateMachine>();
        }

        private void OnEnable()
        {
            _input.LightAttackRequested += TryStartZ1;
            _input.HeavyAttackRequested += TryStartX1;
        }

        private void OnDisable()
        {
            _input.LightAttackRequested -= TryStartZ1;
            _input.HeavyAttackRequested -= TryStartX1;
            _timeline.Reset();
            _animator.EndAttack();
        }

        private void Update()
        {
            TickAttack(Time.deltaTime);
        }

        private void TickAttack(float deltaTime)
        {
            if (!_timeline.IsRunning) return;
            bool hasLanded = false;
            if (_timeline.Attack.ActiveUntilLanding || _timeline.Attack.LockUntilLanding)
            {
                CharacterState state = _stateMachine.RefreshState();
                hasLanded = state == CharacterState.IDLE
                    || state == CharacterState.WALK || state == CharacterState.RUN;
            }
            _timeline.Tick(deltaTime, hasLanded);
            _animator.SetAttackProgress(_timeline.Attack.Duration > 0f
                ? _timeline.ElapsedTime / _timeline.Attack.Duration : 1f);

            if (!_timeline.IsRunning)
            {
                AttackData next = _combo.TryGetNextCombo(_timeline.Attack, _timeline.BufferInput);
                if (next != null)
                {
                    BeginAttack(next);
                }
                else
                {
                    Logging.Log($"[AttackController] : {_timeline.Attack.AttackId} 콤보 종료");
                    _timeline.Reset();
                    _animator.EndAttack();
                }
            }
            
        }

        private void BeginAttack(AttackData attack)
        {
            _timeline.Begin(attack);
            _animator.PlayAttack(attack);
            Logging.Log($"[AttackController] : {attack.AttackId} 실행");
        }

        private void TryStartZ1()
        {
            TryRequestAttack(AttackInput.LIGHT);
        }

        private void TryStartX1()
        {
            TryRequestAttack(AttackInput.HEAVY);
        }

        private void TryRequestAttack(AttackInput input)
        {
            if (_timeline.IsRunning)
            {
                if (_combo.TryGetNextCombo(_timeline.Attack, input) != null)
                {
                    _timeline.TryBufferInput(input);
                }
                return;
            }
            AttackData openingAttack = ResolveOpeningAttack(input, _stateMachine.RefreshState());
            if (openingAttack == null)
            {
                return;
            }
            BeginAttack(openingAttack);
        }

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
