using System;
using System.Collections.Generic;

namespace TurnZero
{
    [Serializable]
    public sealed class BattleBotObservation
    {
        public GameRules Rules;
        public int Player;
        public int Turn;
        public int AP;
        public BattlePhase Phase;
        public bool Ready;
        public BattleBase OwnBase;
        public BattleBase EnemyBase;
        public bool EnemyBaseVisible;
        public List<BattleUnit> OwnUnits = new List<BattleUnit>();
        public List<BattleOrder> Orders = new List<BattleOrder>();
        public List<BattleBotTarget> Targets = new List<BattleBotTarget>();
        public List<UnitObservation> Memory = new List<UnitObservation>();
        public List<GridCell> Visible = new List<GridCell>();
        public List<GridCell> Explored = new List<GridCell>();

        public static BattleBotObservation Capture(LocalBattleMatch match, int player, double now)
        {
            return FromView(match.View(player, now, false), match.Rules);
        }

        public static BattleBotObservation FromView(BattleView view, GameRules rules)
        {
            BattleBotObservation result = new BattleBotObservation
            {
                Rules = rules.Copy(),
                Player = view.Player,
                Turn = view.Turn,
                AP = view.AP - view.ReservedAP,
                Phase = view.Phase,
                Ready = view.Ready,
                OwnBase = view.OwnBase.Copy(),
                EnemyBase = view.EnemyBase?.Copy(),
                EnemyBaseVisible = view.EnemyBaseVisible,
                Visible = new List<GridCell>(view.Visible),
                Explored = new List<GridCell>(view.Explored)
            };

            foreach (BattleUnit unit in view.Units)
            {
                if (unit.Owner == view.Player)
                {
                    result.OwnUnits.Add(unit.Copy());
                }
                else if (unit.Alive)
                {
                    result.Targets.Add(new BattleBotTarget
                    {
                        Id = unit.Id,
                        Cell = unit.Cell,
                        Health = unit.Health,
                        Character = true,
                        RootNextTurn = unit.Rooted(view.Turn + 1)
                    });
                }
            }

            foreach (UnitObservation memory in view.KnownEnemies)
            {
                result.Memory.Add(new UnitObservation
                {
                    Turn = memory.Turn,
                    HasPosition = memory.HasPosition,
                    Unit = new BattleUnit
                    {
                        Id = memory.Unit.Id,
                        Owner = memory.Unit.Owner,
                        Slot = memory.Unit.Slot,
                        Health = memory.Unit.Health,
                        Placed = memory.HasPosition,
                        Cell = memory.HasPosition ? memory.Unit.Cell : default
                    }
                });
            }

            foreach (BattleOrder order in view.Orders)
            {
                result.Orders.Add(order.Copy());
            }

            if (view.EnemyBaseVisible && view.EnemyBase != null && view.EnemyBase.Health > 0)
            {
                result.Targets.Add(new BattleBotTarget
                {
                    Id = rules.BaseId(1 - view.Player),
                    Cell = view.EnemyBase.Cell,
                    Health = view.EnemyBase.Health,
                    Base = true
                });
            }

            foreach (BattleStructure structure in view.Structures)
            {
                if (structure.Owner != view.Player && structure.Health > 0 && view.Visible.Contains(structure.Cell))
                {
                    result.Targets.Add(new BattleBotTarget
                    {
                        Id = structure.Id,
                        Cell = structure.Cell,
                        Health = structure.Health
                    });
                }
            }

            result.OwnUnits.Sort((left, right) => left.Slot.CompareTo(right.Slot));
            result.Targets.Sort((left, right) => left.Id.CompareTo(right.Id));
            result.Memory.Sort((left, right) => left.Unit.Id.CompareTo(right.Unit.Id));
            result.Orders.Sort((left, right) => left.UnitId.CompareTo(right.UnitId));
            result.Visible.Sort(CompareCells);
            result.Explored.Sort(CompareCells);
            return result;
        }

        public static int CompareCells(GridCell left, GridCell right)
        {
            int row = left.Y.CompareTo(right.Y);
            return row != 0 ? row : left.X.CompareTo(right.X);
        }
    }
}
