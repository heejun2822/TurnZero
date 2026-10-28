using System;
using System.Linq;
namespace TurnZero.Game
{
    public static class BattleRevival
    {
        public static void Revive(MatchState state, GameRules rules, BattleOrder[] orders, BattleTimeline timeline)
        {
            var valid = orders.Where(o => {
                var unit = state.Units.Find(u => u.Id == o.UnitId);
                return unit != null && !unit.Alive && unit.DiedTurn < state.Turn && state.Units.Any(u => u.Owner == unit.Owner && u.Alive) &&
                    rules.Home(unit.Owner,o.Target) && BattleCommands.Movable(state,rules,o.Target) &&
                    state.Bases[unit.Owner].Cell.Distance(o.Target) <= rules.BaseVision;
            }).ToArray();
            if (valid.Length == 0) return;
            var step = new ResolutionStep { Before = state.Copy(), Phase = 0 };
            foreach (var order in valid)
            {
                var unit = state.Units.Find(u => u.Id == order.UnitId);
                if (!BattleCosts.TryStart(state,rules,order)) continue;
                unit.Health = rules.Characters[unit.Slot].Health; unit.Cell = order.Target; unit.Placed = true;
                unit.DefeatPending = false; unit.DiedTurn = 0; unit.ReadyOn = new int[4]; BattleStatusEffects.Clear(unit);
                step.Cues.Add(new BattleCue { Kind = CueKind.Revive, ActorId = unit.Id, Owner = unit.Owner, Source = unit.Cell, Target = unit.Cell, Label = "REVIVE", APDelta = -BattleCommands.Cost(step.Before,rules,order) });
            }
            BattleItems.Collect(state,valid.Select(o => state.Units.Find(u => u.Id == o.UnitId)));
            timeline.Record(state,rules,step);
        }
    }
}
