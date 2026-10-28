using System;
using System.Collections.Generic;
using System.Linq;

namespace TurnZero
{
    [Serializable]
    public sealed class GroundItem
    {
        public int Id;
        public GridCell Cell;

        public GroundItem Copy()
        {
            return new GroundItem
            {
                Id = Id,
                Cell = Cell
            };
        }
    }
}
