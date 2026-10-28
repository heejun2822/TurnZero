using System;
using System.Collections.Generic;
using System.Linq;

namespace TurnZero
{
    [Serializable]
    public sealed class FireZone
    {
        public int Id;
        public int Owner;
        public int Damage;
        public int Area;
        public int ThroughTurn;
        public int CreatedTurn;
        public int CreatorId;
        public int Phase = 3;
        public GridCell Cell;

        public FireZone Copy()
        {
            return (FireZone)MemberwiseClone();
        }
    }
}
