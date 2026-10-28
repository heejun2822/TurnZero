using System.Collections.Generic;

namespace TurnZero
{
    public sealed class BattleView
    {
        public int Player;
        public int Turn;
        public int PlacementPlayer;
        public int PlacementSlot;
        public int AP;
        public int ReservedAP;
        public int Materials;
        public BattlePhase Phase;
        public BattleWinner Winner;
        public bool Ready;
        public double SecondsLeft;
        public HashSet<GridCell> Visible;
        public HashSet<GridCell> Explored;
        public List<BattleUnit> Units;
        public List<UnitObservation> LastSeen;
        public List<UnitObservation> KnownEnemies;
        public BattleBase OwnBase;
        public BattleBase EnemyBase;
        public bool EnemyBaseVisible;
        public List<BattleOrder> Orders;
        public List<BattleStructure> Structures;
        public List<FireZone> Fires;
        public List<GroundItem> Items;

        public BattleUnit FindUnit(int id)
        {
            foreach (BattleUnit unit in Units)
            {
                if (unit.Id == id)
                {
                    return unit;
                }
            }

            return null;
        }

        public UnitObservation FindKnownEnemy(int id)
        {
            foreach (UnitObservation observation in KnownEnemies)
            {
                if (observation.Unit.Id == id)
                {
                    return observation;
                }
            }

            return null;
        }

        public UnitObservation FindLastSeen(int id)
        {
            foreach (UnitObservation observation in LastSeen)
            {
                if (observation.Unit.Id == id)
                {
                    return observation;
                }
            }

            return null;
        }

        public BattleOrder FindOrder(int unitId)
        {
            foreach (BattleOrder order in Orders)
            {
                if (order.UnitId == unitId)
                {
                    return order;
                }
            }

            return null;
        }

        public BattleUnit FindNextUnitAt(GridCell cell, int selectedId)
        {
            BattleUnit selected = FindUnit(selectedId);
            bool hasSelection = selected != null && selected.Alive && selected.Cell == cell;
            BattleUnit first = null;
            BattleUnit next = null;

            foreach (BattleUnit unit in Units)
            {
                if (!unit.Alive || unit.Cell != cell)
                {
                    continue;
                }

                if (first == null || CompareSelectionOrder(unit, first) < 0)
                {
                    first = unit;
                }

                if (hasSelection && CompareSelectionOrder(unit, selected) > 0 &&
                    (next == null || CompareSelectionOrder(unit, next) < 0))
                {
                    next = unit;
                }
            }

            return next ?? first;
        }

        private int CompareSelectionOrder(BattleUnit left, BattleUnit right)
        {
            int leftTeam = left.Owner == Player ? 0 : 1;
            int rightTeam = right.Owner == Player ? 0 : 1;
            int team = leftTeam.CompareTo(rightTeam);

            return team != 0 ? team : left.Id.CompareTo(right.Id);
        }

        public Sight Sight(GridCell cell)
        {
            return Visible.Contains(cell)
                ? global::TurnZero.Sight.Visible
                : Explored.Contains(cell) ? global::TurnZero.Sight.Explored : global::TurnZero.Sight.Unknown;
        }
    }
}
