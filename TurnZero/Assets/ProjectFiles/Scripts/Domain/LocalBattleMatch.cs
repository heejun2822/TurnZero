using System;
using System.Collections.Generic;
using System.Linq;

namespace TurnZero
{
    public sealed class LocalBattleMatch
    {
        public GameRules Rules { get; }

        private MatchState state;

        public BattlePhase Phase => state.Phase;
        public int PlacementPlayer => state.PlacementPlayer;
        public int Turn => state.Turn;
        public int Revision { get; private set; }
        public double Deadline => state.Deadline;
        public bool BothReady => state.Ready.All(r => r);

        public LocalBattleMatch(GameRules rules)
        {
            rules.Validate();
            Rules = rules.Copy();
            state = new MatchState();

            for (int player = 0; player < GameDefinitions.PlayerCount; player++)
            {
                state.Materials[player] = Rules.InitialMaterials;
                state.EquipmentDecks[player] = Enumerable.Range(0, Math.Min(GameDefinitions.EquipmentDeckSlots, Rules.Equipment.Length)).ToArray();

                for (int slot = 0; slot < GameDefinitions.CharactersPerTeam; slot++)
                {
                    state.Units.Add(new BattleUnit
                    {
                        Id = player * GameDefinitions.CharactersPerTeam + slot + 1,
                        Owner = player,
                        Slot = slot,
                        Health = Rules.Characters[slot].Health
                    });
                }

                for (int y = 0; y < Rules.Height; y++)
                {
                    for (int x = 0; x < Rules.Width; x++)
                    {
                        if (Rules.Home(player, new GridCell(x, y)) || y >= Rules.HomeHeight && y < Rules.HomeHeight + Rules.NeutralHeight)
                        {
                            state.Explored[player].Add(new GridCell(x, y));
                        }
                    }
                }
            }
        }

        public BattleView View(int player, double now, bool revealAll = false)
        {
            if (player < 0 || player >= GameDefinitions.PlayerCount)
            {
                throw new ArgumentOutOfRangeException(nameof(player));
            }

            return BattleVisibility.View(state, Rules, player, now, revealAll);
        }

        // Full state is only for local diagnostics, recordings and deterministic tests.
        public MatchState DebugSnapshot()
        {
            return state.Copy();
        }

        public void BeginPlacement()
        {
            if (state.Phase == BattlePhase.Setup)
            {
                state.Phase = BattlePhase.BasePlacement;
                Revision++;
            }
        }

        public bool Place(int player, GridCell cell, double now, out string error)
        {
            error = null;

            if (player != state.PlacementPlayer || !Rules.Home(player, cell) || BattleCommands.BaseAt(state, cell))
            {
                error = "Place on an empty tile in your own field.";
            }
            else if (state.Phase == BattlePhase.BasePlacement)
            {
                BattleBase home = state.Bases[player];

                home.Placed = true;
                home.Cell = cell;
                home.Health = Rules.BaseHealth;

                state.Phase = BattlePhase.UnitPlacement;
                state.PlacementSlot = 0;
            }
            else if (state.Phase == BattlePhase.UnitPlacement)
            {
                if (state.Bases[player].Cell.Distance(cell) > Rules.BaseVision)
                {
                    error = "Deploy inside base vision (3 tiles).";
                }
                else
                {
                    BattleUnit unit = state.Units.Find(u => u.Owner == player && u.Slot == state.PlacementSlot);

                    unit.Cell = cell;
                    unit.Placed = true;
                    state.PlacementSlot++;

                    if (state.PlacementSlot == GameDefinitions.CharactersPerTeam)
                    {
                        if (player == 0)
                        {
                            state.PlacementPlayer = 1;
                            state.Phase = BattlePhase.BasePlacement;
                            state.PlacementSlot = 0;
                        }
                        else
                        {
                            StartTurn(now);
                        }
                    }
                }
            }
            else
            {
                error = "Deployment is finished.";
            }

            if (error != null)
            {
                return false;
            }

            BattleVisibility.Observe(state, Rules);

            Revision++;
            return true;
        }

        public void AutoDeploy(double now)
        {
            if (state.Phase == BattlePhase.Setup)
            {
                BeginPlacement();
            }

            while (state.Phase == BattlePhase.BasePlacement || state.Phase == BattlePhase.UnitPlacement)
            {
                int player = state.PlacementPlayer;
                GridCell center = new GridCell(Rules.Width / 2, player == 0 ? Rules.HomeHeight - 3 : Rules.HomeHeight + Rules.NeutralHeight + 2);
                GridCell cell = state.Phase == BattlePhase.BasePlacement
                    ? center
                    : new GridCell(center.X + state.PlacementSlot % 3 - 1,
                        center.Y + (player == 0 ? 1 : -1) * (state.PlacementSlot / 3 + 1));

                if (!Place(player, cell, now, out string error))
                {
                    throw new InvalidOperationException(error);
                }
            }
        }

        public bool Queue(int player, int turn, BattleOrder order, out string error, double? now = null)
        {
            error = now.HasValue && now.Value >= state.Deadline
                ? "Planning is closed."
                : turn != state.Turn ? "The submitted turn has ended." : BattleCommands.Validate(state, Rules, player, order);

            if (error != null)
            {
                return false;
            }

            int reserved = state.Orders.Where(o => o.UnitId != order.UnitId && state.Units.Find(u => u.Id == o.UnitId).Owner == player)
                .Sum(o => BattleCommands.Cost(state, Rules, o));

            if (reserved + BattleCommands.Cost(state, Rules, order) > state.AP[player])
            {
                error = "Not enough available AP. Cancel or replace another action.";

                return false;
            }

            state.Orders.RemoveAll(o => o.UnitId == order.UnitId);

            if (order.Action != BattleAction.Wait)
            {
                state.Orders.Add(order.Copy());
            }

            Revision++;
            return true;
        }

