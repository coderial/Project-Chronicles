using Project_Chronicles.Character.Combat;
using Project_Chronicles.Utils;
using UnityEngine;

namespace Project_Chronicles.Character
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInputListener), typeof(ComboController))]
    [RequireComponent(typeof(CharacterAnimator))]
    public sealed class AttackController : MonoBehaviour
    {
        [SerializeField] private AttackData _z1;
        [SerializeField] private AttackData _x1;

        private readonly AttackTimeline _timeline = new AttackTimeline();
        private PlayerInputListener _input;
        private ComboController _combo;
        private CharacterAnimator _characterAnimator;

        private void Awake()
        {
            _input = GetComponent<PlayerInputListener>();
            _combo = GetComponent<ComboController>();
            _characterAnimator = GetComponent<CharacterAnimator>();
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
            _characterAnimator.EndAttack();
        }

        private void Update()
        {
            TickAttack(Time.deltaTime);
        }

        private void TickAttack(float deltaTime)
        {
            if (!_timeline.IsRunning) return;
            _timeline.Tick(deltaTime);
            _characterAnimator.SetAttackProgress(_timeline.Attack.Duration > 0f
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
                    _characterAnimator.EndAttack();
                }
            }
            
        }

        private void BeginAttack(AttackData attack)
        {
            _timeline.Begin(attack);
            _characterAnimator.PlayAttack(attack);
            Logging.Log($"[AttackController] : {attack.AttackId} 실행");
        }

        private void TryStartZ1()
        {
            TryRequestAttack(AttackInput.LIGHT, _z1);
        }

        private void TryStartX1()
        {
            TryRequestAttack(AttackInput.HEAVY, _x1);
        }

        private void TryRequestAttack(AttackInput input, AttackData openingAttack)
        {
            if (_timeline.IsRunning)
            {
                if (_combo.TryGetNextCombo(_timeline.Attack, input) != null)
                {
                    _timeline.TryBufferInput(input);
                }
                return;
            }
            if (openingAttack == null)
            {
                return;
            }
            BeginAttack(openingAttack);
        }

    }
}
