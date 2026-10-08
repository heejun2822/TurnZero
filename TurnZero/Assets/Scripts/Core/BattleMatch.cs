using System;
using System.Collections.Generic;
using System.Linq;

namespace TurnZero
{
    public sealed class BattleMatch
    {
        private readonly BattleRules rules;
        private BattleState state = new BattleState();
        private readonly Dictionary<int, ActionOrder>[] orders = { new Dictionary<int, ActionOrder>(), new Dictionary<int, ActionOrder>() };
        private readonly Dictionary<int, Observation>[] memory = { new Dictionary<int, Observation>(), new Dictionary<int, Observation>() };
        private readonly List<string>[] messages = { new List<string>(), new List<string>() };
        private readonly bool[] locked = new bool[2];
        private readonly BattleRecording recording;
        private int placementPlayer;
        private double deadline;

        public MatchPhase Phase => state.Phase;
        public int PlacementPlayer => placementPlayer;
        public int Turn => state.Turn;
        public double SecondsLeft(double now) => state.Phase == MatchPhase.Planning ? Math.Max(0, deadline - now) : 0;

        public BattleMatch(BattleRules configuration)
        {
            configuration.Validate();
            rules = configuration.Copy();
            recording = new BattleRecording { Rules = rules.Copy() };
        }

        public bool PlaceBase(int player, Cell cell, double now, out string error)
        {
            error = null;
            if (state.Phase != MatchPhase.Placement || player != placementPlayer || !rules.CanPlaceBase(player, cell))
            {
                error = "Place the base in a highlighted interior tile of your field.";
                return false;
            }
            state.Entities.Add(new EntityState { Id = 101 + player, Owner = player, Kind = EntityKind.Base,
                Position = cell, Health = rules.BaseHealth, MaximumHealth = rules.BaseHealth });
            var offsets = new[] { new Cell(0, 1), new Cell(1, 0), new Cell(0, -1), new Cell(-1, 0) };
            for (int i = 0; i < offsets.Length; i++)
                state.Entities.Add(new EntityState { Id = player * 4 + i + 1, Owner = player, Kind = EntityKind.Unit,
                    Position = new Cell(cell.X + offsets[i].X, cell.Y + offsets[i].Y), Health = rules.UnitHealth, MaximumHealth = rules.UnitHealth });
            recording.BasePlacements.Add(cell);
            placementPlayer++;
            if (placementPlayer == 2)
            {
                state.AP[0] = state.AP[1] = rules.InitialAP;
                state.Turn = 1;
                state.Phase = MatchPhase.Planning;
                deadline = now + rules.PlanningSeconds;
                RefreshMemory();
            }
            return true;
        }

        public bool QueueOrder(int player, int turn, ActionOrder order, out string error)
        {
            error = null;
            if (player < 0 || player > 1 || turn != state.Turn || state.Phase != MatchPhase.Planning || locked[player])
                error = "This planning turn is closed or your orders are locked.";
            else error = BattleResolver.ValidateOrder(state, rules, player, order);
            if (error != null) return false;
            int reserved = orders[player].Values.Where(o => o.UnitId != order.UnitId).Sum(o => rules.Cost(o.Kind)) + rules.Cost(order.Kind);
            if (reserved > state.AP[player]) { error = "Not enough unreserved AP. Change or cancel another order."; return false; }
            if (order.Kind == ActionKind.Wait) orders[player].Remove(order.UnitId);
            else orders[player][order.UnitId] = order.Copy();
            return true;
        }

        public bool LockOrders(int player, int turn)
        {
            if (player < 0 || player > 1 || state.Phase != MatchPhase.Planning || state.Turn != turn || locked[player]) return false;
            locked[player] = true;
            return true;
        }

        public bool Tick(double now)
        {
            if (state.Phase != MatchPhase.Planning || now < deadline) return false;
            ResolveTurn(now);
            return true;
        }