        public List<GridCell> Path(int unitId, GridCell target, bool skill = false)
        {
            BattleUnit unit = state.Units.Find(u => u.Id == unitId);

            if (unit == null || !unit.Alive)
            {
                return new List<GridCell>();
            }

            int range = skill ? Rules.Characters[unit.Slot].Skill.Range : Rules.Characters[unit.Slot].Move.Range;

            return BattlePathfinding.FindPath(state, Rules, unit.Cell, target, range, unit.Owner);
        }

        public bool Ready(int player, double? now = null)
        {
            if (player < 0 ||
                player >= GameDefinitions.PlayerCount ||
                state.Phase != BattlePhase.Planning ||
                BothReady ||
                now.HasValue &&
                now.Value >= state.Deadline)
            {
                return false;
            }

            state.Ready[player] = !state.Ready[player];

            Revision++;
            return true;
        }

        public bool SetEquipmentDeck(int player, int[] deck)
        {
            if (state.Phase != BattlePhase.Setup ||
                player < 0 ||
                player >= GameDefinitions.PlayerCount ||
                deck == null ||
                deck.Length < 1 ||
                deck.Length > GameDefinitions.EquipmentDeckSlots ||
                deck.Distinct().Count() != deck.Length ||
                deck.Any(i => i < 0 || i >= Rules.Equipment.Length))
            {
                return false;
            }

            state.EquipmentDecks[player] = (int[])deck.Clone();

            Revision++;
            return true;
        }

        public HashSet<GridCell> Reachable(int unitId, bool skill = false)
        {
            BattleUnit unit = state.Units.Find(u => u.Id == unitId);

            if (unit == null || !unit.Alive || unit.Rooted(state.Turn))
            {
                return new HashSet<GridCell>();
            }

            int range = skill ? Rules.Characters[unit.Slot].Skill.Range : Rules.Characters[unit.Slot].Move.Range;

            return BattlePathfinding.Reachable(state, Rules, unit.Cell, range, unit.Owner);
        }

        public bool Craft(int player, int unitId, int equipment, bool replace, out string error, double? now = null)
        {
            error = null;
            BattleUnit unit = state.Units.Find(u => u.Owner == player && u.Id == unitId && u.Alive);

            if (player < 0 ||
                player >= GameDefinitions.PlayerCount ||
                state.Phase != BattlePhase.Planning ||
                state.Ready[player] ||
                unit == null ||
                now.HasValue &&
                now.Value >= state.Deadline)
            {
                error = "Craft for a living ally during planning.";
            }
            else if (equipment < 0 ||
                equipment >= Rules.Equipment.Length ||
                !state.EquipmentDecks[player].Contains(equipment) ||
                state.Materials[player] < 1)
            {
                error = "No crafting material or invalid equipment.";
            }
            else if (unit.Equipment >= 0 && !replace)
            {
                error = "Replacing equipment destroys the current item. Confirm replacement.";
            }

            if (error != null)
            {
                return false;
            }

            state.Orders.RemoveAll(o => o.UnitId == unitId && o.Action == BattleAction.Equipment);

            state.Materials[player]--;
            unit.Equipment = equipment;
            unit.ReadyOn[(int)BattleAction.Equipment] = 0;

            Revision++;
            return true;
        }

        public bool Tick(double now)
        {
            if (state.Phase != BattlePhase.TurnStart || now < state.Deadline)
            {
                return false;
            }

            state.Phase = BattlePhase.Planning;
            state.Deadline = now + Rules.PlanningSeconds;

            Revision++;
            return true;
        }

        public bool ShouldResolve(double now)
        {
            return state.Phase == BattlePhase.Planning && (BothReady || now >= state.Deadline);
        }

        public BattleTimeline Resolve()
        {
            if (state.Phase != BattlePhase.Planning)
            {
                return null;
            }

            BattleTimeline timeline = BattleSimulation.Resolve(state, Rules);

            state = timeline.Final.Copy();
            state.Phase = BattlePhase.Resolving;

            Revision++;
            return timeline;
        }

        public bool CompleteResolution(double now)
        {
            if (state.Phase != BattlePhase.Resolving)
            {
                return false;
            }

            state.Orders.Clear();
            state.Ready[0] = state.Ready[1] = false;

            state.Structures.RemoveAll(s => s.Health <= 0);

            foreach (HashSet<GridCell> revealed in state.Revealed)
            {
                revealed.Clear();
            }

            BattleVisibility.Observe(state, Rules);

            if (state.Winner != BattleWinner.None)
            {
                state.Phase = BattlePhase.Finished;

                Revision++;
                return true;
            }

            StartTurn(now);

            Revision++;
            return true;
        }

        private void StartTurn(double now)
        {
            state.Turn++;
            state.Phase = BattlePhase.TurnStart;
            state.Deadline = now + Rules.TurnStartSeconds;

            state.Fires.RemoveAll(f => f.ThroughTurn < state.Turn);

            for (int player = 0; player < GameDefinitions.PlayerCount; player++)
            {
                state.AP[player] = Math.Min(Rules.MaximumAP, state.AP[player] + Rules.TurnIncome);
            }

            BattleItems.Spawn(state, Rules);
            BattleVisibility.Observe(state, Rules);
        }
    }
}
