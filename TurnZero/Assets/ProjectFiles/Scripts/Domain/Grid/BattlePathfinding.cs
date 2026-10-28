using System.Collections.Generic;
using System.Linq;
namespace TurnZero
{
    public static class BattlePathfinding
    {
        public static List<GridCell> FindPath(MatchState state, GameRules rules, GridCell start, GridCell target, int maximum, int player)
        {
            if (start == target || !rules.Contains(target)) return new List<GridCell>();
            var distances = Search(state,rules,start,maximum,player,out var previous);
            var path = new List<GridCell>();
            if (!distances.ContainsKey(target)) return path;
            while (target != start) { path.Add(target); target = previous[target]; }
            path.Reverse(); return path;
        }
        public static HashSet<GridCell> Reachable(MatchState state, GameRules rules, GridCell start, int maximum, int player) =>
            new HashSet<GridCell>(Search(state,rules,start,maximum,player,out _).Keys.Where(c => c != start));
        private static Dictionary<GridCell,int> Search(MatchState state, GameRules rules, GridCell start, int maximum, int player, out Dictionary<GridCell,GridCell> previous)
        {
            var vision = player >= 0 ? BattleVisibility.Vision(state,rules,player) : null;
            var blocked = new HashSet<GridCell>(state.Bases.Where(b => b.Placed && (player < 0 || b.Owner == player || vision.Contains(b.Cell) || state.KnownBases[player]?.Cell == b.Cell)).Select(b => b.Cell));
            var frontier = new Queue<GridCell>();
            previous = new Dictionary<GridCell, GridCell>();
            var distance = new Dictionary<GridCell, int> { [start] = 0 };
            frontier.Enqueue(start);
            var directions = new[] { new GridCell(0,1), new GridCell(1,0), new GridCell(0,-1), new GridCell(-1,0) };
            while (frontier.Count > 0)
            {
                var cell = frontier.Dequeue();
                if (distance[cell] >= maximum) continue;
                foreach (var direction in directions)
                {
                    var next = new GridCell(cell.X + direction.X, cell.Y + direction.Y);
                    if (distance.ContainsKey(next) || !rules.Contains(next) || blocked.Contains(next)) continue;
                    distance[next] = distance[cell] + 1; previous[next] = cell;
                    frontier.Enqueue(next);
                }
            }
            return distance;
        }

    }
}