        // Explicit local testing control. Locking both players does not shorten the normal shared timer.
        public bool ResolveNow(double now)
        {
            if (state.Phase != MatchPhase.Planning) return false;
            ResolveTurn(now);
            return true;
        }

        private void ResolveTurn(double now)
        {
            var one = orders[0].Values.OrderBy(o => o.UnitId).Select(o => o.Copy()).ToList();
            var two = orders[1].Values.OrderBy(o => o.UnitId).Select(o => o.Copy()).ToList();
            var before = state.Copy();
            var result = BattleResolver.Resolve(state, rules, one, two);
            state = result.State;
            recording.Turns.Add(new TurnRecord { Before = before, PlayerOneOrders = one, PlayerTwoOrders = two, After = state.Copy() });
            for (int player = 0; player < 2; player++)
            {
                messages[player].Clear();
                messages[player].Add($"Turn {state.Turn} resolved.");
                messages[player].AddRange(result.Messages[player]);
                foreach (var observation in result.Observations[player]) memory[player][observation.Entity.Id] = observation.Copy();
                orders[player].Clear();
                locked[player] = false;
            }
            RefreshMemory();
            if (state.Phase == MatchPhase.Finished) return;
            state.Turn++;
            for (int player = 0; player < 2; player++) state.AP[player] = Math.Min(rules.MaximumAP, state.AP[player] + rules.TurnIncome);
            deadline = now + rules.PlanningSeconds;
        }

        private void RefreshMemory()
        {
            for (int player = 0; player < 2; player++)
            {
                var vision = BattleResolver.Vision(state, player, rules);
                foreach (var entity in state.Entities.Where(e => e.Owner != player && vision.Contains(e.Position)))
                    memory[player][entity.Id] = new Observation { Entity = entity.Copy(), SeenOnTurn = state.Turn };
                foreach (int id in memory[player].Keys.ToList())
                {
                    var observation = memory[player][id];
                    if (observation.Entity.Health <= 0 || vision.Contains(observation.Entity.Position) &&
                        !state.Entities.Any(e => e.Id == id && e.Health > 0 && e.Position == observation.Entity.Position))
                        memory[player].Remove(id);
                }
            }
        }

        public PlayerView View(int player, double now)
        {
            if (player < 0 || player > 1) throw new ArgumentOutOfRangeException(nameof(player));
            var vision = BattleResolver.Vision(state, player, rules);
            var view = new PlayerView { Player = player, Turn = state.Turn, Phase = state.Phase, Outcome = state.Outcome,
                AP = state.AP[player], ReservedAP = orders[player].Values.Sum(o => rules.Cost(o.Kind)), Locked = locked[player],
                SecondsLeft = SecondsLeft(now), VisibleCells = vision };
            foreach (var entity in state.Entities.OrderBy(e => e.Id))
                if (entity.Owner == player || vision.Contains(entity.Position) && entity.Health > 0) view.Entities.Add(entity.Copy());
            foreach (var observation in memory[player].Values.OrderBy(o => o.Entity.Id))
                if (!vision.Contains(observation.Entity.Position)) view.LastSeen.Add(observation.Copy());
            view.Orders.AddRange(orders[player].Values.OrderBy(o => o.UnitId).Select(o => o.Copy()));
            view.Messages.AddRange(messages[player]);
            return view;
        }

        // Development export contains BOTH players' private information; never send it to a match client.
        public BattleRecording ExportRecording()
        {
            var copy = new BattleRecording { Rules = rules.Copy(), BasePlacements = new List<Cell>(recording.BasePlacements) };
            foreach (var turn in recording.Turns)
                copy.Turns.Add(new TurnRecord { Before = turn.Before.Copy(), After = turn.After.Copy(),
                    PlayerOneOrders = turn.PlayerOneOrders.Select(o => o.Copy()).ToList(), PlayerTwoOrders = turn.PlayerTwoOrders.Select(o => o.Copy()).ToList() });
            return copy;
        }
    }
}
