using Project_Chronicles.Character.Combat;

namespace Project_Chronicles.Character
{
    public sealed class ComboController
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