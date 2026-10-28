using System;
using System.Collections.Generic;
using System.Linq;

namespace TurnZero.Game
{
    [Serializable]
    public sealed class BattleStructure
    {
        public int Id, Owner, Link, Health, ReadyTurn, Vision;
        public StructureKind Kind;
        public bool Active = true, PendingDestroy, Completed;
        public bool Operational => Active && (Health > 0 || PendingDestroy);
        public GridCell Cell, Destination;
        public BattleStructure Copy() => (BattleStructure)MemberwiseClone();
    }
}
