using System;
using System.Collections.Generic;
using System.Linq;

namespace TurnZero.Game
{
    public static class BattleSimulation
    {
        public static BattleTimeline Resolve(MatchState input, GameRules rules)
        {
            rules.Validate();
            if (input.Phase != BattlePhase.Planning) throw new ArgumentException("Resolve a planning state.");
            var state = input.Copy();
            var timeline = new BattleTimeline();
            // Readiness locks editing, not validation of the already accepted orders.
            var validation = state.Copy(); validation.Ready[0] = validation.Ready[1] = false;
            foreach (var order in state.Orders)
            {
                var actor = state.Units.Find(u => u.Id == order.UnitId);
                string error = BattleCommands.Validate(validation, rules, actor?.Owner ?? -1, order);
                if (error != null) throw new ArgumentException(error);
            }
            if (state.Orders.GroupBy(o => o.UnitId).Any(g => g.Count() > 1)) throw new ArgumentException("One order per character.");
            for (int player = 0; player < 2; player++)
                if (state.Orders.Where(o => state.Units.Find(u => u.Id == o.UnitId).Owner == player).Sum(o => BattleCommands.Cost(state, rules, o)) > state.AP[player])
                    throw new ArgumentException("Reserved costs exceed available AP.");
            var orders = state.Orders.OrderBy(o => o.UnitId).ToArray();
            for (int priority = 1; priority <= 4; priority++)
            {
                var group = orders.Where(o => BattleCommands.Priority(input, rules, o) == priority).ToArray();
                foreach (var order in group.Where(o => o.Action == BattleAction.Revive)) Revive(state, rules, order, timeline);
                Move(state, rules, group.Where(o => o.Action == BattleAction.Move || o.Action == BattleAction.Skill && rules.Characters[state.Units.Find(u => u.Id == o.UnitId).Slot].SkillEffect == SkillEffect.StealthMove).ToArray(), timeline);
                if (priority == 2) SettleFire(state, rules, timeline);
                Attack(state, rules, group.Where(o => o.Action == BattleAction.Attack || o.Action == BattleAction.Skill && rules.Characters[state.Units.Find(u => u.Id == o.UnitId).Slot].SkillEffect != SkillEffect.StealthMove).ToArray(), timeline);
                foreach (var order in group.Where(o => o.Action == BattleAction.Equipment)) Equipment(state, rules, order, timeline);
            }
            state.Winner = DetermineWinner(state);
            BattleVisibility.Observe(state, rules);
            timeline.Final = state.Copy(); return timeline;
        }

