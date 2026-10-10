using Project_Chronicles.Utils;
using UnityEngine;

namespace Project_Chronicles.Character.Combat
{
    public sealed class AttackTimeline
    {
        private AttackData _attack;
        private float _elapsedTime;
        private float TotalDuration => _attack.Duration;
        public AttackData Attack => _attack;
        public float ElapsedTime => _elapsedTime;
        public AttackPhase Phase { get; private set; } = AttackPhase.INACTIVE;
        public AttackInput BufferInput { get; private set; } = AttackInput.NONE;
        public bool IsRunning => Phase != AttackPhase.INACTIVE && Phase != AttackPhase.COMPLETE;
        public bool IsComboWindowOpen => IsRunning
            && _attack.ComboClose > _attack.ComboOpen
            && _elapsedTime >= _attack.ComboOpen
            && _elapsedTime < _attack.ComboClose;
        public bool IsAcive => Phase == AttackPhase.ACTIVE;
        
        public void Begin(AttackData attackData)
        {
            if (attackData == null)
            {
                Logging.LogError("[AttackTimeline] : AttackData is Null");
                return;
            }
            _attack = attackData;
            _elapsedTime = 0f;
            BufferInput = AttackInput.NONE;
            RefreshPhase();
        }

        public bool TryBufferInput(AttackInput input)
        {
            if ((input != AttackInput.LIGHT && input != AttackInput.HEAVY) || !IsComboWindowOpen || BufferInput != AttackInput.NONE)
            {
                return false;
            }
            BufferInput = input;
            return true;
        }

        public void Tick(float dt, bool hasLanded = false)
        {
            if (dt < 0f)
            {
                Logging.LogError("[AttackTimeline] : Attack Time Cannot Move Backwards");
                return;
            }
            if (_attack == null || Phase == AttackPhase.COMPLETE)
            {
                return;
            }

            if (hasLanded && (_attack.ActiveUntilLanding || _attack.LockUntilLanding))
            {
                Phase = AttackPhase.COMPLETE;
                return;
            }

            _elapsedTime = _attack.ActiveUntilLanding || _attack.LockUntilLanding
                ? _elapsedTime + dt
                : Mathf.Min(_elapsedTime + dt, TotalDuration);
            RefreshPhase();
            if (!_attack.ActiveUntilLanding && !_attack.LockUntilLanding &&
                _elapsedTime >= TotalDuration)
            {
                Phase = AttackPhase.COMPLETE;
            }
        }

        public void Reset()
        {
            _attack = null;
            _elapsedTime = 0f;
            BufferInput = AttackInput.NONE;
            Phase = AttackPhase.INACTIVE;
        }

        private void RefreshPhase()
        {
            float activeStart = _attack.Startup;
            float recoveryStart = activeStart + _attack.Active;
            float completeTime = recoveryStart + _attack.Recovery;


            if (_elapsedTime < activeStart && !(activeStart - _elapsedTime <= Mathf.Epsilon))
            {
                Phase = AttackPhase.STARTUP;
            }
            else if (_attack.ActiveUntilLanding)
            {
                Phase = AttackPhase.ACTIVE;
            }
            else if (_attack.LockUntilLanding && _elapsedTime >= recoveryStart)
            {
                Phase = AttackPhase.RECOVERY;
            }
            else if (_elapsedTime < recoveryStart)
            {
                Phase = AttackPhase.ACTIVE;
            }
            else if (_elapsedTime < completeTime)
            {
                Phase = AttackPhase.RECOVERY;
            }
            else
            {
                Phase = AttackPhase.COMPLETE;   
            }
        }
    }
}
