using System;
using System.Collections.Generic;
using System.Linq;

namespace TurnZero.Game
{
    [Serializable]
    public sealed class GroundItem
    {
        public int Id;
        public GridCell Cell;
        public GroundItem Copy() => new GroundItem { Id = Id, Cell = Cell };
    }
}
