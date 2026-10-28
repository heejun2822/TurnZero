using System;
using System.Linq;
namespace TurnZero.Game
{
    public static class BattleSimulation
    {
        public static BattleTimeline Resolve(MatchState input, GameRules rules)
        {
            rules.Validate();
            if (input.Phase != BattlePhase.Planning) throw new ArgumentException("Resolve a planning state.");
            var state = input.Copy(); var timeline = new BattleTimeline();
            var validation = state.Copy(); validation.Ready[0] = validation.Ready[1] = false;
            foreach (var order in state.Orders)
            {
                var actor = state.Units.Find(u => u.Id == order.UnitId);
                string error = BattleCommands.Validate(validation,rules,actor?.Owner ?? -1,order);
                if (error != null) throw new ArgumentException(error);
            }
            if (state.Orders.GroupBy(o => o.UnitId).Any(g => g.Count() > 1)) throw new ArgumentException("One order per character.");
            for (int player = 0; player < 2; player++)
                if (state.Orders.Where(o => state.Units.Find(u => u.Id == o.UnitId).Owner == player).Sum(o => BattleCommands.Cost(state,rules,o)) > state.AP[player])
                    throw new ArgumentException("Reserved costs exceed available AP.");
            var orders = state.Orders.OrderBy(o => o.UnitId).ToArray();
            for (int phase = 0; phase <= 4; phase++)
            {
                var group = orders.Where(o => BattleCommands.Priority(input,rules,o) == phase).ToArray();
                BattleRevival.Revive(state,rules,group.Where(o => o.Action == BattleAction.Revive).ToArray(),timeline);
                BattleMovement.Resolve(state,rules,group.Where(o => IsMovement(state,rules,o)).ToArray(),timeline,phase);
                BattleCombat.Resolve(state,rules,group.Where(o => o.Action == BattleAction.Attack || o.Action == BattleAction.Skill && !IsMovement(state,rules,o)).ToArray(),timeline,phase);
                BattleEquipment.Resolve(state,rules,group.Where(o => o.Action == BattleAction.Equipment).ToArray(),timeline,phase);
                var end = new ResolutionStep { Before = state.Copy(), Phase = phase };
                BattlePhaseCompletion.FinishPhase(state,rules,phase); timeline.Record(state,rules,end);
            }
            var wrap = new ResolutionStep { Before = state.Copy(), Phase = 5 };
            for (int team = 0; team < 2; team++)
            {
                int reward = state.PendingKillAP[team], delta = Math.Min(rules.MaximumAP,state.AP[team]+reward)-state.AP[team];
                state.AP[team] += delta; state.PendingKillAP[team] = 0;
                if (reward > 0) wrap.Cues.Add(new BattleCue { Kind = CueKind.Reward, Owner = team, Source = state.Bases[team].Cell,
                    Target = state.Bases[team].Cell, APDelta = delta, Label = $"KILL REWARD +{delta} AP" });
            }
            state.Fires.RemoveAll(f => f.ThroughTurn <= state.Turn); BattleStatusEffects.EndTurn(state);
            state.Winner = BattleVictory.Determine(state);
            timeline.Record(state,rules,wrap); timeline.Final = state.Copy(); return timeline;
        }
        private static bool IsMovement(MatchState state, GameRules rules, BattleOrder order) =>
            order.Action == BattleAction.Move || order.Action == BattleAction.Skill &&
            rules.Characters[state.Units.Find(u => u.Id == order.UnitId).Slot].SkillEffect == SkillEffect.StealthMove;
    }
}
