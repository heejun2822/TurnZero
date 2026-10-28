using System;
using System.Collections.Generic;
using System.Linq;

namespace TurnZero
{
    [Serializable]
    public sealed class BattleBase
    {
        public int Owner;
        public int Health;
        public bool Placed;
        public GridCell Cell;

        public BattleBase Copy()
        {
            return (BattleBase)MemberwiseClone();
        }
    }
}
