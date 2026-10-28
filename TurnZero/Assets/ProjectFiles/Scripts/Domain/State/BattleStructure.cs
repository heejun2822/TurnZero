using System;
using System.Collections.Generic;
using System.Linq;

namespace TurnZero
{
    [Serializable]
    public sealed class BattleStructure
    {
        public int Id;
        public int Owner;
        public int Link;
        public int Health;
        public int ReadyTurn;
        public int Vision;
        public StructureKind Kind;
        public bool Active = true;
        public bool PendingDestroy;
        public bool Completed;

        public bool Operational => Active && (Health > 0 || PendingDestroy);

        public GridCell Cell;
        public GridCell Destination;

        public BattleStructure Copy()
        {
            return (BattleStructure)MemberwiseClone();
        }
    }
}
