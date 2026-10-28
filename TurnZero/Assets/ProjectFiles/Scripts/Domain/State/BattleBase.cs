using System;
using System.Collections.Generic;
using System.Linq;

namespace TurnZero
{
    [Serializable]
    public sealed class BattleBase
    {
        public int Owner, Health;
        public bool Placed;
        public GridCell Cell;
        public BattleBase Copy() => (BattleBase)MemberwiseClone();
    }
}
