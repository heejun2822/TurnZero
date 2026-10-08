using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace TurnZero.Tests
{
    public sealed class BattleTests
    {
        private static readonly ActionOrder[] NoOrders = Array.Empty<ActionOrder>();

        private static EntityState Entity(int id, int owner, EntityKind kind, int x, int y, int hp = 3) =>
            new EntityState { Id = id, Owner = owner, Kind = kind, Position = new Cell(x, y), Health = hp, MaximumHealth = hp };

        private static BattleState Combat(Cell? one = null, Cell? two = null)
        {
            var state = new BattleState { Turn = 1, Phase = MatchPhase.Planning, AP = new[] { 8, 8 } };
            state.Entities.Add(Entity(101, 0, EntityKind.Base, 1, 1, 8));
            state.Entities.Add(Entity(102, 1, EntityKind.Base, 6, 8, 8));
            var a = one ?? new Cell(3, 4);
            var b = two ?? new Cell(3, 5);
            state.Entities.Add(Entity(1, 0, EntityKind.Unit, a.X, a.Y));
            state.Entities.Add(Entity(5, 1, EntityKind.Unit, b.X, b.Y));
            return state;
        }

        private static BattleMatch Start(BattleRules rules = null, Cell? one = null, Cell? two = null)
        {
            var match = new BattleMatch(rules ?? new BattleRules());
            Assert.That(match.PlaceBase(0, one ?? new Cell(2, 2), 10, out _), Is.True);
            Assert.That(match.PlaceBase(1, two ?? new Cell(5, 7), 10, out _), Is.True);
            return match;
        }

        private static void SameState(BattleState actual, BattleState expected)
        {
            Assert.That(actual.Turn, Is.EqualTo(expected.Turn));
            Assert.That(actual.Phase, Is.EqualTo(expected.Phase));
            Assert.That(actual.Outcome, Is.EqualTo(expected.Outcome));
            CollectionAssert.AreEqual(expected.AP, actual.AP);
            CollectionAssert.AreEqual(expected.Entities.OrderBy(e => e.Id).Select(e => $"{e.Id}/{e.Owner}/{e.Kind}/{e.Position}/{e.Health}/{e.MaximumHealth}"),
                actual.Entities.OrderBy(e => e.Id).Select(e => $"{e.Id}/{e.Owner}/{e.Kind}/{e.Position}/{e.Health}/{e.MaximumHealth}"));
        }

        [Test]
        public void PlacementRejectsEdgesAndWrongPlayerAndSpawnsFourUnitsEach()
        {
            var rules = new BattleRules();
            var match = new BattleMatch(rules);
            Assert.That(match.PlaceBase(1, new Cell(3, 7), 0, out _), Is.False);
            Assert.That(match.PlaceBase(0, new Cell(0, 0), 0, out _), Is.False);
            Assert.That(match.PlaceBase(0, new Cell(3, 5), 0, out _), Is.False);
            Assert.That(match.PlaceBase(0, new Cell(2, 2), 0, out _), Is.True);
            Assert.That(match.PlaceBase(0, new Cell(3, 2), 0, out _), Is.False);
            Assert.That(match.PlaceBase(1, new Cell(5, 7), 0, out _), Is.True);
            for (int p = 0; p < 2; p++)
            {
                var own = match.View(p, 0).Entities.Where(e => e.Owner == p).ToList();
                Assert.That(own.Count(e => e.Kind == EntityKind.Unit), Is.EqualTo(4));
                Assert.That(own.Select(e => e.Position).Distinct().Count(), Is.EqualTo(5));
                Assert.That(own.All(e => rules.Contains(e.Position)), Is.True);
            }
            Assert.That(match.Phase, Is.EqualTo(MatchPhase.Planning));
        }

        [Test]
        public void ReservationsReplaceCancelAndRejectOverspendingWithoutChangingExistingOrder()
        {
            var match = Start(new BattleRules { InitialAP = 1 });
            Assert.That(match.QueueOrder(0, 1, ActionOrder.Move(1, new Cell(2, 4)), out _), Is.True);
            Assert.That(match.QueueOrder(0, 1, ActionOrder.Move(1, new Cell(1, 3)), out _), Is.True);
            Assert.That(match.View(0, 10).ReservedAP, Is.EqualTo(1));
            Assert.That(match.QueueOrder(0, 1, ActionOrder.Move(2, new Cell(4, 2)), out _), Is.False);
            Assert.That(match.View(0, 10).Orders.Count, Is.EqualTo(1));
            Assert.That(match.QueueOrder(0, 1, ActionOrder.Wait(1), out _), Is.True);
            Assert.That(match.View(0, 10).ReservedAP, Is.Zero);
            Assert.That(match.QueueOrder(0, 1, ActionOrder.Move(2, new Cell(4, 2)), out _), Is.True);
        }

        [Test]
        public void LocksStaleTurnsAndDeadlinesRejectInvalidSubmissions()
        {
            var match = Start();
            Assert.That(match.QueueOrder(0, 0, ActionOrder.Wait(1), out _), Is.False);
            Assert.That(match.QueueOrder(0, 1, ActionOrder.Wait(5), out _), Is.False);
            Assert.That(match.LockOrders(0, 1), Is.True);
            Assert.That(match.QueueOrder(0, 1, ActionOrder.Move(1, new Cell(2, 4)), out _), Is.False);
            Assert.That(match.Tick(39.999), Is.False);
            Assert.That(match.Tick(40), Is.True);
            Assert.That(match.Turn, Is.EqualTo(2));
            Assert.That(match.Tick(40), Is.False);
            Assert.That(match.View(0, 40).Locked, Is.False);
            Assert.That(match.QueueOrder(0, 1, ActionOrder.Wait(1), out _), Is.False);
        }

        [Test]
        public void WaitDoesNotSpendAPAndIncomeIsCapped()
        {
            var match = Start();
            match.ResolveNow(11);
            Assert.That(match.View(0, 11).AP, Is.EqualTo(8));
            match.ResolveNow(12);
            Assert.That(match.View(0, 12).AP, Is.EqualTo(8));
            Assert.That(match.View(0, 12).ReservedAP, Is.Zero);
        }

        [Test]
        public void ContestedMovesBothFailAndSpendAP()
        {
            var before = Combat(new Cell(3, 4), new Cell(4, 5));
            var result = BattleResolver.Resolve(before, new BattleRules(), new[] { ActionOrder.Move(1, new Cell(4, 4)) }, new[] { ActionOrder.Move(5, new Cell(4, 4)) });
            Assert.That(result.State.Entities.Find(e => e.Id == 1).Position, Is.EqualTo(new Cell(3, 4)));
            Assert.That(result.State.Entities.Find(e => e.Id == 5).Position, Is.EqualTo(new Cell(4, 5)));
            CollectionAssert.AreEqual(new[] { 7, 7 }, result.State.AP);
            Assert.That(before.AP[0], Is.EqualTo(8), "The input state must remain unchanged.");
        }

        [Test]
        public void SwapsAndFollowingIntoInitiallyOccupiedTilesFail()
        {
            var before = Combat();
            var swap = BattleResolver.Resolve(before, new BattleRules(), new[] { ActionOrder.Move(1, new Cell(3, 5)) }, new[] { ActionOrder.Move(5, new Cell(3, 4)) });
            Assert.That(swap.State.Entities.Find(e => e.Id == 1).Position, Is.EqualTo(new Cell(3, 4)));
            Assert.That(swap.State.Entities.Find(e => e.Id == 5).Position, Is.EqualTo(new Cell(3, 5)));
            var follow = BattleResolver.Resolve(before, new BattleRules(), new[] { ActionOrder.Move(1, new Cell(3, 5)) }, new[] { ActionOrder.Move(5, new Cell(3, 6)) });
            Assert.That(follow.State.Entities.Find(e => e.Id == 1).Position, Is.EqualTo(new Cell(3, 4)));
            Assert.That(follow.State.Entities.Find(e => e.Id == 5).Position, Is.EqualTo(new Cell(3, 6)));
        }

        [Test]
        public void BasesBlockMovement()
        {
            var before = Combat(new Cell(1, 2));
            var result = BattleResolver.Resolve(before, new BattleRules(), new[] { ActionOrder.Move(1, new Cell(1, 1)) }, NoOrders);
            Assert.That(result.State.Entities.Find(e => e.Id == 1).Position, Is.EqualTo(new Cell(1, 2)));
            Assert.That(result.State.AP[0], Is.EqualTo(7));
        }

        [Test]
        public void AttackRechecksRangeAfterMovementAndDoesNotRefund()
        {
            var before = Combat(new Cell(3, 4), new Cell(3, 6));
            var result = BattleResolver.Resolve(before, new BattleRules(), new[] { ActionOrder.Attack(1, 5) }, new[] { ActionOrder.Move(5, new Cell(3, 7)) });
            Assert.That(result.State.Entities.Find(e => e.Id == 5).Health, Is.EqualTo(3));
            Assert.That(result.State.AP[0], Is.EqualTo(6));
            Assert.That(result.Messages[0].Any(m => m.Contains("attack failed")), Is.True);
            Assert.That(result.Messages[0].Any(m => m.Contains("(4,8)")), Is.False, "Hidden destination must not appear in public movement results.");
        }

        [Test]
        public void AttackTracksAnEntityThatMovesWithinVisibleRange()
        {
            var result = BattleResolver.Resolve(Combat(), new BattleRules(), new[] { ActionOrder.Attack(1, 5) }, new[] { ActionOrder.Move(5, new Cell(4, 5)) });
            Assert.That(result.State.Entities.Find(e => e.Id == 5).Health, Is.EqualTo(2));
        }

        [Test]
        public void MutualEliminationIsADrawAndBothAttacksLand()
        {
            var state = Combat();
            state.Entities.Find(e => e.Id == 1).Health = 1;
            state.Entities.Find(e => e.Id == 5).Health = 1;
            var result = BattleResolver.Resolve(state, new BattleRules(), new[] { ActionOrder.Attack(1, 5) }, new[] { ActionOrder.Attack(5, 1) });
            Assert.That(result.State.Entities.Where(e => e.Kind == EntityKind.Unit).All(e => e.Health == 0), Is.True);
            Assert.That(result.State.Outcome, Is.EqualTo(BattleOutcome.Draw));
            Assert.That(result.State.Phase, Is.EqualTo(MatchPhase.Finished));
        }

        [Test]
        public void SimultaneousBaseDestructionIsADrawEvenWithSurvivingUnits()
        {
            var state = Combat(new Cell(6, 7), new Cell(1, 2));
            state.Entities.Where(e => e.Kind == EntityKind.Base).ToList().ForEach(e => e.Health = 1);
            var result = BattleResolver.Resolve(state, new BattleRules(), new[] { ActionOrder.Attack(1, 102) }, new[] { ActionOrder.Attack(5, 101) });
            Assert.That(result.State.Outcome, Is.EqualTo(BattleOutcome.Draw));
            Assert.That(result.State.Entities.Where(e => e.Kind == EntityKind.Unit).All(e => e.Health > 0), Is.True);
        }

        [Test]
        public void EitherBaseDestructionOrUnitEliminationWins()
        {
            var eliminated = Combat();
            eliminated.Entities.Find(e => e.Id == 5).Health = 1;
            Assert.That(BattleResolver.Resolve(eliminated, new BattleRules(), new[] { ActionOrder.Attack(1, 5) }, NoOrders).State.Outcome, Is.EqualTo(BattleOutcome.PlayerOne));
            var destroyed = Combat(new Cell(6, 7));
            destroyed.Entities.Find(e => e.Id == 102).Health = 1;
            Assert.That(BattleResolver.Resolve(destroyed, new BattleRules(), new[] { ActionOrder.Attack(1, 102) }, NoOrders).State.Outcome, Is.EqualTo(BattleOutcome.PlayerOne));
        }

        [Test]
        public void OrderAndEntityIterationOrderDoNotChangeResolution()
        {
            var state = Combat();
            state.Entities.Add(Entity(2, 0, EntityKind.Unit, 4, 4));
            var one = new[] { ActionOrder.Attack(1, 5), ActionOrder.Attack(2, 5) };
            var expected = BattleResolver.Resolve(state, new BattleRules(), one, new[] { ActionOrder.Attack(5, 1) });
            state.Entities.Reverse();
            var actual = BattleResolver.Resolve(state, new BattleRules(), one.Reverse().ToArray(), new[] { ActionOrder.Attack(5, 1) });
            SameState(actual.State, expected.State);
            CollectionAssert.AreEqual(expected.Messages[0], actual.Messages[0]);
        }

        [Test]
        public void InvalidDuplicateForeignAndOverBudgetOrdersAreRejected()
        {
            var state = Combat();
            var rules = new BattleRules();
            Assert.Throws<ArgumentException>(() => BattleResolver.Resolve(state, rules, new[] { ActionOrder.Wait(5) }, NoOrders));
            Assert.Throws<ArgumentException>(() => BattleResolver.Resolve(state, rules, new[] { ActionOrder.Wait(1), ActionOrder.Wait(1) }, NoOrders));
            Assert.Throws<ArgumentException>(() => BattleResolver.Resolve(state, rules, new[] { new ActionOrder { UnitId = 1, Kind = (ActionKind)99 } }, NoOrders));
            state.AP[0] = 0;
            Assert.Throws<ArgumentException>(() => BattleResolver.Resolve(state, rules, new[] { ActionOrder.Attack(1, 5) }, NoOrders));
        }

        [Test]
        public void HiddenTargetsAndNonexistentTargetsReturnTheSameError()
        {
            var state = Combat(new Cell(1, 2), new Cell(6, 7));
            var rules = new BattleRules();
            Assert.That(BattleResolver.ValidateOrder(state, rules, 0, ActionOrder.Attack(1, 5)),
                Is.EqualTo(BattleResolver.ValidateOrder(state, rules, 0, ActionOrder.Attack(1, 999))));
        }

        [Test]
        public void VisionIsAManhattanDiamondAndRemovedWhenSourcesDie()
        {
            var state = Combat(new Cell(3, 4));
            var rules = new BattleRules();
            var vision = BattleResolver.Vision(state, 0, rules);
            Assert.That(vision.Contains(new Cell(3, 6)), Is.True);
            Assert.That(vision.Contains(new Cell(5, 6)), Is.False);
            state.Entities.Where(e => e.Owner == 0).ToList().ForEach(e => e.Health = 0);
            Assert.That(BattleResolver.Vision(state, 0, rules), Is.Empty);
        }

        [Test]
        public void PublicViewsAndRecordingsAreDetachedFromAuthorityState()
        {
            var match = Start();
            Assert.That(match.View(0, 10).Entities.All(e => e.Owner == 0), Is.True);
            var view = match.View(0, 10);
            view.Entities[0].Health = 999;
            view.VisibleCells.Clear();
            Assert.That(match.View(0, 10).Entities.Any(e => e.Health == 999), Is.False);
            match.ResolveNow(11);
            var exported = match.ExportRecording();
            exported.Turns[0].After.AP[0] = -99;
            Assert.That(match.ExportRecording().Turns[0].After.AP[0], Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void HiddenEnemyKeepsItsLastObservedPositionAndHealth()
        {
            var match = Start(new BattleRules { Height = 12 }, new Cell(1, 1), new Cell(6, 10));
            // Scout away from the other vision sources so last-seen cells can actually become fogged.
            var path = new List<Cell>();
            for (int y = 3; y <= 8; y++) path.Add(new Cell(1, y));
            for (int x = 2; x <= 4; x++) path.Add(new Cell(x, 8));
            path.Add(new Cell(4, 9));
            foreach (var cell in path)
            {
                Assert.That(match.QueueOrder(0, match.Turn, ActionOrder.Move(1, cell), out _), Is.True);
                match.ResolveNow(10 + match.Turn);
            }
            Assert.That(match.View(0, 30).Entities.Any(e => e.Id == 7), Is.True);
            Assert.That(match.QueueOrder(0, match.Turn, ActionOrder.Move(1, new Cell(4, 8)), out _), Is.True);
            Assert.That(match.QueueOrder(1, match.Turn, ActionOrder.Move(7, new Cell(7, 9)), out _), Is.True);
            match.ResolveNow(30);
            var hidden = match.View(0, 30);
            Assert.That(hidden.Entities.Any(e => e.Id == 7), Is.False);
            var memory = hidden.LastSeen.Single(o => o.Entity.Id == 7);
            Assert.That(memory.Entity.Position, Is.EqualTo(new Cell(6, 9)));
            Assert.That(memory.Entity.Health, Is.EqualTo(3));
            Assert.That(hidden.Messages.Any(m => m.Contains("(8,10)")), Is.False);
            Assert.That(match.QueueOrder(0, match.Turn, ActionOrder.Move(1, new Cell(4, 9)), out _), Is.True);
            match.ResolveNow(31);
            Assert.That(match.View(0, 31).LastSeen.Any(o => o.Entity.Id == 7), Is.False, "Revisiting the now-empty last-seen tile must clear its stale marker.");
            Assert.That(match.QueueOrder(0, match.Turn, ActionOrder.Move(1, new Cell(5, 9)), out _), Is.True);
            match.ResolveNow(32);
            Assert.That(match.View(0, 32).Entities.Single(e => e.Id == 7).Position, Is.EqualTo(new Cell(7, 9)), "Re-observation must expose the latest location.");
        }

        [Test]
        public void FinishedMatchRejectsActionsAndDoesNotRecoverAP()
        {
            var match = Start(one: new Cell(2, 3), two: new Cell(2, 6));
            for (int hit = 0; hit < 8; hit++)
            {
                Assert.That(match.QueueOrder(0, match.Turn, ActionOrder.Attack(1, 102), out _), Is.True);
                match.ResolveNow(11 + hit);
            }
            Assert.That(match.Phase, Is.EqualTo(MatchPhase.Finished));
            Assert.That(match.View(0, 20).AP, Is.EqualTo(6));
            Assert.That(match.Tick(1000), Is.False);
            Assert.That(match.ResolveNow(1000), Is.False);
            Assert.That(match.QueueOrder(0, match.Turn, ActionOrder.Wait(1), out _), Is.False);
        }

        [Test]
        public void EveryRecordedTurnReplaysToTheSameState()
        {
            var match = Start();
            match.QueueOrder(0, 1, ActionOrder.Move(1, new Cell(2, 4)), out _);
            match.ResolveNow(11);
            match.ResolveNow(12);
            var recording = match.ExportRecording();
            foreach (var turn in recording.Turns)
                SameState(BattleResolver.Resolve(turn.Before, recording.Rules, turn.PlayerOneOrders, turn.PlayerTwoOrders).State, turn.After);
        }

        [Test]
        public void InvalidRulesFailBeforeCreatingAMatch()
        {
            Assert.Throws<ArgumentException>(() => new BattleMatch(new BattleRules { Height = 9 }));
            Assert.Throws<ArgumentException>(() => new BattleMatch(new BattleRules { PlanningSeconds = float.NaN }));
            Assert.Throws<ArgumentException>(() => new BattleMatch(new BattleRules { InitialAP = 10 }));
        }
    }
}
