using Project_Chronicles.Character.Combat;
using UnityEngine;

namespace Project_Chronicles.Character
{
    [DisallowMultipleComponent]
    public sealed class ComboController : MonoBehaviour
    {
        public AttackData TryGetNextCombo(AttackData current, AttackInput input)
        {
            if (input == AttackInput.NONE)
            {
                return null;
            }

            if (current == null)
            { 
                return null;
            }
            
            return input == AttackInput.LIGHT
                ? current.NextZ
                : input == AttackInput.HEAVY
                ? current.NextX
                : null;
        }
    }
}