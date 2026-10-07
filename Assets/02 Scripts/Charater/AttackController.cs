using Project_Chronicles.Character.Combat;
using Project_Chronicles.Utils;
using UnityEngine;

namespace Project_Chronicles.Character
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInputListener), typeof(ComboController))]
    public sealed class AttackController : MonoBehaviour
    {
        [SerializeField] private AttackData _z1;
        //[SerializeField] private AttackData _x1;

        private readonly AttackTimeline _timeline = new AttackTimeline();
        private PlayerInputListener _input;
        private ComboController _combo;
        //private AttackInput _pendingHitStopInput;

        private void Awake()
        {
            _input = GetComponent<PlayerInputListener>();
            _combo = GetComponent<ComboController>();
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
        }

        private void Update()
        {
            if (!_timeline.IsRunning) return;
            _timeline.Tick(Time.deltaTime);

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
                }
            }
            
        }

        private void BeginAttack(AttackData attack)
        {
            _timeline.Begin(attack);
            Logging.Log($"[AttackController] : {attack.AttackId} 실행");
            //_pendingHitStopInput = AttackInput.NONE;
        }

        private void TryStartZ1()
        {
            if (_timeline.IsRunning)
            {
                _timeline.TryBufferInput(AttackInput.LIGHT);
                return;
            }
            AttackData openingAttack = _z1;
            if (openingAttack == null)
            {
                Logging.LogError("[AttackController] : Z1 is Null");
                return;
            }
            BeginAttack(openingAttack);
        }

        private void TryStartX1()
        {

        }
    }
}