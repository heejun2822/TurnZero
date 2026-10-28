using System;
using System.Collections.Generic;
using System.Linq;

namespace TurnZero.Game
{
    [Serializable]
    public sealed class BattleUnit
    {
        public int Id, Owner, Slot, Health, DiedTurn;
        public bool Placed;
        public GridCell Cell;
        public int Equipment = -1;
        public int[] ReadyOn = new int[4];
        public bool DefeatPending;
        public List<StatusEffect> Effects = new List<StatusEffect>();
        public bool Alive => Placed && (Health > 0 || DefeatPending);
        public bool Rooted(int turn) => BattleStatusEffects.Has(this, StatusEffectKind.Root, turn);
        public BattleUnit Copy()
        {
            var copy = (BattleUnit)MemberwiseClone();
            copy.ReadyOn = (int[])ReadyOn.Clone();
            copy.Effects = Effects.Select(e => e.Copy()).ToList();
            return copy;
        }
    }
}
