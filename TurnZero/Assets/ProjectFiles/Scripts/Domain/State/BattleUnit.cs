using System;
using System.Collections.Generic;
using System.Linq;

namespace TurnZero
{
    [Serializable]
    public sealed class BattleUnit
    {
        public int Id;
        public int Owner;
        public int Slot;
        public int Health;
        public int DiedTurn;
        public bool Placed;
        public GridCell Cell;
        public int Equipment = -1;
        public int[] ReadyOn = new int[GameDefinitions.CooldownActionCount];
        public bool DefeatPending;
        public List<StatusEffect> Effects = new List<StatusEffect>();

        public bool Alive => Placed && (Health > 0 || DefeatPending);

        public bool Rooted(int turn)
        {
            return BattleStatusEffects.Has(this, StatusEffectKind.Root, turn);
        }

        public BattleUnit Copy()
        {
            BattleUnit copy = (BattleUnit)MemberwiseClone();

            copy.ReadyOn = (int[])ReadyOn.Clone();
            copy.Effects = Effects.Select(e => e.Copy()).ToList();

            return copy;
        }
    }
}
