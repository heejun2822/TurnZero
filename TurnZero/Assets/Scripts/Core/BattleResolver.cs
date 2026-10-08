using System;
using System.Collections.Generic;
using System.Linq;

namespace TurnZero
{
    public static class BattleResolver
    {
        public static HashSet<Cell> Vision(BattleState state, int player, BattleRules rules)
        {
            var cells = new HashSet<Cell>();
            foreach (var entity in state.Entities.Where(e => e.Owner == player && e.Health > 0))
                for (int y = -rules.VisionRange; y <= rules.VisionRange; y++)
                    for (int x = -rules.VisionRange; x <= rules.VisionRange; x++)
                    {
                        var cell = new Cell(entity.Position.X + x, entity.Position.Y + y);
                        if (rules.Contains(cell) && Math.Abs(x) + Math.Abs(y) <= rules.VisionRange) cells.Add(cell);
                    }
            return cells;
        }

        public static string ValidateOrder(BattleState state, BattleRules rules, int player, ActionOrder order)
        {
            if (player < 0 || player > 1 || order == null || state.Phase != MatchPhase.Planning)
                return "Orders are only available during planning.";
            var actor = state.Entities.Find(e => e.Id == order.UnitId && e.Owner == player && e.Kind == EntityKind.Unit && e.Health > 0);
            if (actor == null) return "Choose a living allied unit.";
            if (order.Kind == ActionKind.Wait) return null;
            if (order.Kind == ActionKind.Move)
                return rules.Contains(order.Destination) && actor.Position.Distance(order.Destination) == 1 ? null : "Move one tile horizontally or vertically.";
            if (order.Kind != ActionKind.Attack) return "Unknown action.";
            var target = state.Entities.Find(e => e.Id == order.TargetId && e.Owner != player && e.Health > 0);
            if (target == null || !Vision(state, player, rules).Contains(target.Position) || actor.Position.Distance(target.Position) > rules.AttackRange)
                return "Choose a visible enemy within attack range.";
            return null;
        }

        // Input state is never changed. Stable entity order makes recording/replay independent of submission order.
        public static TurnResult Resolve(BattleState input, BattleRules rules, IReadOnlyList<ActionOrder> one, IReadOnlyList<ActionOrder> two)
        {
            rules.Validate();
            if (input.Phase != MatchPhase.Planning) throw new ArgumentException("A turn must start in planning.");
            var state = input.Copy();
            state.Entities.Sort((a, b) => a.Id.CompareTo(b.Id));
            var result = new TurnResult { State = state };
            var orders = new List<ActionOrder>();
            var byPlayer = new[] { one, two };
            for (int player = 0; player < 2; player++)
            {
                var ids = new HashSet<int>();
                int cost = 0;
                foreach (var order in byPlayer[player])
                {
                    string error = ValidateOrder(input, rules, player, order);
                    if (error != null) throw new ArgumentException(error);
                    if (!ids.Add(order.UnitId)) throw new ArgumentException("Only one order per unit is allowed.");
                    cost += rules.Cost(order.Kind);
                    orders.Add(order.Copy());
                }
                if (cost > state.AP[player]) throw new ArgumentException("Orders exceed available AP.");
                state.AP[player] -= cost;
                Observe(state, player, Vision(state, player, rules), result);
            }
            orders.Sort((a, b) => a.UnitId.CompareTo(b.UnitId));

            var startVision = new[] { Vision(state, 0, rules), Vision(state, 1, rules) };
            var occupied = new HashSet<Cell>(state.Entities.Where(e => e.Health > 0).Select(e => e.Position));
            var moves = orders.Where(o => o.Kind == ActionKind.Move).ToList();
            var destinations = moves.GroupBy(o => o.Destination).ToDictionary(g => g.Key, g => g.Count());
            var accepted = new List<Tuple<EntityState, Cell, Cell>>();
            foreach (var move in moves)
            {
                var actor = state.Entities.Find(e => e.Id == move.UnitId);
                if (occupied.Contains(move.Destination) || destinations[move.Destination] > 1)
                    result.Messages[actor.Owner].Add($"{actor.Label}: move blocked; AP spent.");
                else accepted.Add(Tuple.Create(actor, actor.Position, move.Destination));
            }
            foreach (var move in accepted) move.Item1.Position = move.Item3;
            var attackVision = new[] { Vision(state, 0, rules), Vision(state, 1, rules) };
            foreach (var move in accepted)
                for (int player = 0; player < 2; player++)
                {
                    bool from = startVision[player].Contains(move.Item2);
                    bool to = attackVision[player].Contains(move.Item3);
                    if (move.Item1.Owner == player || from && to)
                        result.Messages[player].Add($"{move.Item1.Label}: {move.Item2} -> {move.Item3}.");
                    else if (from) result.Messages[player].Add($"{move.Item1.Label} left vision.");
                    else if (to) result.Messages[player].Add($"{move.Item1.Label} appeared at {move.Item3}.");
                }
            for (int player = 0; player < 2; player++) Observe(state, player, attackVision[player], result);

            var damage = new Dictionary<int, int>();
            foreach (var order in orders.Where(o => o.Kind == ActionKind.Attack))
            {
                var actor = state.Entities.Find(e => e.Id == order.UnitId);
                var target = state.Entities.Find(e => e.Id == order.TargetId && e.Health > 0);
                if (target == null || !attackVision[actor.Owner].Contains(target.Position) || actor.Position.Distance(target.Position) > rules.AttackRange)
                {
                    result.Messages[actor.Owner].Add($"{actor.Label}: attack failed; AP spent.");
                    continue;
                }
                damage[target.Id] = damage.TryGetValue(target.Id, out int total) ? total + rules.AttackDamage : rules.AttackDamage;
                for (int player = 0; player < 2; player++)
                    if (attackVision[player].Contains(actor.Position) && attackVision[player].Contains(target.Position))
                        result.Messages[player].Add($"{actor.Label} hit {target.Label} for {rules.AttackDamage}.");
            }
            foreach (var hit in damage.OrderBy(h => h.Key))
            {
                var target = state.Entities.Find(e => e.Id == hit.Key);
                target.Health = Math.Max(0, target.Health - hit.Value);
                if (target.Health == 0)
                    for (int player = 0; player < 2; player++)
                        if (attackVision[player].Contains(target.Position)) result.Messages[player].Add($"{target.Label} destroyed.");
            }
            // An observed death remains known even if the observer also died in this damage batch.
            for (int player = 0; player < 2; player++) Observe(state, player, attackVision[player], result);

            bool oneLost = Lost(state, 0);
            bool twoLost = Lost(state, 1);
            if (oneLost || twoLost)
            {
                state.Outcome = oneLost && twoLost ? BattleOutcome.Draw : oneLost ? BattleOutcome.PlayerTwo : BattleOutcome.PlayerOne;
                state.Phase = MatchPhase.Finished;
            }
            return result;
        }

        private static bool Lost(BattleState state, int player) =>
            !state.Entities.Any(e => e.Owner == player && e.Kind == EntityKind.Base && e.Health > 0) ||
            !state.Entities.Any(e => e.Owner == player && e.Kind == EntityKind.Unit && e.Health > 0);

        private static void Observe(BattleState state, int player, HashSet<Cell> vision, TurnResult result)
        {
            foreach (var entity in state.Entities.Where(e => e.Owner != player && vision.Contains(e.Position)))
            {
                result.Observations[player].RemoveAll(o => o.Entity.Id == entity.Id);
                result.Observations[player].Add(new Observation { Entity = entity.Copy(), SeenOnTurn = state.Turn });
            }
        }
    }
}
