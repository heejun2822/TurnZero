using System;
using System.Collections.Generic;

namespace TurnZero
{
    public sealed class BattleBotCandidates
    {
        private readonly BattleBotObservation observation;
        private readonly GameRules rules;
        private readonly HashSet<GridCell> blocked = new HashSet<GridCell>();
        private readonly HashSet<GridCell> visible;
        private readonly HashSet<GridCell> explored;

        public BattleBotCandidates(BattleBotObservation observation)
        {
            this.observation = observation;
            rules = observation.Rules;
            visible = new HashSet<GridCell>(observation.Visible);
            explored = new HashSet<GridCell>(observation.Explored);

            if (observation.OwnBase.Placed)
            {
                blocked.Add(observation.OwnBase.Cell);
            }

            if (observation.EnemyBase != null && observation.EnemyBase.Placed)
            {
                blocked.Add(observation.EnemyBase.Cell);
            }
        }

        public List<BattleBotCandidate> Create()
        {
            List<BattleBotCandidate> result = new List<BattleBotCandidate>();
            bool livingAlly = observation.OwnUnits.Exists(unit => unit.Alive);

            foreach (BattleUnit unit in observation.OwnUnits)
            {
                if (observation.Orders.Exists(order => order.UnitId == unit.Id))
                {
                    continue;
                }

                Dictionary<GridCell, int> goals = GoalDistances(unit);

                if (!unit.Alive)
                {
                    if (livingAlly && unit.DiedTurn < observation.Turn)
                    {
                        AddRevival(result, unit, goals);
                    }

                    continue;
                }

                AddAction(result, unit, BattleAction.Move, goals);
                AddAction(result, unit, BattleAction.Attack, goals);
                AddAction(result, unit, BattleAction.Skill, goals);
            }

            result.Sort(Compare);
            return result;
        }

        private void AddAction(List<BattleBotCandidate> result, BattleUnit unit, BattleAction action, Dictionary<GridCell, int> goals)
        {
            if (unit.ReadyOn[(int)action] > observation.Turn)
            {
                return;
            }

            ActionSpec spec = BattleCommands.Spec(rules, unit, action);
            bool stealth = action == BattleAction.Skill && rules.Characters[unit.Slot].SkillEffect == SkillEffect.StealthMove;

            if (spec.Cost > observation.AP)
            {
                return;
            }

            if (action == BattleAction.Move || stealth)
            {
                if (unit.Rooted(observation.Turn))
                {
                    return;
                }

                Dictionary<GridCell, List<GridCell>> paths = BattlePathfinding.Paths(rules, unit.Cell, spec.Range, blocked);

                foreach (KeyValuePair<GridCell, List<GridCell>> entry in paths)
                {
                    BattleBotCandidate candidate = Candidate(unit, action, entry.Key);
                    candidate.Order.Path = entry.Value;
                    candidate.Stealth = stealth;
                    candidate.DistanceGain = goals.TryGetValue(unit.Cell, out int before) && goals.TryGetValue(entry.Key, out int after)
                        ? before - after
                        : 0;
                    candidate.Exploration = NewVision(entry.Value);
                    result.Add(candidate);
                }

                return;
            }

            foreach (GridCell center in BattleVisibility.Area(rules, unit.Cell, spec.Range))
            {
                BattleBotCandidate candidate = Candidate(unit, action, center);
                candidate.Damage = spec.Damage;
                candidate.Root = action == BattleAction.Skill && rules.Characters[unit.Slot].SkillEffect == SkillEffect.RootDamage;

                foreach (BattleBotTarget target in observation.Targets)
                {
                    if (center.Distance(target.Cell) <= spec.Area)
                    {
                        candidate.Targets.Add(target.Id);
                    }
                }

                if (candidate.Targets.Count > 0)
                {
                    result.Add(candidate);
                }
            }
        }

        private void AddRevival(List<BattleBotCandidate> result, BattleUnit unit, Dictionary<GridCell, int> goals)
        {
            int cost = BattleCommands.Cost(rules, unit, BattleAction.Revive, observation.Turn);

            if (cost > observation.AP)
            {
                return;
            }

            foreach (GridCell cell in BattleVisibility.Area(rules, observation.OwnBase.Cell, rules.BaseVision))
            {
                if (!rules.Home(observation.Player, cell) || blocked.Contains(cell))
                {
                    continue;
                }

                BattleBotCandidate candidate = Candidate(unit, BattleAction.Revive, cell);
                candidate.ReviveHealth = rules.Characters[unit.Slot].Health;
                candidate.PositionDistance = goals.TryGetValue(cell, out int distance) ? distance : int.MaxValue;
                candidate.Exploration = NewVision(new[] { cell });
                result.Add(candidate);
            }
        }

        private BattleBotCandidate Candidate(BattleUnit unit, BattleAction action, GridCell target)
        {
            return new BattleBotCandidate
            {
                Order = new BattleOrder { UnitId = unit.Id, Action = action, Target = target },
                Cost = BattleCommands.Cost(rules, unit, action, observation.Turn)
            };
        }

        private Dictionary<GridCell, int> GoalDistances(BattleUnit unit)
        {
            HashSet<GridCell> goals = new HashSet<GridCell>();
            CharacterSpec character = rules.Characters[unit.Slot];
            int range = character.SkillEffect == SkillEffect.StealthMove
                ? character.Attack.Range
                : Math.Max(character.Attack.Range, character.Skill.Range);

            foreach (BattleBotTarget target in observation.Targets)
            {
                goals.UnionWith(BattleVisibility.Area(rules, target.Cell, range));
            }

            if (goals.Count == 0 && observation.EnemyBase != null && observation.EnemyBase.Health > 0)
            {
                // Reacquire a remembered base before scoring attacks against it.
                goals.UnionWith(BattleVisibility.Area(rules, observation.EnemyBase.Cell, rules.UnitVision));
            }

            if (goals.Count == 0)
            {
                foreach (UnitObservation memory in observation.Memory)
                {
                    if (memory.HasPosition && memory.Unit.Health > 0 && !visible.Contains(memory.Unit.Cell))
                    {
                        goals.UnionWith(BattleVisibility.Area(rules, memory.Unit.Cell, rules.UnitVision));
                    }
                }
            }

            if (goals.Count == 0)
            {
                for (int y = 0; y < rules.Height; y++)
                {
                    for (int x = 0; x < rules.Width; x++)
                    {
                        GridCell cell = new GridCell(x, y);

                        if (rules.Home(1 - observation.Player, cell) && !explored.Contains(cell))
                        {
                            goals.Add(cell);
                        }
                    }
                }
            }

            goals.ExceptWith(blocked);
            return BattlePathfinding.Distances(rules, goals, blocked);
        }

        private List<GridCell> NewVision(IEnumerable<GridCell> path)
        {
            HashSet<GridCell> revealed = new HashSet<GridCell>();

            foreach (GridCell position in path)
            {
                foreach (GridCell cell in BattleVisibility.Area(rules, position, rules.UnitVision))
                {
                    if (!explored.Contains(cell))
                    {
                        revealed.Add(cell);
                    }
                }
            }

            List<GridCell> result = new List<GridCell>(revealed);
            result.Sort(BattleBotObservation.CompareCells);
            return result;
        }

        private static int Compare(BattleBotCandidate left, BattleBotCandidate right)
        {
            int unit = left.Order.UnitId.CompareTo(right.Order.UnitId);
            int action = left.Order.Action.CompareTo(right.Order.Action);
            return unit != 0 ? unit : action != 0 ? action : BattleBotObservation.CompareCells(left.Order.Target, right.Order.Target);
        }
    }
}
