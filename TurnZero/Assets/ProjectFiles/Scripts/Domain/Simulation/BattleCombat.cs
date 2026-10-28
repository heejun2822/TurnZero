using System;
using System.Linq;
namespace TurnZero
{
    public static class BattleCombat
    {
        public static void Resolve(MatchState state, GameRules rules, BattleOrder[] orders, BattleTimeline timeline, int phase)
        {
            var step = new ResolutionStep { Before = state.Copy(), Phase = phase };
            foreach (var fire in state.Fires.Where(f => f.Phase == phase && f.CreatedTurn < state.Turn && state.Turn <= f.ThroughTurn)
                .OrderBy(f => f.CreatedTurn).ThenBy(f => f.CreatorId).ThenBy(f => f.Id))
            {
                var cue = new BattleCue { Kind = CueKind.Skill, Owner = fire.Owner, ActorId = fire.CreatorId,
                    Source = fire.Cell, Target = fire.Cell, Area = fire.Area, Damage = fire.Damage, Label = "BURN",
                    Persistent = true, CreatedTurn = fire.CreatedTurn, EffectId = fire.Id };
                CaptureTargets(state,rules,cue); step.Cues.Add(cue);
            }
            foreach (var order in orders)
            {
                var actor = state.Units.Find(u => u.Id == order.UnitId);
                if (!actor.Alive || actor.Health <= 0 || !BattleCosts.TryStart(state,rules,order)) continue;
                var spec = BattleCommands.Spec(state,rules,order);
                var cue = new BattleCue { Kind = order.Action == BattleAction.Attack ? CueKind.Attack : CueKind.Skill,
                    ActorId = actor.Id, Owner = actor.Owner, Source = actor.Cell, Target = order.Target, Area = spec.Area,
                    Damage = spec.Damage, Label = spec.Name, APDelta = -spec.Cost };
                CaptureTargets(state,rules,cue);
                state.Revealed[actor.Owner].UnionWith(BattleVisibility.Area(rules,order.Target,spec.Area));
                if (order.Action == BattleAction.Skill && rules.Characters[actor.Slot].SkillEffect == SkillEffect.BurningGround)
                    state.Fires.Add(new FireZone { Id = state.NextId++, Owner = actor.Owner, CreatorId = actor.Id, CreatedTurn = state.Turn,
                        Phase = spec.Priority, Cell = order.Target, Area = spec.Area, Damage = spec.Damage,
                        ThroughTurn = state.Turn + rules.Characters[actor.Slot].SkillDuration - 1 });
                step.Cues.Add(cue);
            }
            if (step.Cues.Count == 0) return;
            foreach (var cue in step.Cues)
            {
                ApplyDamage(state,cue);
                if (cue.Persistent || cue.Kind != CueKind.Skill) continue;
                var actor = state.Units.Find(u => u.Id == cue.ActorId);
                if (rules.Characters[actor.Slot].SkillEffect == SkillEffect.RootDamage)
                    foreach (int id in cue.UnitDamage.Keys)
                    {
                        var target = state.Units.Find(u => u.Id == id);
                        if (target.Health > 0 && cue.UnitDamage[id] > 0) BattleStatusEffects.Apply(target,GameDefinitions.Root,state.Turn,actor.Id);
                    }
            }
            timeline.Record(state,rules,step);
        }

        private static void Damage(BattleUnit target, int damage)
        {
            if (target.Health <= 0 || damage <= 0) return;
            target.Health = Math.Max(0,target.Health - damage);
            if (target.Health == 0) target.DefeatPending = true;
        }

        private static void CaptureTargets(MatchState state, GameRules rules, BattleCue cue)
        {
            var area = BattleVisibility.Area(rules,cue.Target,cue.Area);
            var cells = new System.Collections.Generic.HashSet<GridCell>(area);
            foreach (var unit in state.Units.Where(u => u.Owner != cue.Owner && u.Alive && u.Health > 0 && cells.Contains(u.Cell)))
                cue.UnitDamage[unit.Id] = cue.Damage;
            var home = state.Bases[1-cue.Owner];
            if (home.Health > 0 && cells.Contains(home.Cell)) cue.BaseDamage[home.Owner] = cue.Damage;
            foreach (var structure in state.Structures.Where(s => s.Owner != cue.Owner && s.Operational && cells.Contains(s.Cell)))
                cue.StructureDamage[structure.Id] = cue.Damage;
        }

        public static void ApplyDamage(MatchState state, BattleCue cue)
        {
            foreach (var hit in cue.UnitDamage)
            {
                var target = state.Units.Find(u => u.Id == hit.Key);
                if (target != null) Damage(target,hit.Value);
            }
            for (int owner = 0; owner < GameDefinitions.PlayerCount; owner++) state.Bases[owner].Health = Math.Max(0,state.Bases[owner].Health-cue.BaseDamage[owner]);
            foreach (var hit in cue.StructureDamage)
            {
                var structure = state.Structures.Find(s => s.Id == hit.Key);
                if (structure == null) continue;
                structure.Health = Math.Max(0,structure.Health-hit.Value);
                if (structure.Health == 0) structure.PendingDestroy = true;
            }
            foreach (var destroyed in state.Structures.Where(s => s.PendingDestroy && s.Link > 0).ToArray())
                foreach (var linked in state.Structures.Where(s => s.Link == destroyed.Link)) { linked.Health = 0; linked.PendingDestroy = true; }
        }
    }
}
