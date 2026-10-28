using System;
using System.Collections.Generic;
using System.Linq;

namespace TurnZero.Game
{
    [Serializable]
    public sealed class FireZone
    {
        public int Id, Owner, Damage, Area, ThroughTurn, CreatedTurn, CreatorId;
        public int Phase = 3;
        public GridCell Cell;
        public FireZone Copy() => (FireZone)MemberwiseClone();
    }
}
