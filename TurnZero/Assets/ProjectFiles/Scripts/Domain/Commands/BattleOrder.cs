using System;
using System.Collections.Generic;
using System.Linq;

namespace TurnZero
{
    [Serializable]
    public sealed class BattleOrder
    {
        public int UnitId;
        public BattleAction Action;
        public GridCell Target;
        public GridCell Exit;
        public int TargetUnit;
        public List<GridCell> Path = new List<GridCell>();

        public BattleOrder Copy()
        {
            return new BattleOrder
            {
                UnitId = UnitId,
                Action = Action,
                Target = Target,
                Exit = Exit,
                TargetUnit = TargetUnit,
                Path = new List<GridCell>(Path)
            };
        }
    }
}
