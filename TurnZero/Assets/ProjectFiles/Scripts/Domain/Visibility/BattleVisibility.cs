using System.Collections.Generic;
using System.Linq;

namespace TurnZero
{
    public static class BattleVisibility
    {
        public static IEnumerable<GridCell> Area(GameRules rules, GridCell center, int radius)
        {
            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    GridCell cell = new GridCell(center.X + x, center.Y + y);

                    if (rules.Contains(cell) && center.Distance(cell) <= radius)
                    {
                        yield return cell;
                    }
                }
            }
        }

        public static HashSet<GridCell> Vision(MatchState state, GameRules rules, int player)
        {
            HashSet<GridCell> cells = new HashSet<GridCell>(state.Revealed[player]);
            BattleBase home = state.Bases[player];

            if (home.Placed && home.Health > 0)
            {
                cells.UnionWith(Area(rules, home.Cell, rules.BaseVision));
            }

            foreach (BattleUnit unit in state.Units)
            {
                if (unit.Owner == player && unit.Alive)
                {
                    cells.UnionWith(Area(rules, unit.Cell, rules.UnitVision));
                }
            }

            foreach (BattleStructure beacon in state.Structures)
            {
                if (beacon.Owner == player && beacon.Operational && beacon.Kind == StructureKind.Beacon)
                {
                    cells.UnionWith(Area(rules, beacon.Cell, beacon.Vision));
                }
            }

            return cells;
        }

        public static bool Detected(BattleUnit unit, MatchState state, int player)
        {
            foreach (BattleStructure beacon in state.Structures)
            {
                if (beacon.Owner == player && beacon.Kind == StructureKind.Beacon &&
                    beacon.Operational && beacon.Cell.Distance(unit.Cell) <= beacon.Vision)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool CanSee(BattleUnit unit, MatchState state, int player, HashSet<GridCell> vision)
        {
            return unit.Owner == player ||
                unit.Alive &&
                vision.Contains(unit.Cell) &&
                (!BattleStatusEffects.Has(unit, StatusEffectKind.Stealth, state.Turn) || Detected(unit, state, player));
        }

        public static void Observe(MatchState state, GameRules rules)
        {
            for (int player = 0; player < GameDefinitions.PlayerCount; player++)
            {
                HashSet<GridCell> visible = Vision(state, rules, player);

                state.Explored[player].UnionWith(visible);

                foreach (BattleUnit unit in state.Units.Where(u => u.Owner != player))
                {
                    bool observed = unit.Placed &&
                        visible.Contains(unit.Cell) &&
                        (!BattleStatusEffects.Has(unit, StatusEffectKind.Stealth, state.Turn) || Detected(unit, state, player));

                    if (observed)
                    {
                        state.Memory[player][unit.Id] = new UnitObservation
                        {
                            Unit = unit.Copy(),
                            Turn = state.Turn
                        };
                    }
                    else if (unit.Placed && unit.Health <= 0 && !unit.DefeatPending)
                    {
                        bool known = state.Memory[player].TryGetValue(unit.Id, out UnitObservation last);
                        BattleUnit dead = known
                            ? last.Unit.Copy()
                            : new BattleUnit
                            {
                                Id = unit.Id,
                                Owner = unit.Owner,
                                Slot = unit.Slot,
                                Placed = true
                            };

                        dead.Health = 0;
                        dead.DefeatPending = false;
                        dead.DiedTurn = unit.DiedTurn;

                        state.Memory[player][unit.Id] = new UnitObservation
                        {
                            Unit = dead,
                            Turn = known ? last.Turn : state.Turn,
                            HasPosition = known && last.HasPosition
                        };
                    }
                }

                BattleBase enemyBase = state.Bases[1 - player];

                if (enemyBase.Placed && visible.Contains(enemyBase.Cell))
                {
                    state.KnownBases[player] = enemyBase.Copy();
                }

                foreach (GroundItem item in state.Items.Where(i => visible.Contains(i.Cell)))
                {
                    state.ItemMemory[player][item.Id] = item.Copy();
                }

                foreach (GroundItem item in state.ItemMemory[player].Values.Where(i => visible.Contains(i.Cell) && !state.Items.Any(a => a.Id == i.Id))
                    .ToArray())
                {
                    state.ItemMemory[player].Remove(item.Id);
                }

                foreach (BattleStructure structure in state.Structures.Where(s => s.Owner != player && s.Operational && visible.Contains(s.Cell)))
                {
                    state.StructureMemory[player][structure.Id] = structure.Copy();
                }

                foreach (BattleStructure structure in state.StructureMemory[player].Values.Where(s => visible.Contains(s.Cell) && !state.Structures.Any(a => a.Id == s.Id && a.Operational))
                    .ToArray())
                {
                    state.StructureMemory[player].Remove(structure.Id);
                }
            }
        }

        public static BattleView View(MatchState state, GameRules rules, int player, double now, bool revealAll = false)
        {
            HashSet<GridCell> vision = revealAll
                ? new HashSet<GridCell>(Area(rules, new GridCell(rules.Width / 2, rules.Height / 2), rules.Width + rules.Height))
                : Vision(state, rules, player);
            List<BattleOrder> orders = state.Phase == BattlePhase.Planning
                ? state.Orders.Where(o => state.Units.Find(u => u.Id == o.UnitId).Owner == player)
                .Select(o => o.Copy())
                .ToList()
                : new List<BattleOrder>();
            List<BattleUnit> units = state.Units.Where(u => u.Owner == player || (revealAll ? u.Alive : CanSee(u, state, player, vision)))
                .Select(u => u.Copy())
                .ToList();

            return new BattleView
            {
                Player = player,
                Turn = state.Turn,
                Phase = state.Phase,
                Winner = state.Winner,
                PlacementPlayer = state.PlacementPlayer,
                PlacementSlot = state.PlacementSlot,
                AP = state.AP[player],
                Materials = state.Materials[player],
                Ready = state.Ready[player],
                ReservedAP = orders.Sum(o => BattleCommands.Cost(state, rules, o)),
                SecondsLeft = System.Math.Max(0, state.Deadline - now),
                Visible = vision,
                Explored = new HashSet<GridCell>(state.Explored[player]),
                Units = units,
                LastSeen = state.Memory[player].Values.Where(o => o.HasPosition && o.Unit.Health > 0 && !units.Any(u => u.Id == o.Unit.Id))
                    .Select(o => o.Copy())
                    .ToList(),
                KnownEnemies = state.Memory[player].Values.Select(o => o.Copy()).ToList(),
                OwnBase = state.Bases[player].Copy(),
                EnemyBase = revealAll ? state.Bases[1 - player].Copy() : state.KnownBases[player]?.Copy(),
                EnemyBaseVisible = state.Bases[1 - player].Placed && vision.Contains(state.Bases[1 - player].Cell),
                Orders = orders,
                Structures = revealAll
                    ? state.Structures.Where(s => s.Operational).Select(s => s.Copy()).ToList()
                    : state.Structures.Where(s => s.Owner == player && s.Operational)
                    .Select(s => s.Copy())
                    .Concat(state.StructureMemory[player].Values.Select(s => s.Copy()))
                    .ToList(),
                Fires = state.Fires.Where(f => f.Owner == player || Area(rules, f.Cell, f.Area).Any(vision.Contains))
                    .Select(f => f.Copy())
                    .ToList(),
                Items = revealAll
                    ? state.Items.Select(i => i.Copy()).ToList()
                    : state.ItemMemory[player].Values.Select(i => i.Copy()).ToList()
            };
        }
    }
}
