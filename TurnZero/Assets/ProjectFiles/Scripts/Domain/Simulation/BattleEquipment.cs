using System.Collections.Generic;
using System.Linq;
namespace TurnZero
{
    public static class BattleEquipment
    {
        public static void Resolve(MatchState state, GameRules rules, BattleOrder[] orders, BattleTimeline timeline, int phase)
        {
            var swaps = orders.Where(o => {
                var actor = state.Units.Find(u => u.Id == o.UnitId);
                var ally = state.Units.Find(u => u.Id == o.TargetUnit);
                return actor.Alive && actor.Health > 0 && actor.Equipment >= 0 && rules.Equipment[actor.Equipment].Effect == EquipmentEffect.Swap &&
                    ally != null && ally.Owner == actor.Owner && ally.Id != actor.Id && ally.Alive && ally.Health > 0 &&
                    !actor.Rooted(state.Turn) && !ally.Rooted(state.Turn);
            }).ToArray();
            var participants = swaps.SelectMany(o => new[] { o.UnitId,o.TargetUnit }).GroupBy(id => id).ToDictionary(g => g.Key,g => g.Count());
            var validSwaps = swaps.Where(o => participants[o.UnitId] == 1 && participants[o.TargetUnit] == 1).ToArray();
            if (validSwaps.Length > 0)
            {
                var step = new ResolutionStep { Before = state.Copy(), Phase = phase };
                var arrivals = new List<BattleUnit>();
                foreach (var order in validSwaps)
                {
                    var actor = state.Units.Find(u => u.Id == order.UnitId); var ally = state.Units.Find(u => u.Id == order.TargetUnit);
                    if (!BattleCosts.TryStart(state,rules,order)) continue;
                    var from = actor.Cell; actor.Cell = ally.Cell; ally.Cell = from;
                    var cue = new BattleCue { Kind = CueKind.Swap, ActorId = actor.Id, Owner = actor.Owner, Source = from, Target = actor.Cell, Label = "SWAP", APDelta = -rules.Equipment[actor.Equipment].Action.Cost };
                    cue.Positions[actor.Id] = actor.Cell; cue.Positions[ally.Id] = ally.Cell; step.Cues.Add(cue);
                    arrivals.Add(actor); arrivals.Add(ally);
                }
                BattleItems.Collect(state,arrivals);
                timeline.Record(state,rules,step);
            }
            var installs = orders.Where(o => {
                var actor = state.Units.Find(u => u.Id == o.UnitId);
                if (!actor.Alive || actor.Health <= 0 || actor.Equipment < 0) return false;
                var equipment = rules.Equipment[actor.Equipment];
                return equipment.Effect != EquipmentEffect.Swap && BattleCommands.Installable(state,rules,o.Target) &&
                    (equipment.Effect != EquipmentEffect.Portal || o.Exit != o.Target && BattleCommands.Installable(state,rules,o.Exit));
            }).ToArray();
            var claims = installs.SelectMany(o => ClaimedCells(state,rules,o)).GroupBy(c => c).ToDictionary(g => g.Key,g => g.Count());
            installs = installs.Where(o => ClaimedCells(state,rules,o).All(c => claims[c] == 1)).ToArray();
            if (installs.Length == 0) return;
            var installStep = new ResolutionStep { Before = state.Copy(), Phase = phase };
            foreach (var order in installs)
            {
                var actor = state.Units.Find(u => u.Id == order.UnitId);
                if (!BattleCosts.TryStart(state,rules,order)) continue;
                var equipment = rules.Equipment[actor.Equipment];
                if (equipment.Effect == EquipmentEffect.Beacon)
                    state.Structures.Add(new BattleStructure { Id = state.NextId++, Owner = actor.Owner, Health = equipment.Health, Active = false,
                        Kind = StructureKind.Beacon, Cell = order.Target, Vision = equipment.Action.Area });
                else
                {
                    int link = state.NextId++;
                    state.Structures.Add(new BattleStructure { Id = state.NextId++, Link = link, Owner = actor.Owner, Health = equipment.Health, Active = false,
                        Kind = StructureKind.PortalEntrance, Cell = order.Target, Destination = order.Exit, ReadyTurn = state.Turn+equipment.Duration-1 });
                    state.Structures.Add(new BattleStructure { Id = state.NextId++, Link = link, Owner = actor.Owner, Health = equipment.Health, Active = false,
                        Kind = StructureKind.PortalExit, Cell = order.Exit, Destination = order.Target, ReadyTurn = state.Turn+equipment.Duration-1 });
                }
                installStep.Cues.Add(new BattleCue { Kind = CueKind.Structure, ActorId = actor.Id, Owner = actor.Owner, Source = actor.Cell, Target = order.Target, Label = equipment.Name, APDelta = -equipment.Action.Cost });
            }
            timeline.Record(state,rules,installStep);
        }
        private static IEnumerable<GridCell> ClaimedCells(MatchState state, GameRules rules, BattleOrder order)
        {
            yield return order.Target;
            var unit = state.Units.Find(u => u.Id == order.UnitId);
            if (rules.Equipment[unit.Equipment].Effect == EquipmentEffect.Portal) yield return order.Exit;
        }
    }
}
