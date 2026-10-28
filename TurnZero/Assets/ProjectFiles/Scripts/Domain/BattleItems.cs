using System;
using System.Collections.Generic;
using System.Linq;
namespace TurnZero.Game
{
    public static class BattleItems
    {
        public static void Collect(MatchState state, IEnumerable<BattleUnit> arrivals)
        {
            var contacts = arrivals.Where(u => u.Alive && u.Health > 0).Select(u => (u.Owner,u.Cell)).ToArray();
            foreach (var item in state.Items.ToArray())
            {
                var teams = contacts.Where(c => c.Cell == item.Cell).Select(c => c.Owner).Distinct().ToArray();
                if (teams.Length == 0) continue;
                foreach (int team in teams) { state.Materials[team]++; state.ItemMemory[team].Remove(item.Id); }
                state.Items.Remove(item);
            }
        }
        public static void Spawn(MatchState state, GameRules rules)
        {
            // Q-12 has no defined upgrade/consumable contents; do not spawn placeholder rewards.
            if (state.Turn <= 0 || state.Turn % 5 != 0) return;
            var accessible = new HashSet<GridCell>();
            foreach (var unit in state.Units.Where(u => u.Alive))
                accessible.UnionWith(BattlePathfinding.Reachable(state,rules,unit.Cell,rules.Width*rules.Height,-1));
            var random = new Random(unchecked(rules.Seed + state.Turn * 7919));
            for (int region = 0; region < 3; region++)
            {
                int start = region == 0 ? 0 : region == 1 ? rules.HomeHeight : rules.HomeHeight + rules.NeutralHeight;
                int height = region == 1 ? rules.NeutralHeight : rules.HomeHeight;
                var candidates = new List<GridCell>();
                for (int y = start; y < start+height; y++) for (int x = 0; x < rules.Width; x++)
                {
                    var cell = new GridCell(x,y);
                    if (accessible.Contains(cell) && BattleCommands.Movable(state,rules,cell) &&
                        !state.Units.Any(u => u.Alive && u.Cell == cell) && !state.Items.Any(i => i.Cell == cell) &&
                        !state.Structures.Any(s => s.Operational && s.Cell == cell)) candidates.Add(cell);
                }
                if (candidates.Count > 0) state.Items.Add(new GroundItem { Id = state.NextId++, Cell = candidates[random.Next(candidates.Count)] });
            }
        }
    }
}
