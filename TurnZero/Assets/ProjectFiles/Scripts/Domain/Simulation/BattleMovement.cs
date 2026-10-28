using System.Collections.Generic;
using System.Linq;

namespace TurnZero
{
    public static class BattleMovement
    {
        public static void Resolve(MatchState state, GameRules rules, BattleOrder[] orders, BattleTimeline timeline, int phase)
        {
            if (orders.Length == 0)
            {
                return;
            }

            HashSet<int> started = new HashSet<int>();
            int maximum = orders.Max(o => o.Path.Count);

            for (int index = 0; index < maximum; index++)
            {
                List<BattleOrder> moving = new List<BattleOrder>();

                foreach (BattleOrder order in orders.Where(o => index < o.Path.Count))
                {
                    BattleUnit unit = state.Units.Find(u => u.Id == order.UnitId);

                    if (!unit.Alive ||
                        unit.Health <= 0 ||
                        unit.Rooted(state.Turn) ||
                        !BattleCommands.Movable(state, rules, order.Path[index]))
                    {
                        started.Remove(unit.Id);
                        continue;
                    }

                    if (index == 0)
                    {
                        if (!BattleCosts.TryStart(state, rules, order))
                        {
                            continue;
                        }

                        started.Add(unit.Id);

                        if (order.Action == BattleAction.Skill)
                        {
                            BattleStatusEffects.Apply(unit, GameDefinitions.Stealth, state.Turn, unit.Id);
                        }
                    }

                    if (started.Contains(unit.Id))
                    {
                        moving.Add(order);
                    }
                }

                if (moving.Count == 0)
                {
                    continue;
                }

                ResolutionStep step = new ResolutionStep
                {
                    Before = state.Copy(),
                    Phase = phase,
                    SimultaneousMovement = true
                };
                List<BattleUnit> contacts = new List<BattleUnit>();

                foreach (BattleOrder order in moving)
                {
                    BattleUnit unit = state.Units.Find(u => u.Id == order.UnitId);

                    GridCell source = unit.Cell;

                    unit.Cell = order.Path[index];
                    contacts.Add(unit.Copy());

                    step.Cues.Add(new BattleCue
                    {
                        Kind = CueKind.Move,
                        ActorId = unit.Id,
                        Owner = unit.Owner,
                        Source = source,
                        Target = unit.Cell,
                        Label = order.Action == BattleAction.Skill ? "STEALTH MOVE" : "MOVE"
                    });
                }

                (BattleUnit Unit, BattleStructure Portal)[] portals = moving.Where(o => index == o.Path.Count - 1)
                    .Select(o => state.Units.Find(u => u.Id == o.UnitId))
                    .Select(u => (Unit: u, Portal: state.Structures.FirstOrDefault(s => s.Kind == StructureKind.PortalEntrance && s.Operational && s.Completed && s.Cell == u.Cell)))
                    .Where(p => p.Portal != null && !p.Unit.Rooted(state.Turn) && BattleCommands.Movable(state, rules, p.Portal.Destination))
                    .ToArray();

                if (portals.Length > 0)
                {
                    timeline.Record(state, rules, step);
                    step = new ResolutionStep
                    {
                        Before = state.Copy(),
                        Phase = phase,
                        SimultaneousMovement = true
                    };

                    foreach ((BattleUnit Unit, BattleStructure Portal) pair in portals)
                    {
                        GridCell source = pair.Unit.Cell;

                        pair.Unit.Cell = pair.Portal.Destination;
                        contacts.Add(pair.Unit.Copy());

                        step.Cues.Add(new BattleCue
                        {
                            Kind = CueKind.Portal,
                            ActorId = pair.Unit.Id,
                            Owner = pair.Unit.Owner,
                            Source = source,
                            Target = pair.Unit.Cell,
                            Label = "PORTAL"
                        });
                    }
                }

                BattleItems.Collect(state, contacts);
                timeline.Record(state, rules, step);
            }
        }
    }
}
