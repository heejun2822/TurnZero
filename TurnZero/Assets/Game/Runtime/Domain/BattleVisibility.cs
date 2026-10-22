using System.Collections.Generic;
using System.Linq;

namespace TurnZero.Game
{
    public static class BattleVisibility
    {
        public static IEnumerable<GridCell> Area(GameRules rules, GridCell center, int radius)
        {
            for (int y = -radius; y <= radius; y++)
                for (int x = -radius; x <= radius; x++)
                {
                    var cell = new GridCell(center.X + x, center.Y + y);
                    if (rules.Contains(cell) && center.Distance(cell) <= radius) yield return cell;
                }
        }

        public static HashSet<GridCell> Vision(MatchState state, GameRules rules, int player)
        {
            var cells = new HashSet<GridCell>(state.Revealed[player]);
            var home = state.Bases[player];
            if (home.Placed && home.Health > 0) cells.UnionWith(Area(rules, home.Cell, rules.BaseVision));
            foreach (var unit in state.Units.Where(u => u.Owner == player && u.Alive)) cells.UnionWith(Area(rules, unit.Cell, rules.UnitVision));
            foreach (var beacon in state.Structures.Where(s => s.Owner == player && s.Health > 0 && s.Kind == StructureKind.Beacon))
                cells.UnionWith(Area(rules, beacon.Cell, beacon.Vision));
            return cells;
        }

        public static bool CanSee(BattleUnit unit, MatchState state, int player, HashSet<GridCell> vision) =>
            unit.Owner == player || unit.Alive && vision.Contains(unit.Cell) && (unit.HiddenThrough == 0 || unit.HiddenThrough < state.Turn);

        public static void Observe(MatchState state, GameRules rules)
        {
            for (int player = 0; player < 2; player++)
            {
                var visible = Vision(state, rules, player);
                state.Explored[player].UnionWith(visible);
                foreach (var unit in state.Units.Where(u => u.Owner != player))
                {
                    if (CanSee(unit, state, player, visible))
                        state.Memory[player][unit.Id] = new UnitObservation { Unit = unit.Copy(), Turn = state.Turn };
                    else if (unit.Placed && unit.Health <= 0 && visible.Contains(unit.Cell))
                        state.Memory[player][unit.Id] = new UnitObservation { Unit = unit.Copy(), Turn = state.Turn };
                    else if (state.Memory[player].TryGetValue(unit.Id, out var memory) && memory.Unit.Health > 0 && visible.Contains(memory.Unit.Cell))
                        state.Memory[player].Remove(unit.Id);
                }
                var enemyBase = state.Bases[1 - player];
                if (enemyBase.Placed && visible.Contains(enemyBase.Cell)) state.KnownBases[player] = enemyBase.Copy();
            }
        }

        public static BattleView View(MatchState state, GameRules rules, int player, double now, bool revealAll = false)
        {
            var vision = revealAll ? new HashSet<GridCell>(Area(rules, new GridCell(rules.Width / 2, rules.Height / 2), rules.Width + rules.Height)) : Vision(state, rules, player);
            var orders = state.Phase == BattlePhase.Planning ? state.Orders.Where(o => state.Units.Find(u => u.Id == o.UnitId).Owner == player).Select(o => o.Copy()).ToList() : new List<BattleOrder>();
            return new BattleView { Player = player, Turn = state.Turn, Phase = state.Phase, Winner = state.Winner,
                PlacementPlayer = state.PlacementPlayer, PlacementSlot = state.PlacementSlot,
                AP = state.AP[player], Materials = state.Materials[player], Ready = state.Ready[player],
                ReservedAP = orders.Sum(o => BattleCommands.Cost(state, rules, o)), SecondsLeft = System.Math.Max(0, state.Deadline - now),
                Visible = vision, Explored = new HashSet<GridCell>(state.Explored[player]),
                Units = state.Units.Where(u => u.Owner == player || (revealAll ? u.Alive : CanSee(u, state, player, vision))).Select(u => u.Copy()).ToList(),
                LastSeen = state.Memory[player].Values.Where(o => o.Unit.Health > 0 && !vision.Contains(o.Unit.Cell)).Select(o => o.Copy()).ToList(),
                KnownEnemies = state.Memory[player].Values.Select(o => o.Copy()).ToList(),
                OwnBase = state.Bases[player].Copy(), EnemyBase = revealAll ? state.Bases[1 - player].Copy() : state.KnownBases[player]?.Copy(),
                EnemyBaseVisible = state.Bases[1 - player].Placed && vision.Contains(state.Bases[1 - player].Cell), Orders = orders,
                Structures = state.Structures.Where(s => s.Health > 0 && (s.Owner == player || vision.Contains(s.Cell))).Select(s => s.Copy()).ToList(),
                Fires = state.Fires.Where(f => f.Owner == player || Area(rules, f.Cell, System.Math.Max(0, f.Area - 1)).Any(vision.Contains)).Select(f => f.Copy()).ToList(),
                Items = state.Items.Where(i => state.Explored[player].Contains(i.Cell) || vision.Contains(i.Cell)).Select(i => i.Copy()).ToList() };
        }
    }
}
