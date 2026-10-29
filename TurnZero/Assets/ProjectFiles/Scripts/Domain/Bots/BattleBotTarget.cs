using System;

namespace TurnZero
{
    [Serializable]
    public sealed class BattleBotTarget
    {
        public int Id;
        public GridCell Cell;
        public int Health;
        public bool Character;
        public bool Base;
        public bool RootNextTurn;
    }
}