        private static bool Spend(MatchState state, GameRules rules, BattleOrder order)
        {
            var unit = state.Units.Find(u => u.Id == order.UnitId);
            if (order.Action != BattleAction.Revive && !unit.Alive) return false;
            int cost = BattleCommands.Cost(state, rules, order);
            if (state.AP[unit.Owner] < cost) return false;
            state.AP[unit.Owner] -= cost;
            if ((int)order.Action < 4) unit.ReadyOn[(int)order.Action] = state.Turn + Math.Max(1, BattleCommands.Spec(state, rules, order).Cooldown);
            return true;
        }
        private static void FinishStep(MatchState state, GameRules rules, ResolutionStep step, BattleTimeline timeline)
        {
            BattleVisibility.Observe(state, rules); step.After = state.Copy(); timeline.Steps.Add(step);
        }
        private static void Move(MatchState state, GameRules rules, BattleOrder[] orders, BattleTimeline timeline)
        {
            if (orders.Length == 0) return;
            var started = new HashSet<int>();
            int maximum = orders.Max(o => o.Path.Count);
            for (int index = 0; index < maximum; index++)
            {
                var step = new ResolutionStep { Before = state.Copy() };
                foreach (var order in orders.Where(o => index < o.Path.Count))
                {
                    var unit = state.Units.Find(u => u.Id == order.UnitId);
                    if (!unit.Alive || unit.Rooted(state.Turn)) continue;
                    if (index > 0 && !started.Contains(unit.Id)) continue;
                    if (index == 0)
                    {
                        if (!Spend(state, rules, order)) continue;
                        started.Add(unit.Id);
                        if (order.Action == BattleAction.Skill) unit.HiddenThrough = state.Turn + 1;
                    }
                    if (!BattleCommands.Movable(state, rules, order.Path[index])) { started.Remove(unit.Id); continue; }
                    var from = unit.Cell; unit.Cell = order.Path[index];
                    step.Cues.Add(new BattleCue { Kind = CueKind.Move, ActorId = unit.Id, Owner = unit.Owner, Source = from, Target = unit.Cell,
                        HiddenMovement = unit.HiddenThrough > 0 && unit.HiddenThrough >= state.Turn, Label = order.Action == BattleAction.Skill ? "SHADOW STEP" : "MOVE" });
                    Collect(state, unit); FireDamage(state, rules, unit);
                    if (index == order.Path.Count - 1 && unit.Alive) Teleport(state, rules, unit, step);
                }
                FinishStep(state, rules, step, timeline);
            }
        }
        private static void Collect(MatchState state, BattleUnit unit)
        {
            foreach (var item in state.Items.Where(i => i.Cell == unit.Cell && !i.Collected[unit.Owner]))
            { item.Collected[unit.Owner] = true; state.Materials[unit.Owner]++; }
        }
        private static void FireDamage(MatchState state, GameRules rules, BattleUnit unit)
        {
            foreach (var fire in state.Fires.Where(f => f.Owner != unit.Owner && unit.Cell.Distance(f.Cell) <= Math.Max(0, f.Area - 1)))
            {
                if (!unit.Alive || !state.FireHits.Add($"{fire.Id}/{unit.Id}")) continue;
                DamageUnit(state, rules, unit, fire.Damage, fire.Owner);
            }
        }
        private static void SettleFire(MatchState state, GameRules rules, BattleTimeline timeline)
        {
            if (state.Fires.Count == 0) return;
            var step = new ResolutionStep { Before = state.Copy() };
            foreach (var unit in state.Units.Where(u => u.Alive).ToArray())
            {
                int health = unit.Health;
                FireDamage(state, rules, unit);
                if (health != unit.Health) step.Cues.Add(new BattleCue { Kind = CueKind.Skill, Owner = 1 - unit.Owner, Source = unit.Cell,
                    Target = unit.Cell, Area = 1, Damage = health - unit.Health, Label = "BURN" });
            }
            if (step.Cues.Count > 0) FinishStep(state, rules, step, timeline);
        }
        private static void Teleport(MatchState state, GameRules rules, BattleUnit unit, ResolutionStep step)
        {
            if (unit.Rooted(state.Turn)) return;
            var portal = state.Structures.FirstOrDefault(s => s.Kind == StructureKind.PortalEntrance && s.Health > 0 && s.ReadyTurn <= state.Turn && s.Cell == unit.Cell);
            if (portal == null || !BattleCommands.Movable(state, rules, portal.Destination)) return;
            var from = unit.Cell; unit.Cell = portal.Destination;
            step.Cues.Add(new BattleCue { Kind = CueKind.Portal, ActorId = unit.Id, Owner = unit.Owner, Source = from, Target = unit.Cell,
                HiddenMovement = unit.HiddenThrough > 0 && unit.HiddenThrough >= state.Turn, Label = "PORTAL" });
            Collect(state, unit); FireDamage(state, rules, unit);
        }
        private static void Attack(MatchState state, GameRules rules, BattleOrder[] orders, BattleTimeline timeline)
        {
            if (orders.Length == 0) return;
            var step = new ResolutionStep { Before = state.Copy() };
            var unitDamage = new Dictionary<int, int>();
            var roots = new HashSet<int>();
            var baseDamage = new int[2];
            var structureDamage = new Dictionary<int, int>();
            foreach (var order in orders)
            {
                var actor = state.Units.Find(u => u.Id == order.UnitId);
                if (!actor.Alive) continue;
                var spec = BattleCommands.Spec(state, rules, order);
                if (!Spend(state, rules, order)) continue;
                // Local profile uses fixed tile targets. A miss is still an executed attack.
                if (actor.Cell.Distance(order.Target) > spec.Range) continue;
                var area = new HashSet<GridCell>(BattleVisibility.Area(rules, order.Target, Math.Max(0, spec.Area - 1)));
                state.Revealed[actor.Owner].UnionWith(area);
                bool root = order.Action == BattleAction.Skill && rules.Characters[actor.Slot].SkillEffect == SkillEffect.RootDamage;
                foreach (var target in state.Units.Where(u => u.Owner != actor.Owner && u.Alive && area.Contains(u.Cell)))
                {
                    unitDamage[target.Id] = unitDamage.TryGetValue(target.Id, out int damage) ? damage + spec.Damage : spec.Damage;
                    if (root) roots.Add(target.Id);
                }
                var home = state.Bases[1 - actor.Owner];
                if (home.Health > 0 && area.Contains(home.Cell)) baseDamage[home.Owner] += spec.Damage;
                foreach (var structure in state.Structures.Where(s => s.Owner != actor.Owner && s.Health > 0 && area.Contains(s.Cell)))
                    structureDamage[structure.Id] = structureDamage.TryGetValue(structure.Id, out int damage) ? damage + spec.Damage : spec.Damage;
                if (order.Action == BattleAction.Skill && rules.Characters[actor.Slot].SkillEffect == SkillEffect.BurningGround)
                {
                    var fire = new FireZone { Id = state.NextId++, Owner = actor.Owner, Cell = order.Target, Area = spec.Area, Damage = spec.Damage, ThroughTurn = state.Turn + 2 };
                    state.Fires.Add(fire);
                    foreach (var target in state.Units.Where(u => u.Owner != actor.Owner && area.Contains(u.Cell))) state.FireHits.Add($"{fire.Id}/{target.Id}");
                }
                step.Cues.Add(new BattleCue { Kind = order.Action == BattleAction.Attack ? CueKind.Attack : CueKind.Skill,
                    ActorId = actor.Id, Owner = actor.Owner, Source = actor.Cell, Target = order.Target, Area = spec.Area, Damage = spec.Damage, Label = spec.Name });
            }
            foreach (var hit in unitDamage.OrderBy(h => h.Key))
            {
                var target = state.Units.Find(u => u.Id == hit.Key);
                DamageUnit(state, rules, target, hit.Value, 1 - target.Owner);
                if (target.Alive && roots.Contains(target.Id)) { target.RootFrom = state.Turn + 1; target.RootThrough = state.Turn + 1; }
            }
            for (int player = 0; player < 2; player++) state.Bases[player].Health = Math.Max(0, state.Bases[player].Health - baseDamage[player]);
            foreach (var hit in structureDamage) state.Structures.Find(s => s.Id == hit.Key).Health = Math.Max(0, state.Structures.Find(s => s.Id == hit.Key).Health - hit.Value);
            foreach (var structure in state.Structures.Where(s => s.Health <= 0 && s.Link > 0).ToArray())
                foreach (var linked in state.Structures.Where(s => s.Link == structure.Link)) linked.Health = 0;
            FinishStep(state, rules, step, timeline);
        }
        private static void DamageUnit(MatchState state, GameRules rules, BattleUnit target, int damage, int owner)
        {
            if (!target.Alive || damage <= 0) return;
            target.Health = Math.Max(0, target.Health - damage);
            if (target.Health > 0) return;
            target.DiedTurn = state.Turn; target.Equipment = -1; target.HiddenThrough = 0; target.RootFrom = target.RootThrough = 0;
            state.AP[owner] = Math.Min(rules.MaximumAP, state.AP[owner] + rules.KillReward);
        }
        private static void Revive(MatchState state, GameRules rules, BattleOrder order, BattleTimeline timeline)
        {
            var unit = state.Units.Find(u => u.Id == order.UnitId);
            if (unit.Alive || !state.Units.Any(u => u.Owner == unit.Owner && u.Alive) || !BattleCommands.Movable(state, rules, order.Target)) return;
            var step = new ResolutionStep { Before = state.Copy() };
            if (!Spend(state, rules, order)) return;
            unit.Health = rules.Characters[unit.Slot].Health; unit.Cell = order.Target; unit.Placed = true;
            unit.ReadyOn = new int[4]; unit.HiddenThrough = unit.RootFrom = unit.RootThrough = 0;
            step.Cues.Add(new BattleCue { Kind = CueKind.Revive, ActorId = unit.Id, Owner = unit.Owner, Source = unit.Cell, Target = unit.Cell, Label = "REVIVE" });
            FinishStep(state, rules, step, timeline);
        }
        private static void Equipment(MatchState state, GameRules rules, BattleOrder order, BattleTimeline timeline)
        {
            var actor = state.Units.Find(u => u.Id == order.UnitId);
            if (!actor.Alive || actor.Equipment < 0) return;
            var equipment = rules.Equipment[actor.Equipment];
            var ally = state.Units.Find(u => u.Id == order.TargetUnit && u.Owner == actor.Owner && u.Alive);
            if (equipment.Effect == EquipmentEffect.Swap && (ally == null || actor.Rooted(state.Turn) || ally.Rooted(state.Turn))) return;
            if (equipment.Effect != EquipmentEffect.Swap && !BattleCommands.Movable(state, rules, order.Target)) return;
            var step = new ResolutionStep { Before = state.Copy() };
            if (!Spend(state, rules, order)) return;
            if (equipment.Effect == EquipmentEffect.Swap)
            {
                var from = actor.Cell; actor.Cell = ally.Cell; ally.Cell = from;
                Collect(state, actor); Collect(state, ally); FireDamage(state, rules, actor); FireDamage(state, rules, ally);
            }
            else if (equipment.Effect == EquipmentEffect.Beacon)
                state.Structures.Add(new BattleStructure { Id = state.NextId++, Owner = actor.Owner, Health = equipment.Health,
                    Kind = StructureKind.Beacon, Cell = order.Target, Vision = equipment.Action.Area });
            else
            {
                int link = state.NextId++;
                state.Structures.Add(new BattleStructure { Id = state.NextId++, Link = link, Owner = actor.Owner, Health = equipment.Health,
                    Kind = StructureKind.PortalEntrance, Cell = order.Target, Destination = order.Exit, ReadyTurn = state.Turn + equipment.Duration - 1 });
                state.Structures.Add(new BattleStructure { Id = state.NextId++, Link = link, Owner = actor.Owner, Health = equipment.Health,
                    Kind = StructureKind.PortalExit, Cell = order.Exit, Destination = order.Target, ReadyTurn = state.Turn + equipment.Duration - 1 });
            }
            step.Cues.Add(new BattleCue { Kind = CueKind.Structure, ActorId = actor.Id, Owner = actor.Owner, Source = step.Before.Units.Find(u => u.Id == actor.Id).Cell,
                Target = equipment.Effect == EquipmentEffect.Swap ? actor.Cell : order.Target, Label = equipment.Name, Area = 1 });
            FinishStep(state, rules, step, timeline);
        }
        public static BattleWinner DetermineWinner(MatchState state)
        {
            bool oneUnits = state.Units.Any(u => u.Owner == 0 && u.Alive), twoUnits = state.Units.Any(u => u.Owner == 1 && u.Alive);
            int oneBase = state.Bases[0].Health, twoBase = state.Bases[1].Health;
            if (oneBase > 0 && twoBase > 0)
            {
                if (oneUnits && twoUnits) return BattleWinner.None;
                if (oneUnits) return BattleWinner.PlayerOne;
                if (twoUnits) return BattleWinner.PlayerTwo;
                return oneBase == twoBase ? BattleWinner.Draw : oneBase > twoBase ? BattleWinner.PlayerOne : BattleWinner.PlayerTwo;
            }
            // ponytail: Q-02's unresolved combined eliminations keep the documented local-profile draw.
            if (oneBase <= 0 && twoBase <= 0 || !oneUnits && !twoUnits) return BattleWinner.Draw;
            return oneBase > 0 ? BattleWinner.PlayerOne : BattleWinner.PlayerTwo;
        }
    }
}
