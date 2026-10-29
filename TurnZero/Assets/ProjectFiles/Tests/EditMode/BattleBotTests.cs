using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace TurnZero.Tests
{
    public sealed class BattleBotTests
    {
        private static BattleBotObservation Arena(out MatchState state)
        {
            GameRules rules = LocalBattleTests.Rules();
            state = LocalBattleTests.Arena(rules);
            state.AP[0] = 25;
            BattleVisibility.Observe(state, rules);
            return BattleBotObservation.FromView(BattleVisibility.View(state, rules, 0, 2), rules);
        }

        private static string Decisions(BattleBotPlan plan)
        {
            return string.Join("|", plan.Decisions.Select(decision =>
                $"{decision.Order.UnitId}/{decision.Order.Action}/{decision.Order.Target}/{string.Join(";", decision.Order.Path)}/{decision.Score}"));
        }

        [Test]
        public void AutomaticDeploymentOnlyPlacesTheRequestedTeamAndPreservesManualPositions()
        {
            LocalBattleMatch match = new LocalBattleMatch(LocalBattleTests.Rules());
            match.BeginPlacement();
            Assert.That(match.Place(0, new GridCell(3, 3), 0, out _), Is.True);

            for (int i = 0; i < 5; i++)
            {
                Assert.That(match.Place(0, new GridCell(3, 4), 0, out _), Is.True);
            }

            Assert.That(match.AutoDeploy(1, 0, out string error), Is.True, error);
            MatchState state = match.DebugSnapshot();
            Assert.That(state.Units.Where(unit => unit.Owner == 0).All(unit => unit.Cell == new GridCell(3, 4)), Is.True);
            Assert.That(state.Bases[0].Cell, Is.EqualTo(new GridCell(3, 3)));
            Assert.That(state.Units.All(unit => unit.Placed), Is.True);
            Assert.That(match.Phase, Is.EqualTo(BattlePhase.TurnStart));
        }

        [Test]
        public void InvalidAutomaticPresetFailsBeforeMutatingPlacement()
        {
            GameRules rules = LocalBattleTests.Rules();
            rules.BaseVision = 1;
            LocalBattleMatch match = new LocalBattleMatch(rules);
            Assert.That(match.AutoDeploy(0, 0, out string error), Is.False);
            Assert.That(error, Is.Not.Null);
            Assert.That(match.Phase, Is.EqualTo(BattlePhase.Setup));
            Assert.That(match.DebugSnapshot().Units.Any(unit => unit.Placed), Is.False);
        }

        [Test]
        public void ObservationCopiesPublicDataAndDoesNotCopyEnemyCooldownsOrOrders()
        {
            BattleBotObservation observation = Arena(out MatchState state);
            int health = observation.OwnUnits[0].Health;
            state.Units[0].Health = 1;
            state.Units[0].ReadyOn[0] = 100;
            Assert.That(observation.OwnUnits[0].Health, Is.EqualTo(health));
            Assert.That(observation.OwnUnits[0].ReadyOn[0], Is.EqualTo(0));
            Assert.That(observation.Orders.Count, Is.Zero);
            Assert.That(observation.Targets.Count, Is.EqualTo(5));
            Assert.That(observation.Memory.All(memory => memory.Unit.ReadyOn.All(turn => turn == 0)), Is.True);
        }

        [Test]
        public void HiddenEnemyStateAndHumanOrdersDoNotChangeThePlan()
        {
            GameRules rules = LocalBattleTests.Rules();
            LocalBattleMatch match = new LocalBattleMatch(rules);
            match.AutoDeploy(0);
            match.Tick(2);
            MatchState state = match.DebugSnapshot();
            BattleBotObservation first = BattleBotObservation.FromView(BattleVisibility.View(state, rules, 0, 2), rules);

            foreach (BattleUnit enemy in state.Units.Where(unit => unit.Owner == 1))
            {
                enemy.Cell = new GridCell(0, 19);
                enemy.Health = 1;
                enemy.ReadyOn[0] = 100;
                state.Orders.Add(new BattleOrder { UnitId = enemy.Id, Action = BattleAction.Attack, Target = new GridCell(0, 0) });
            }

            state.Bases[1].Cell = new GridCell(11, 19);
            state.Bases[1].Health = 1;
            BattleBotObservation second = BattleBotObservation.FromView(BattleVisibility.View(state, rules, 0, 2), rules);
            BattleBotPlanner planner = new BattleBotPlanner();
            Assert.That(Decisions(planner.Plan(first, 19)), Is.EqualTo(Decisions(planner.Plan(second, 19))));
        }

        [Test]
        public void PublicDeathAndHiddenRevivalRetainTheSameBotMemoryRules()
        {
            BattleBotObservation initial = Arena(out MatchState state);
            GameRules rules = initial.Rules;
            BattleUnit enemy = state.Units.Single(unit => unit.Id == 6);
            enemy.Cell = new GridCell(0, 19);
            enemy.Health = 0;
            enemy.DiedTurn = state.Turn;
            BattleVisibility.Observe(state, rules);
            BattleBotObservation dead = BattleBotObservation.FromView(BattleVisibility.View(state, rules, 0, 2), rules);
            Assert.That(dead.Memory.Single(memory => memory.Unit.Id == 6).Unit.Health, Is.Zero);

            enemy.Health = 14;
            BattleVisibility.Observe(state, rules);
            BattleBotObservation revived = BattleBotObservation.FromView(BattleVisibility.View(state, rules, 0, 2), rules);
            Assert.That(revived.Memory.Single(memory => memory.Unit.Id == 6).Unit.Health, Is.Zero);
            Assert.That(revived.Targets.Exists(target => target.Id == 6), Is.False);
        }

        [Test]
        public void FiveCharacterActionCandidatesUseTheirOwnRangesAndSkillKinds()
        {
            BattleBotObservation observation = Arena(out MatchState state);
            List<BattleBotCandidate> candidates = new BattleBotCandidates(observation).Create();

            foreach (BattleUnit unit in observation.OwnUnits)
            {
                Assert.That(candidates.Exists(candidate => candidate.Order.UnitId == unit.Id && candidate.Order.Action == BattleAction.Move), Is.True);
                Assert.That(candidates.Exists(candidate => candidate.Order.UnitId == unit.Id && candidate.Order.Action == BattleAction.Attack), Is.True);
                Assert.That(candidates.Exists(candidate => candidate.Order.UnitId == unit.Id && candidate.Order.Action == BattleAction.Skill), Is.True);
            }

            foreach (BattleBotCandidate candidate in candidates)
            {
                Assert.That(BattleCommands.Validate(state, observation.Rules, 0, candidate.Order), Is.Null);
            }
        }

        [Test]
        public void ScoringCapsDamageAndRemovesRepeatedDefeatAndRootBonuses()
        {
            Dictionary<int, BattleBotTarget> targets = new Dictionary<int, BattleBotTarget>
            {
                [6] = new BattleBotTarget { Id = 6, Health = 5, Character = true }
            };
            Dictionary<int, int> health = new Dictionary<int, int> { [6] = 5 };
            HashSet<int> roots = new HashSet<int>();
            HashSet<GridCell> explored = new HashSet<GridCell>();
            BattleBotCandidate basic = new BattleBotCandidate { Damage = 3, Cost = 2, Targets = new List<int> { 6 } };
            BattleBotCandidate skill = new BattleBotCandidate { Damage = 6, Cost = 4, Targets = new List<int> { 6 } };
            Assert.That(BattleBotScoring.Evaluate(basic, targets, health, roots, explored).Total, Is.EqualTo(10));
            Assert.That(BattleBotScoring.Evaluate(skill, targets, health, roots, explored).Total, Is.EqualTo(28));
            health[6] = 0;
            Assert.That(BattleBotScoring.Evaluate(skill, targets, health, roots, explored).Total, Is.EqualTo(-4));
            health[6] = 5;
            basic.Root = true;
            Assert.That(BattleBotScoring.Evaluate(basic, targets, health, roots, explored).Root, Is.EqualTo(2));
            roots.Add(6);
            Assert.That(BattleBotScoring.Evaluate(basic, targets, health, roots, explored).Root, Is.Zero);
        }

        [Test]
        public void BaseFinishingRevivalAndExplorationUseDocumentedWeights()
        {
            Dictionary<int, BattleBotTarget> targets = new Dictionary<int, BattleBotTarget>
            {
                [102] = new BattleBotTarget { Id = 102, Health = 3, Base = true }
            };
            Dictionary<int, int> health = new Dictionary<int, int> { [102] = 3 };
            HashSet<int> roots = new HashSet<int>();
            HashSet<GridCell> explored = new HashSet<GridCell>();
            BattleBotCandidate attack = new BattleBotCandidate { Damage = 3, Cost = 2, Targets = new List<int> { 102 } };
            Assert.That(BattleBotScoring.Evaluate(attack, targets, health, roots, explored).Total, Is.EqualTo(56));
            BattleBotCandidate revival = new BattleBotCandidate { ReviveHealth = 14, Cost = 15 };
            Assert.That(BattleBotScoring.Evaluate(revival, targets, health, roots, explored).Total, Is.EqualTo(61));
            BattleBotCandidate move = new BattleBotCandidate
            {
                DistanceGain = 3,
                Cost = 2,
                Exploration = new List<GridCell> { new GridCell(1, 1), new GridCell(2, 1) }
            };
            Assert.That(BattleBotScoring.Evaluate(move, targets, health, roots, explored).Total, Is.EqualTo(9));
            explored.Add(new GridCell(1, 1));
            Assert.That(BattleBotScoring.Evaluate(move, targets, health, roots, explored).Total, Is.EqualTo(8));
        }

        [Test]
        public void TeamPlanningNeverOverspendsOrReservesTwoActionsForOneCharacter()
        {
            BattleBotObservation observation = Arena(out _);

            foreach (int ap in new[] { 0, 2, 10, 25 })
            {
                observation.AP = ap;
                BattleBotPlan plan = new BattleBotPlanner().Plan(observation, 4);
                Assert.That(plan.ReservedAP, Is.LessThanOrEqualTo(ap));
                Assert.That(plan.Decisions.Select(decision => decision.Order.UnitId).Distinct().Count(), Is.EqualTo(5));
                Assert.That(plan.Decisions.Any(decision => decision.Order.Action == BattleAction.Equipment), Is.False);
            }
        }

        [Test]
        public void RootCooldownAndSameTurnDeathFallBackToFreeWaiting()
        {
            BattleBotObservation observation = Arena(out _);

            foreach (BattleUnit unit in observation.OwnUnits)
            {
                unit.ReadyOn = new[] { 99, 99, 99, 99 };
            }

            observation.OwnUnits[0].Health = 0;
            observation.OwnUnits[0].DiedTurn = observation.Turn;
            BattleBotPlan plan = new BattleBotPlanner().Plan(observation, 1);
            Assert.That(plan.Decisions.All(decision => decision.Order.Action == BattleAction.Wait), Is.True);
            Assert.That(plan.ReservedAP, Is.Zero);

            BattleUnit rooted = observation.OwnUnits[1];
            rooted.ReadyOn[0] = 0;
            rooted.ReadyOn[2] = 0;
            BattleStatusEffects.Apply(rooted, GameDefinitions.Root, observation.Turn - 1, 6);
            Assert.That(new BattleBotCandidates(observation).Create().Any(candidate => candidate.Order.UnitId == rooted.Id), Is.False);
        }

        [Test]
        public void RevivalUsesDiscountedCostAndConsumesTheCharactersOnlyAction()
        {
            BattleBotObservation observation = Arena(out _);
            observation.Turn = 8;
            observation.OwnUnits[0].Health = 0;
            observation.OwnUnits[0].DiedTurn = 1;
            BattleBotPlan plan = new BattleBotPlanner().Plan(observation, 7);
            BattleBotDecision revival = plan.Decisions.Single(decision => decision.Order.UnitId == 1);
            Assert.That(revival.Order.Action, Is.EqualTo(BattleAction.Revive));
            Assert.That(revival.Score.Cost, Is.EqualTo(10));
            Assert.That(revival.Order.Target.Distance(observation.OwnBase.Cell), Is.LessThanOrEqualTo(observation.Rules.BaseVision));
            Assert.That(revival.Order.Target, Is.Not.EqualTo(observation.OwnBase.Cell));
        }

        [Test]
        public void SameObservationSeedAndPermutedCollectionsProduceTheSamePlan()
        {
            BattleBotObservation observation = Arena(out _);
            BattleBotPlanner planner = new BattleBotPlanner();
            BattleBotPlan first = planner.Plan(observation, 99);
            observation.OwnUnits.Reverse();
            observation.Targets.Reverse();
            observation.Visible.Reverse();
            observation.Explored.Reverse();
            BattleBotPlan second = planner.Plan(observation, 99);
            Assert.That(Decisions(first), Is.EqualTo(Decisions(second)));
            Assert.That(first.RulesSignature, Is.EqualTo(second.RulesSignature));
        }

        [Test]
        public void AHiddenBaseIsOnlyASearchGoalAndNeverAnAttackCandidate()
        {
            BattleBotObservation observation = Arena(out _);
            observation.Targets.Clear();
            observation.EnemyBase = new BattleBase { Owner = 1, Placed = true, Cell = new GridCell(11, 19), Health = 30 };
            observation.EnemyBaseVisible = false;
            BattleBotPlan plan = new BattleBotPlanner().Plan(observation, 2);
            Assert.That(plan.Decisions.All(decision => decision.Order.Action == BattleAction.Move || decision.Order.Action == BattleAction.Wait || decision.Order.Action == BattleAction.Skill && decision.Order.UnitId == 2), Is.True);
        }

        [Test]
        public void ControllerPlansOnceAndKeepsItsPlanAfterHumanEdits()
        {
            LocalBattleMatch match = new LocalBattleMatch(LocalBattleTests.Rules());
            match.AutoDeploy(0);
            match.Tick(2);
            BattleBotController controller = new BattleBotController(match, 1);
            Assert.That(controller.Prepare(2), Is.True);
            string first = Decisions(controller.LastPlan);
            Assert.That(controller.LastPlan.Decisions.All(decision => decision.Accepted), Is.True);
            Assert.That(match.Queue(0, match.Turn, new BattleOrder { UnitId = 1, Action = BattleAction.Wait }, out _, 2), Is.True);
            Assert.That(controller.Prepare(2), Is.False);
            Assert.That(controller.PlanningCount, Is.EqualTo(1));
            Assert.That(Decisions(controller.LastPlan), Is.EqualTo(first));
            Assert.That(match.View(1, 2).Ready, Is.True);
            Assert.That(match.Ready(0, 2), Is.True);
            Assert.That(match.ShouldResolve(2), Is.True);
        }

        [Test]
        public void DeadlineAndClosedPhasesNeverStartBotPlanning()
        {
            LocalBattleMatch match = new LocalBattleMatch(LocalBattleTests.Rules());
            BattleBotController controller = new BattleBotController(match, 1);
            Assert.That(controller.Prepare(0), Is.False);
            match.AutoDeploy(0);
            match.Tick(2);
            Assert.That(controller.Prepare(match.Deadline), Is.False);
            Assert.That(controller.PlanningCount, Is.Zero);
            Assert.That(match.View(1, 2).Ready, Is.False);
        }

        [Test]
        public void MovementUsesTheSamePathsAsTheExistingCore()
        {
            BattleBotObservation observation = Arena(out MatchState state);
            List<BattleBotCandidate> candidates = new BattleBotCandidates(observation).Create();

            foreach (BattleBotCandidate candidate in candidates.Where(item => item.Order.Path.Count > 0))
            {
                BattleUnit unit = state.Units.Single(item => item.Id == candidate.Order.UnitId);
                int range = BattleCommands.Spec(observation.Rules, unit, candidate.Order.Action).Range;
                CollectionAssert.AreEqual(BattlePathfinding.FindPath(state, observation.Rules, unit.Cell, candidate.Order.Target, range, 0), candidate.Order.Path);
            }
        }

        [Test]
        public void PassiveOpponentMatchesFinishWithoutIllegalCommandsAndCanRestart()
        {
            foreach (int player in new[] { 0, 1 })
            {
                GameRules rules = LocalBattleTests.Rules();
                rules.Seed = 2471 + player;
                LocalBattleMatch match = new LocalBattleMatch(rules);
                match.AutoDeploy(0);
                BattleBotController bot = new BattleBotController(match, player);
                double clock = 2;

                for (int turn = 0; turn < 120 && match.Phase != BattlePhase.Finished; turn++)
                {
                    clock = Math.Max(clock, match.Deadline);
                    match.Tick(clock);
                    Assert.That(bot.Prepare(clock), Is.True);
                    Assert.That(bot.LastPlan.Decisions.All(decision => decision.Accepted), Is.True);
                    match.Ready(1 - player, clock);
                    match.Resolve();
                    match.CompleteResolution(clock);
                }

                Assert.That(match.Phase, Is.EqualTo(BattlePhase.Finished), $"Bot P{player + 1} stalled.");
                Assert.That(match.DebugSnapshot().Winner, Is.EqualTo(player == 0 ? BattleWinner.PlayerOne : BattleWinner.PlayerTwo));
                Assert.That(bot.Prepare(clock), Is.False);
                BattleBotController restarted = new BattleBotController(new LocalBattleMatch(rules), player);
                Assert.That(restarted.LastPlan, Is.Null);
                Assert.That(restarted.PlanningCount, Is.Zero);
            }
        }
    }
}
