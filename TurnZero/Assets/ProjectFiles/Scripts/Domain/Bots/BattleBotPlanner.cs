using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace TurnZero
{
    public sealed class BattleBotPlanner
    {
        public BattleBotPlan Plan(BattleBotObservation observation, int seed)
        {
            BattleBotPlan plan = new BattleBotPlan
            {
                Version = GameDefinitions.BotVersion,
                RulesSignature = Signature(observation.Rules),
                Seed = seed,
                Observation = observation
            };

            if (observation.Phase != BattlePhase.Planning || observation.Ready)
            {
                return plan;
            }

            List<BattleBotCandidate> candidates = new BattleBotCandidates(observation).Create();
            Dictionary<int, BattleBotTarget> targets = new Dictionary<int, BattleBotTarget>();
            Dictionary<int, int> health = new Dictionary<int, int>();
            HashSet<int> roots = new HashSet<int>();
            HashSet<int> assigned = new HashSet<int>();
            HashSet<GridCell> explored = new HashSet<GridCell>();
            Random random = new Random(unchecked(seed * 397 ^ observation.Player * 7919 ^ observation.Turn * 104729));

            foreach (BattleBotTarget target in observation.Targets)
            {
                targets.Add(target.Id, target);
                health.Add(target.Id, target.Health);
            }

            foreach (BattleOrder order in observation.Orders)
            {
                assigned.Add(order.UnitId);
            }

            while (assigned.Count < observation.OwnUnits.Count)
            {
                BattleBotCandidate best = null;
                BattleBotScore bestScore = default;
                int ties = 0;

                foreach (BattleBotCandidate candidate in candidates)
                {
                    if (assigned.Contains(candidate.Order.UnitId) || candidate.Cost > observation.AP - plan.ReservedAP)
                    {
                        continue;
                    }

                    BattleBotScore score = BattleBotScoring.Evaluate(candidate, targets, health, roots, explored);
                    int comparison = score.Total.CompareTo(bestScore.Total);

                    if (comparison == 0 &&
                        best != null &&
                        candidate.ReviveHealth > 0 &&
                        best.ReviveHealth > 0 &&
                        candidate.Order.UnitId == best.Order.UnitId)
                    {
                        comparison = best.PositionDistance.CompareTo(candidate.PositionDistance);

                        if (comparison == 0)
                        {
                            comparison = candidate.Exploration.Count.CompareTo(best.Exploration.Count);
                        }
                    }

                    if (score.Total <= 0 || comparison < 0)
                    {
                        continue;
                    }

                    if (comparison > 0)
                    {
                        ties = 0;
                    }

                    ties++;

                    if (comparison > 0 || random.Next(ties) == 0)
                    {
                        best = candidate;
                        bestScore = score;
                    }
                }

                if (best == null)
                {
                    break;
                }

                plan.Decisions.Add(new BattleBotDecision { Order = best.Order.Copy(), Score = bestScore });
                plan.ReservedAP += best.Cost;
                assigned.Add(best.Order.UnitId);
                explored.UnionWith(best.Exploration);

                foreach (int id in best.Targets)
                {
                    health[id] = Math.Max(0, health[id] - best.Damage);

                    if (best.Root && health[id] > 0)
                    {
                        roots.Add(id);
                    }
                }
            }

            foreach (BattleUnit unit in observation.OwnUnits)
            {
                if (!assigned.Contains(unit.Id))
                {
                    plan.Decisions.Add(new BattleBotDecision
                    {
                        Order = new BattleOrder { UnitId = unit.Id, Action = BattleAction.Wait },
                        Result = "No positive affordable action."
                    });
                }
            }

            return plan;
        }

        private static string Signature(GameRules rules)
        {
            StringBuilder text = new StringBuilder();
            text.Append($"{rules.Width}/{rules.Height}/{rules.HomeHeight}/{rules.NeutralHeight}/{rules.BaseHealth}/{rules.BaseVision}/{rules.UnitVision}/{rules.TurnIncome}/{rules.MaximumAP}/{rules.KillReward}/{rules.ReviveCost}/{rules.MinimumReviveCost}/{rules.InitialMaterials}/{rules.Seed}/");
            text.Append(rules.TurnStartSeconds.ToString("R", CultureInfo.InvariantCulture));
            text.Append('/');
            text.Append(rules.PlanningSeconds.ToString("R", CultureInfo.InvariantCulture));

            foreach (CharacterSpec character in rules.Characters)
            {
                text.Append($"/{character.Id}/{character.Health}/{(int)character.SkillEffect}/{character.SkillDuration}");

                foreach (ActionSpec action in new[] { character.Move, character.Attack, character.Skill })
                {
                    text.Append($"/{action.Range}/{action.Area}/{action.Damage}/{action.Cost}/{action.Priority}/{action.Cooldown}");
                }
            }

            foreach (EquipmentSpec equipment in rules.Equipment)
            {
                ActionSpec action = equipment.Action;
                text.Append($"/{equipment.Id}/{(int)equipment.Effect}/{equipment.Health}/{equipment.Duration}/{equipment.ExitRange}/{action.Range}/{action.Area}/{action.Damage}/{action.Cost}/{action.Priority}/{action.Cooldown}");
            }

            uint hash = 2166136261;

            foreach (char character in text.ToString())
            {
                hash = unchecked((hash ^ character) * 16777619);
            }

            return hash.ToString("x8", CultureInfo.InvariantCulture);
        }
    }
}
