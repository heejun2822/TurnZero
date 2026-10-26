using System;
using System.Collections.Generic;
using System.Linq;

namespace TurnZero.Game
{
    public sealed class LocalBattleMatch
    {
        public GameRules Rules { get; }
        private MatchState state;
        public BattlePhase Phase => state.Phase;
        public int PlacementPlayer => state.PlacementPlayer;
        public int Turn => state.Turn;
        public bool BothReady => state.Ready.All(r => r);
        public LocalBattleMatch(GameRules rules)
        {
            rules.Validate(); Rules = rules.Copy(); state = new MatchState();
            for (int player = 0; player < 2; player++)
            {
                state.Materials[player] = Rules.InitialMaterials;
                for (int slot = 0; slot < 5; slot++) state.Units.Add(new BattleUnit { Id = player * 5 + slot + 1, Owner = player, Slot = slot, Health = Rules.Characters[slot].Health });
                for (int y = 0; y < Rules.Height; y++)
                    for (int x = 0; x < Rules.Width; x++)
                        if (Rules.Home(player, new GridCell(x,y)) || y >= Rules.HomeHeight && y < Rules.HomeHeight + Rules.NeutralHeight)
                            state.Explored[player].Add(new GridCell(x,y));
            }
        }
        public BattleView View(int player, double now, bool revealAll = false)
        {
            if (player < 0 || player > 1) throw new ArgumentOutOfRangeException(nameof(player));
            return BattleVisibility.View(state, Rules, player, now, revealAll);
        }
        // Full state is only for local diagnostics, recordings and deterministic tests.
        public MatchState DebugSnapshot() => state.Copy();
        public void BeginPlacement()
        {
            if (state.Phase == BattlePhase.Setup) state.Phase = BattlePhase.BasePlacement;
        }
        public bool Place(int player, GridCell cell, double now, out string error)
        {
            error = null;
            if (player != state.PlacementPlayer || !Rules.Home(player, cell) || BattleCommands.BaseAt(state, cell)) error = "Place on an empty tile in your own field.";
            else if (state.Phase == BattlePhase.BasePlacement)
            {
                var home = state.Bases[player]; home.Placed = true; home.Cell = cell; home.Health = Rules.BaseHealth;
                state.Phase = BattlePhase.UnitPlacement; state.PlacementSlot = 0;
            }
            else if (state.Phase == BattlePhase.UnitPlacement)
            {
                if (state.Bases[player].Cell.Distance(cell) > Rules.BaseVision) error = "Deploy inside base vision (3 tiles).";
                else
                {
                    var unit = state.Units.Find(u => u.Owner == player && u.Slot == state.PlacementSlot);
                    unit.Cell = cell; unit.Placed = true; state.PlacementSlot++;
                    if (state.PlacementSlot == 5)
                    {
                        if (player == 0) { state.PlacementPlayer = 1; state.Phase = BattlePhase.BasePlacement; state.PlacementSlot = 0; }
                        else StartTurn(now);
                    }
                }
            }
            else error = "Deployment is finished.";
            if (error != null) return false;
            BattleVisibility.Observe(state, Rules); return true;
        }
        public void AutoDeploy(double now)
        {
            if (state.Phase == BattlePhase.Setup) BeginPlacement();
            while (state.Phase == BattlePhase.BasePlacement || state.Phase == BattlePhase.UnitPlacement)
            {
                int player = state.PlacementPlayer;
                var center = new GridCell(Rules.Width / 2, player == 0 ? Rules.HomeHeight - 3 : Rules.HomeHeight + Rules.NeutralHeight + 2);
                var cell = state.Phase == BattlePhase.BasePlacement ? center : new GridCell(center.X + state.PlacementSlot % 3 - 1,
                    center.Y + (player == 0 ? 1 : -1) * (state.PlacementSlot / 3 + 1));
                if (!Place(player, cell, now, out string error)) throw new InvalidOperationException(error);
            }
        }
        public bool Queue(int player, int turn, BattleOrder order, out string error)
        {
            error = turn != state.Turn ? "The submitted turn has ended." : BattleCommands.Validate(state, Rules, player, order);
            if (error != null) return false;
            int reserved = state.Orders.Where(o => o.UnitId != order.UnitId && state.Units.Find(u => u.Id == o.UnitId).Owner == player).Sum(o => BattleCommands.Cost(state, Rules, o));
            if (reserved + BattleCommands.Cost(state, Rules, order) > state.AP[player]) { error = "Not enough available AP. Cancel or replace another action."; return false; }
            state.Orders.RemoveAll(o => o.UnitId == order.UnitId);
            if (order.Action != BattleAction.Wait) state.Orders.Add(order.Copy());
            return true;
        }
        public List<GridCell> Path(int unitId, GridCell target, bool skill = false)
        {
            var unit = state.Units.Find(u => u.Id == unitId);
            if (unit == null || !unit.Alive) return new List<GridCell>();
            int range = skill ? Rules.Characters[unit.Slot].Skill.Range : Rules.Characters[unit.Slot].Move.Range;
            return BattleCommands.FindPath(state, Rules, unit.Cell, target, range, unit.Owner);
        }
        public bool Ready(int player)
        {
            if (player < 0 || player > 1 || state.Phase != BattlePhase.Planning || state.Ready[player]) return false;
            state.Ready[player] = true; return true;
        }
        public HashSet<GridCell> Reachable(int unitId, bool skill = false)
        {
            var unit = state.Units.Find(u => u.Id == unitId);
            if (unit == null || !unit.Alive || unit.Rooted(state.Turn)) return new HashSet<GridCell>();
            int range = skill ? Rules.Characters[unit.Slot].Skill.Range : Rules.Characters[unit.Slot].Move.Range;
            return BattleCommands.Reachable(state,Rules,unit.Cell,range,unit.Owner);
        }
        public bool Craft(int player, int unitId, int equipment, bool replace, out string error)
        {
            error = null;
            var unit = state.Units.Find(u => u.Owner == player && u.Id == unitId && u.Alive);
            if (state.Phase != BattlePhase.Planning || state.Ready[player] || unit == null) error = "Craft for a living ally during planning.";
            else if (equipment < 0 || equipment >= Rules.Equipment.Length || state.Materials[player] < 1) error = "No crafting material or invalid equipment.";
            else if (unit.Equipment >= 0 && !replace) error = "Replacing equipment destroys the current item. Confirm replacement.";
            if (error != null) return false;
            state.Orders.RemoveAll(o => o.UnitId == unitId && o.Action == BattleAction.Equipment);
            state.Materials[player]--; unit.Equipment = equipment; unit.ReadyOn[(int)BattleAction.Equipment] = 0;
            return true;
        }
        public bool Tick(double now)
        {
            if (state.Phase != BattlePhase.TurnStart || now < state.Deadline) return false;
            state.Phase = BattlePhase.Planning; state.Deadline = now + Rules.PlanningSeconds; return true;
        }
        public bool ShouldResolve(double now) => state.Phase == BattlePhase.Planning && (BothReady || now >= state.Deadline);
        public BattleTimeline Resolve()
        {
            if (state.Phase != BattlePhase.Planning) return null;
            var timeline = BattleSimulation.Resolve(state, Rules);
            state = timeline.Final.Copy(); state.Phase = BattlePhase.Resolving;
            return timeline;
        }
        public bool CompleteResolution(double now)
        {
            if (state.Phase != BattlePhase.Resolving) return false;
            state.Orders.Clear(); state.Ready[0] = state.Ready[1] = false;
            state.Items.RemoveAll(i => i.Collected.Any(c => c));
            state.Structures.RemoveAll(s => s.Health <= 0);
            if (state.Winner != BattleWinner.None) { state.Phase = BattlePhase.Finished; return true; }
            StartTurn(now); return true;
        }
        private void StartTurn(double now)
        {
            state.Turn++; state.Phase = BattlePhase.TurnStart; state.Deadline = now + Rules.TurnStartSeconds;
            state.FireHits.Clear(); state.Fires.RemoveAll(f => f.ThroughTurn < state.Turn);
            for (int player = 0; player < 2; player++) { state.AP[player] = Math.Min(Rules.MaximumAP, state.AP[player] + Rules.TurnIncome); state.Revealed[player].Clear(); }
            SpawnMaterials(); BattleVisibility.Observe(state, Rules);
        }
        private void SpawnMaterials()
        {
            if (state.Turn % 5 != 0) return;
            var random = new Random(unchecked(Rules.Seed + state.Turn * 7919));
            for (int region = 0; region < 3; region++)
            {
                int start = region == 0 ? 0 : region == 1 ? Rules.HomeHeight : Rules.HomeHeight + Rules.NeutralHeight;
                int height = region == 1 ? Rules.NeutralHeight : Rules.HomeHeight;
                var candidates = new List<GridCell>();
                for (int y = start; y < start + height; y++) for (int x = 0; x < Rules.Width; x++)
                {
                    var cell = new GridCell(x,y);
                    if (BattleCommands.Movable(state, Rules, cell) && !state.Units.Any(u => u.Alive && u.Cell == cell) && !state.Items.Any(i => i.Cell == cell) && !state.Structures.Any(s => s.Health > 0 && s.Cell == cell)) candidates.Add(cell);
                }
                if (candidates.Count > 0) state.Items.Add(new GroundItem { Id = state.NextId++, Cell = candidates[random.Next(candidates.Count)] });
            }
        }
    }
}
