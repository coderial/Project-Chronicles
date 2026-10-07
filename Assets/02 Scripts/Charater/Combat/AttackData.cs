using UnityEngine;

namespace Project_Chronicles.Character.Combat
{
    [CreateAssetMenu(fileName = "CharacterAttackData", menuName = "Character/Combat/AttackData")]
    public class AttackData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _attackId = string.Empty;
        [SerializeField] private string _animation = string.Empty;
        [SerializeField] private AttackInput _input;

        [Header("Timing (Seconds)")]
        [SerializeField, Min(0f)] private float _startup;
        [SerializeField, Min(0f)] private float _active;
        [SerializeField, Min(0f)] private float _recovery;
        [SerializeField] private bool _activeUntilLanding;
        [SerializeField] private bool _lockUntilLanding;
        [SerializeField, Min(0f)] private float _plungeSpeed;

        [Header("Hits")]
        [SerializeField] private float _hitBoxOffsetY;
        [SerializeField, Min(0f)] private float _damage;
        [SerializeField, Min(0f)] private float _hitStun;
        [SerializeField] private float _knockbackX;
        [SerializeField] private float _knockbackY;
        [SerializeField, Min(0f)] private float _downValue;
        [SerializeField] private bool _forceKnockDown;
        [SerializeField, Min(0f)] private float _hitStop;

        [Header("Combo Timing(Seconds from Attack Start")]
        [SerializeField, Min(0f)] private float _comboOpen;
        [SerializeField, Min(0f)] private float _comboClose;
        [SerializeField, Min(0f)] private float _cancelOpen;

        [Header("Branches")]
        [SerializeField] private AttackData _nextZ;
        [SerializeField] private AttackData _nextX;

        public string AttackId => _attackId;
        public string Animation => _animation;
        public AttackInput Input => _input;
        public float Startup => _startup;

        public float Active => _active;
        public float Recovery => _recovery;
        public bool ActiveUntilLanding => _activeUntilLanding;
        public bool LockUntilLanding => _lockUntilLanding;
        public float PlungeSpeed => _plungeSpeed;
        public float HitBoxOffsetY => _hitBoxOffsetY;
        public float Damage => _damage;
        public float HitStun => _hitStun;
        public float KnockbackX => _knockbackX;
        public float KnockbackY => _knockbackY;
        public float DownValue => _downValue;
        public bool ForceKnockDown => _forceKnockDown;
        public float HitStop => _hitStop;
        public float ComboOpen => _comboOpen;
        public float ComboClose => _comboClose;
        public float CancelOpen => _cancelOpen;
        public AttackData NextZ => _nextZ;
        public AttackData NextX => _nextX;

    }
}