using System.Collections.Generic;

namespace TurnZero
{
    public static class BattlePathfinding
    {
        private static readonly GridCell[] Directions =
        {
            new GridCell(0, 1),
            new GridCell(1, 0),
            new GridCell(0, -1),
            new GridCell(-1, 0)
        };

        public static List<GridCell> FindPath(MatchState state, GameRules rules, GridCell start, GridCell target, int maximum, int player)
        {
            if (start == target || !rules.Contains(target))
            {
                return new List<GridCell>();
            }

            Dictionary<GridCell, int> distances = Search(state, rules, start, maximum, player, out Dictionary<GridCell, GridCell> previous);
            List<GridCell> path = new List<GridCell>();

            if (!distances.ContainsKey(target))
            {
                return path;
            }

            while (target != start)
            {
                path.Add(target);
                target = previous[target];
            }

            path.Reverse();

            return path;
        }

        public static HashSet<GridCell> Reachable(MatchState state, GameRules rules, GridCell start, int maximum, int player)
        {
            HashSet<GridCell> reachable = new HashSet<GridCell>(Search(state, rules, start, maximum, player, out _).Keys);

            reachable.Remove(start);

            return reachable;
        }

        private static Dictionary<GridCell, int> Search(
            MatchState state,
            GameRules rules,
            GridCell start,
            int maximum,
            int player,
            out Dictionary<GridCell, GridCell> previous)
        {
            HashSet<GridCell> vision = player >= 0 ? BattleVisibility.Vision(state, rules, player) : null;
            HashSet<GridCell> blocked = new HashSet<GridCell>();

            foreach (BattleBase home in state.Bases)
            {
                if (home.Placed &&
                    (player < 0 || home.Owner == player || vision.Contains(home.Cell) || state.KnownBases[player]?.Cell == home.Cell))
                {
                    blocked.Add(home.Cell);
                }
            }

            return Search(rules, new[] { start }, maximum, blocked, out previous);
        }

        public static Dictionary<GridCell, List<GridCell>> Paths(GameRules rules, GridCell start, int maximum, ISet<GridCell> blocked)
        {
            Dictionary<GridCell, int> distances = Search(rules, new[] { start }, maximum, blocked, out Dictionary<GridCell, GridCell> previous);
            Dictionary<GridCell, List<GridCell>> paths = new Dictionary<GridCell, List<GridCell>>();

            foreach (GridCell destination in distances.Keys)
            {
                if (destination == start)
                {
                    continue;
                }

                List<GridCell> path = new List<GridCell>();
                GridCell current = destination;

                while (current != start)
                {
                    path.Add(current);
                    current = previous[current];
                }

                path.Reverse();
                paths.Add(destination, path);
            }

            return paths;
        }

        public static Dictionary<GridCell, int> Distances(GameRules rules, IEnumerable<GridCell> goals, ISet<GridCell> blocked)
        {
            return Search(rules, goals, rules.Width * rules.Height, blocked, out _);
        }

        private static Dictionary<GridCell, int> Search(
            GameRules rules,
            IEnumerable<GridCell> starts,
            int maximum,
            ISet<GridCell> blocked,
            out Dictionary<GridCell, GridCell> previous)
        {
            Queue<GridCell> frontier = new Queue<GridCell>();

            previous = new Dictionary<GridCell, GridCell>();
            Dictionary<GridCell, int> distance = new Dictionary<GridCell, int>();

            foreach (GridCell start in starts)
            {
                if (rules.Contains(start) && !distance.ContainsKey(start))
                {
                    distance.Add(start, 0);
                    frontier.Enqueue(start);
                }
            }

            while (frontier.Count > 0)
            {
                GridCell cell = frontier.Dequeue();

                if (distance[cell] >= maximum)
                {
                    continue;
                }

                foreach (GridCell direction in Directions)
                {
                    GridCell next = new GridCell(cell.X + direction.X, cell.Y + direction.Y);

                    if (distance.ContainsKey(next) || !rules.Contains(next) || blocked.Contains(next))
                    {
                        continue;
                    }

                    distance[next] = distance[cell] + 1;
                    previous[next] = cell;

                    frontier.Enqueue(next);
                }
            }

            return distance;
        }
    }
}
