using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace TurnZero.Tests
{
    public sealed class BattleTests
    {
        private static readonly ActionOrder[] NoOrders = Array.Empty<ActionOrder>();

        private static EntityState Entity(int id, int owner, EntityKind kind, int x, int y, int hp = 3)
        {
            return new EntityState
            {
                Id = id,
                Owner = owner,
                Kind = kind,
                Position = new Cell(x, y),
                Health = hp,
                MaximumHealth = hp
            };
        }

        private static BattleState Combat(Cell? one = null, Cell? two = null)
        {
            BattleState state = new BattleState
            {
                Turn = 1,
                Phase = MatchPhase.Planning,
                AP = new[] { 8, 8 }
            };

            state.Entities.Add(Entity(101, 0, EntityKind.Base, 1, 1, 8));
            state.Entities.Add(Entity(102, 1, EntityKind.Base, 6, 8, 8));
            Cell a = one ?? new Cell(3, 4);
            Cell b = two ?? new Cell(3, 5);

            state.Entities.Add(Entity(1, 0, EntityKind.Unit, a.X, a.Y));
            state.Entities.Add(Entity(5, 1, EntityKind.Unit, b.X, b.Y));

            return state;
        }

        private static BattleMatch Start(BattleRules rules = null, Cell? one = null, Cell? two = null)
        {
            BattleMatch match = new BattleMatch(rules ?? new BattleRules());

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
            CollectionAssert.AreEqual(expected.Entities.OrderBy(e => e.Id)
                .Select(e => $"{e.Id}/{e.Owner}/{e.Kind}/{e.Position}/{e.Health}/{e.MaximumHealth}"),
                actual.Entities.OrderBy(e => e.Id)
                .Select(e => $"{e.Id}/{e.Owner}/{e.Kind}/{e.Position}/{e.Health}/{e.MaximumHealth}"));
        }

        [Test]
        public void FogStartsWithOnlyEachPlayersHomeFieldExplored()
        {
            BattleRules rules = new BattleRules
            {
                Width = 16,
                Height = 24,
                NeutralHeight = 4
            };
            BattleMatch match = new BattleMatch(rules);

            for (int player = 0; player < 2; player++)
            {
                PlayerView view = match.View(player, 0);

                Assert.That(view.VisibleCells, Is.Empty);
                Assert.That(view.ExploredCells.Count, Is.EqualTo(160));

                for (int y = 0; y < 24; y++)
                {
                    for (int x = 0; x < 16; x++)
                    {
                        bool home = player == 0 ? y < 10 : y >= 14;

                        Assert.That(view.Visibility(new Cell(x, y)), Is.EqualTo(home ? TileVisibility.Explored : TileVisibility.Unexplored));
                    }
                }

                view.ExploredCells.Clear();
                Assert.That(match.View(player, 0).ExploredCells.Count, Is.EqualTo(160));
            }
        }

        [Test]
        public void ScoutingNeutralAndEnemyTilesPersistsAfterLeavingVision()
        {
            BattleRules rules = new BattleRules
            {
                Width = 16,
                Height = 24,
                NeutralHeight = 4
            };
            BattleMatch match = Start(rules, new Cell(7, 8), new Cell(14, 22));
            Cell neutral = new Cell(7, 11);

            Assert.That(match.View(0, 10).Visibility(neutral), Is.EqualTo(TileVisibility.Visible));
            Assert.That(match.View(1, 10).Visibility(neutral), Is.EqualTo(TileVisibility.Unexplored));

            for (int y = 10; y <= 14; y++)
            {
                Assert.That(match.QueueOrder(0, match.Turn, ActionOrder.Move(1, new Cell(7, y)), out _), Is.True);
                match.ResolveNow(11 + y);
            }

            Cell enemy = new Cell(7, 16);

            Assert.That(match.View(0, 30).Visibility(enemy), Is.EqualTo(TileVisibility.Visible));
            Assert.That(match.View(0, 30).Visibility(neutral), Is.EqualTo(TileVisibility.Explored));
            match.QueueOrder(0, match.Turn, ActionOrder.Move(1, new Cell(7, 13)), out _);
            match.ResolveNow(31);
            Assert.That(match.View(0, 31).Visibility(enemy), Is.EqualTo(TileVisibility.Explored));
            Assert.That(match.View(0, 31).Visibility(new Cell(0, 23)), Is.EqualTo(TileVisibility.Unexplored));
        }

        [Test]
        public void ActionRevealIsOneCellForItsOwnerAndExpiresWithItsTurn()
        {
            BattleRules rules = new BattleRules();
            BattleState state = Combat(new Cell(1, 2), new Cell(6, 7));
            Cell point = new Cell(3, 6);

            state.RevealCell(0, point);
            Assert.That(BattleResolver.Vision(state, 0, rules).Contains(point), Is.True);
            Assert.That(BattleResolver.Vision(state, 0, rules).Contains(new Cell(3, 7)), Is.False);
            Assert.That(BattleResolver.Vision(state, 1, rules).Contains(point), Is.False);
            BattleState copy = state.Copy();

            copy.VisionReveals.Clear();
            Assert.That(BattleResolver.Vision(state, 0, rules).Contains(point), Is.True);
            state.Turn++;
            Assert.That(BattleResolver.Vision(state, 0, rules).Contains(point), Is.False);
        }

        [Test]
        public void AttackRevealSurvivesItsSourceDeathAndIsRecordedAsExplored()
        {
            BattleRules rules = new BattleRules
            {
                AttackDamage = 3
            };
            BattleState state = Combat(new Cell(3, 4), new Cell(3, 6));

            // Keep another unit alive so the game can advance after the mutual kill.
            state.Entities.Add(Entity(2, 0, EntityKind.Unit, 0, 0));
            state.Entities.Add(Entity(6, 1, EntityKind.Unit, 7, 9));
            TurnResult result = BattleResolver.Resolve(
                state,
                rules,
                new[] { ActionOrder.Attack(1, 5) },
                new[] { ActionOrder.Attack(5, 1) });

            Assert.That(result.State.Phase, Is.EqualTo(MatchPhase.Planning));
            Assert.That(result.State.Entities.Single(e => e.Id == 1).Health, Is.Zero);
            Assert.That(result.State.VisionReveals.Any(r => r.Player == 0 && r.Position == new Cell(3, 6)), Is.True);
            Assert.That(BattleResolver.Vision(result.State, 0, rules).Contains(new Cell(3, 6)), Is.True);
            Assert.That(result.SeenCells[0].Contains(new Cell(3, 6)), Is.True);
            result.State.Turn++;
            Assert.That(BattleResolver.Vision(result.State, 0, rules).Contains(new Cell(3, 6)), Is.False);
        }

        [Test]
        public void ResolutionDisplaysTheSameTurnAndDelaysTheNextPlanningTimer()
        {
            BattleMatch match = Start();

            Assert.That(match.ResolveNow(11, true), Is.True);
            Assert.That(match.Phase, Is.EqualTo(MatchPhase.Resolving));
            Assert.That(match.Turn, Is.EqualTo(1));
            Assert.That(match.QueueOrder(0, 1, ActionOrder.Wait(1), out _), Is.False);
            Assert.That(match.Tick(11.5, true), Is.False);
            Assert.That(match.Tick(11.75, true), Is.True);
            Assert.That(match.Phase, Is.EqualTo(MatchPhase.Planning));
            Assert.That(match.Turn, Is.EqualTo(2));
            Assert.That(match.SecondsLeft(11.75), Is.EqualTo(30));
            TurnRecord record = match.ExportRecording().Turns.Single();

            SameState(BattleResolver.Resolve(record.Before, new BattleRules(), record.PlayerOneOrders, record.PlayerTwoOrders).State, record.After);
        }

        [Test]
        public void AttackPointBecomesExploredWhenResolutionEndsAndItsSourceIsDead()
        {
            BattleRules rules = new BattleRules
            {
                Height = 12,
                NeutralHeight = 2,
                VisionRange = 1,
                AttackRange = 1,
                AttackDamage = 3
            };
            BattleMatch match = Start(rules, new Cell(2, 2), new Cell(5, 9));
            Cell[] enemyPath = new[]
            {
                new Cell(5,7),
                new Cell(5,6),
                new Cell(5,5),
                new Cell(4,5),
                new Cell(3,5)
            };

            for (int i = 0; i < enemyPath.Length; i++)
            {
                if (i < 2)
                {
                    Assert.That(match.QueueOrder(0, match.Turn, ActionOrder.Move(1, new Cell(2, 4 + i)), out _), Is.True);
                }

                Assert.That(match.QueueOrder(1, match.Turn, ActionOrder.Move(7, enemyPath[i]), out _), Is.True);
                match.ResolveNow(11 + i);
            }

            Cell point = new Cell(3, 5);

            Assert.That(match.QueueOrder(0, match.Turn, ActionOrder.Attack(1, 7), out _), Is.True);
            Assert.That(match.QueueOrder(1, match.Turn, ActionOrder.Attack(7, 1), out _), Is.True);
            match.ResolveNow(20, true);
            Assert.That(match.View(0, 20).Entities.Single(e => e.Id == 1).Health, Is.Zero);
            Assert.That(match.View(0, 20).Visibility(point), Is.EqualTo(TileVisibility.Visible));
            match.Tick(20.75);
            Assert.That(match.View(0, 20.75).Visibility(point), Is.EqualTo(TileVisibility.Explored));
            Assert.That(match.View(0, 20.75).VisibleCells.Contains(point), Is.False);
            BattleMatch fresh = new BattleMatch(rules);

            Assert.That(fresh.View(0, 0).Visibility(point), Is.EqualTo(TileVisibility.Unexplored));
        }

        [Test]
        public void NeutralRowsAndHomeBordersRejectBasesAndSpawnsStayInHomeFields()
        {
            BattleRules rules = new BattleRules
            {
                Width = 16,
                Height = 24,
                NeutralHeight = 4
            };

            for (int y = 10; y < 14; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    for (int player = 0; player < 2; player++)
                    {
                        Assert.That(rules.CanPlaceBase(player, new Cell(x, y)), Is.False);
                    }
                }
            }

            Assert.That(rules.CanPlaceBase(0, new Cell(7, 9)), Is.False);
            Assert.That(rules.CanPlaceBase(1, new Cell(7, 14)), Is.False);
            BattleMatch match = Start(rules, new Cell(7, 8), new Cell(8, 15));

            Assert.That(match.View(0, 10).Entities.Where(e => e.Owner == 0).All(e => e.Position.Y < 10), Is.True);
            Assert.That(match.View(1, 10).Entities.Where(e => e.Owner == 1).All(e => e.Position.Y >= 14), Is.True);
            Assert.Throws<ArgumentException>(() => new BattleMatch(new BattleRules
            {
                Height = 24,
                NeutralHeight = 19
            }));
        }

        [Test]
        public void PlacementRejectsEdgesAndWrongPlayerAndSpawnsFourUnitsEach()
        {
            BattleRules rules = new BattleRules();
            BattleMatch match = new BattleMatch(rules);

            Assert.That(match.PlaceBase(1, new Cell(3, 7), 0, out _), Is.False);
            Assert.That(match.PlaceBase(0, new Cell(0, 0), 0, out _), Is.False);
            Assert.That(match.PlaceBase(0, new Cell(3, 5), 0, out _), Is.False);
            Assert.That(match.PlaceBase(0, new Cell(2, 2), 0, out _), Is.True);
            Assert.That(match.PlaceBase(0, new Cell(3, 2), 0, out _), Is.False);
            Assert.That(match.PlaceBase(1, new Cell(5, 7), 0, out _), Is.True);

            for (int p = 0; p < 2; p++)
            {
                List<EntityState> own = match.View(p, 0).Entities.Where(e => e.Owner == p).ToList();

                Assert.That(own.Count(e => e.Kind == EntityKind.Unit), Is.EqualTo(4));
                Assert.That(own.Select(e => e.Position).Distinct().Count(), Is.EqualTo(5));
                Assert.That(own.All(e => rules.Contains(e.Position)), Is.True);
            }

            Assert.That(match.Phase, Is.EqualTo(MatchPhase.Planning));
        }

        [Test]
        public void FirstTurnGrantsIncomeOnceAfterBothBasesArePlaced()
        {
            BattleRules rules = new BattleRules();

            Assert.That(rules.InitialAP, Is.Zero);
            Assert.That(rules.TurnIncome, Is.EqualTo(10));
            Assert.That(rules.MaximumAP, Is.EqualTo(25));
            BattleMatch match = new BattleMatch(rules);

            for (int player = 0; player < 2; player++)
            {
                Assert.That(match.View(player, 0).AP, Is.Zero);
            }

            Assert.That(match.PlaceBase(0, new Cell(2, 2), 0, out _), Is.True);
            Assert.That(match.View(0, 0).AP, Is.Zero);
            Assert.That(match.PlaceBase(1, new Cell(5, 7), 10, out _), Is.True);
            Assert.That(match.PlaceBase(1, new Cell(5, 7), 10, out _), Is.False);
            Assert.That(match.Tick(10), Is.False);
            Assert.That(match.Tick(39.999), Is.False);

            for (int player = 0; player < 2; player++)
            {
                Assert.That(match.View(player, 10).AP, Is.EqualTo(10));
            }
        }

        [Test]
        public void ReservationsReplaceCancelAndRejectOverspendingWithoutChangingExistingOrder()
        {
            BattleMatch match = Start(new BattleRules
            {
                TurnIncome = 1
            });

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
        public void ReservationReplacementUsesCurrentAPAndKeepsThePreviousOrderOnFailure()
        {
            BattleRules rules = new BattleRules
            {
                TurnIncome = 3,
                VisionRange = 4,
                AttackRange = 4
            };
            BattleMatch match = Start(rules, new Cell(2, 3), new Cell(2, 6));

            Assert.That(match.QueueOrder(0, 1, ActionOrder.Attack(1, 7), out _), Is.True);
            Assert.That(match.QueueOrder(0, 1, ActionOrder.Move(2, new Cell(4, 3)), out _), Is.True);
            Assert.That(match.View(0, 10).ReservedAP, Is.EqualTo(3));
            Assert.That(match.QueueOrder(0, 1, ActionOrder.Attack(2, 7), out _), Is.False);
            Assert.That(match.View(0, 10).Orders.Single(o => o.UnitId == 2).Kind, Is.EqualTo(ActionKind.Move));
            Assert.That(match.View(0, 10).ReservedAP, Is.EqualTo(3));
            Assert.That(match.View(0, 10).AP, Is.EqualTo(3));
            Assert.That(match.View(1, 10).ReservedAP, Is.Zero);
            Assert.That(match.QueueOrder(0, 1, ActionOrder.Wait(1), out _), Is.True);
            Assert.That(match.QueueOrder(0, 1, ActionOrder.Attack(2, 7), out _), Is.True);
            Assert.That(match.View(0, 10).ReservedAP, Is.EqualTo(2));
            Assert.That(match.View(0, 10).AP, Is.EqualTo(3));
            match.ResolveNow(11, true);
            Assert.That(match.View(0, 11).AP, Is.EqualTo(1));
            Assert.That(match.View(0, 11).ReservedAP, Is.Zero);
        }

        [Test]
        public void LocksStaleTurnsAndDeadlinesRejectInvalidSubmissions()
        {
            BattleMatch match = Start();

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
            BattleMatch match = Start();

            match.ResolveNow(11);
            Assert.That(match.View(0, 11).AP, Is.EqualTo(20));
            match.ResolveNow(12);
            Assert.That(match.View(0, 12).AP, Is.EqualTo(25));
            match.ResolveNow(13);
            Assert.That(match.View(0, 13).AP, Is.EqualTo(25));
            Assert.That(match.View(0, 12).ReservedAP, Is.Zero);
        }

        [Test]
        public void NextTurnCarriesUnspentAPAndResolutionDoesNotGrantIncome()
        {
            BattleMatch match = Start();

            Assert.That(match.QueueOrder(0, 1, ActionOrder.Move(1, new Cell(2, 4)), out _), Is.True);
            Assert.That(match.View(0, 10).AP, Is.EqualTo(10));
            match.ResolveNow(11, true);
            Assert.That(match.View(0, 11).AP, Is.EqualTo(9));
            Assert.That(match.View(1, 11).AP, Is.EqualTo(10));
            Assert.That(match.Tick(11.5), Is.False);
            Assert.That(match.View(0, 11.5).AP, Is.EqualTo(9));
            Assert.That(match.Tick(11.75), Is.True);
            Assert.That(match.View(0, 11.75).AP, Is.EqualTo(19));
            Assert.That(match.View(1, 11.75).AP, Is.EqualTo(20));
            Assert.That(match.Tick(11.75), Is.False);
            Assert.That(match.View(0, 11.75).AP, Is.EqualTo(19));
        }

        [Test]
        public void ContestedMovesBothFailAndSpendAP()
        {
            BattleState before = Combat(new Cell(3, 4), new Cell(4, 5));
            TurnResult result = BattleResolver.Resolve(
                before,
                new BattleRules(),
                new[] { ActionOrder.Move(1, new Cell(4, 4)) },
                new[] { ActionOrder.Move(5, new Cell(4, 4)) });

            Assert.That(result.State.Entities.Find(e => e.Id == 1).Position, Is.EqualTo(new Cell(3, 4)));
            Assert.That(result.State.Entities.Find(e => e.Id == 5).Position, Is.EqualTo(new Cell(4, 5)));
            CollectionAssert.AreEqual(new[] { 7, 7 }, result.State.AP);
            Assert.That(before.AP[0], Is.EqualTo(8), "The input state must remain unchanged.");
        }

        [Test]
        public void SwapsAndFollowingIntoInitiallyOccupiedTilesFail()
        {
            BattleState before = Combat();
            TurnResult swap = BattleResolver.Resolve(
                before,
                new BattleRules(),
                new[] { ActionOrder.Move(1, new Cell(3, 5)) },
                new[] { ActionOrder.Move(5, new Cell(3, 4)) });

            Assert.That(swap.State.Entities.Find(e => e.Id == 1).Position, Is.EqualTo(new Cell(3, 4)));
            Assert.That(swap.State.Entities.Find(e => e.Id == 5).Position, Is.EqualTo(new Cell(3, 5)));
            TurnResult follow = BattleResolver.Resolve(
                before,
                new BattleRules(),
                new[] { ActionOrder.Move(1, new Cell(3, 5)) },
                new[] { ActionOrder.Move(5, new Cell(3, 6)) });

            Assert.That(follow.State.Entities.Find(e => e.Id == 1).Position, Is.EqualTo(new Cell(3, 4)));
            Assert.That(follow.State.Entities.Find(e => e.Id == 5).Position, Is.EqualTo(new Cell(3, 6)));
        }

        [Test]
        public void BasesBlockMovement()
        {
            BattleState before = Combat(new Cell(1, 2));
            TurnResult result = BattleResolver.Resolve(before, new BattleRules(), new[] { ActionOrder.Move(1, new Cell(1, 1)) }, NoOrders);

            Assert.That(result.State.Entities.Find(e => e.Id == 1).Position, Is.EqualTo(new Cell(1, 2)));
            Assert.That(result.State.AP[0], Is.EqualTo(7));
        }

        [Test]
        public void AttackRechecksRangeAfterMovementAndDoesNotRefund()
        {
            BattleState before = Combat(new Cell(3, 4), new Cell(3, 6));
            TurnResult result = BattleResolver.Resolve(
                before,
                new BattleRules(),
                new[] { ActionOrder.Attack(1, 5) },
                new[] { ActionOrder.Move(5, new Cell(3, 7)) });

            Assert.That(result.State.Entities.Find(e => e.Id == 5).Health, Is.EqualTo(3));
            Assert.That(result.State.AP[0], Is.EqualTo(6));
            Assert.That(result.Messages[0].Any(m => m.Contains("attack failed")), Is.True);
            Assert.That(
                result.Messages[0].Any(m => m.Contains("(4,8)")),
                Is.False,
                "Hidden destination must not appear in public movement results.");
        }

        [Test]
        public void AttackTracksAnEntityThatMovesWithinVisibleRange()
        {
            TurnResult result = BattleResolver.Resolve(
                Combat(),
                new BattleRules(),
                new[] { ActionOrder.Attack(1, 5) },
                new[] { ActionOrder.Move(5, new Cell(4, 5)) });

            Assert.That(result.State.Entities.Find(e => e.Id == 5).Health, Is.EqualTo(2));
        }

        [Test]
        public void MutualEliminationIsADrawAndBothAttacksLand()
        {
            BattleState state = Combat();

            state.Entities.Find(e => e.Id == 1).Health = 1;
            state.Entities.Find(e => e.Id == 5).Health = 1;
            TurnResult result = BattleResolver.Resolve(state, new BattleRules(), new[] { ActionOrder.Attack(1, 5) }, new[] { ActionOrder.Attack(5, 1) });

            Assert.That(result.State.Entities.Where(e => e.Kind == EntityKind.Unit).All(e => e.Health == 0), Is.True);
            Assert.That(result.State.Outcome, Is.EqualTo(BattleOutcome.Draw));
            Assert.That(result.State.Phase, Is.EqualTo(MatchPhase.Finished));
            CollectionAssert.AreEqual(new[] { 6, 6 }, result.State.AP);
        }

        [Test]
        public void MutualEliminationComparesSurvivingBaseHealthForEitherPlayer()
        {
            foreach (int healthierPlayer in new[] { 0, 1 })
            {
                BattleState state = Combat();

                state.Entities.Where(e => e.Kind == EntityKind.Unit).ToList().ForEach(e => e.Health = 1);
                state.Entities.Single(e => e.Kind == EntityKind.Base && e.Owner == healthierPlayer).Health = 7;
                state.Entities.Single(e => e.Kind == EntityKind.Base && e.Owner != healthierPlayer).Health = 4;
                ActionOrder[] one = new[] { ActionOrder.Attack(1, 5) };
                ActionOrder[] two = new[] { ActionOrder.Attack(5, 1) };
                TurnResult result = BattleResolver.Resolve(state, new BattleRules(), one, two);

                Assert.That(result.State.Outcome, Is.EqualTo(healthierPlayer == 0 ? BattleOutcome.PlayerOne : BattleOutcome.PlayerTwo));
                Assert.That(result.State.Phase, Is.EqualTo(MatchPhase.Finished));
                Assert.That(result.State.Entities.Where(e => e.Kind == EntityKind.Unit).All(e => e.Health == 0), Is.True);
                CollectionAssert.AreEqual(new[] { 6, 6 }, result.State.AP);
                state.Entities.Reverse();
                SameState(BattleResolver.Resolve(state, new BattleRules(), one, two).State, result.State);
            }
        }

        [Test]
        public void BaseDestructionTakesPriorityOverOpposingEliminationForEitherPlayer()
        {
            foreach (int losingPlayer in new[] { 0, 1 })
            {
                BattleState state = Combat();

                state.Entities.Single(e => e.Id == 101).Position = new Cell(3, 6);
                state.Entities.Single(e => e.Id == 101).Health = 1;
                state.Entities.Single(e => e.Id == 5).Health = 1;

                if (losingPlayer == 1)
                {
                    foreach (EntityState entity in state.Entities)
                    {
                        entity.Owner = 1 - entity.Owner;
                    }
                }

                ActionOrder[] one = new[] { ActionOrder.Attack(1, 5) };
                ActionOrder[] two = new[] { ActionOrder.Attack(5, 101) };
                TurnResult result = BattleResolver.Resolve(state, new BattleRules(), losingPlayer == 0 ? one : two, losingPlayer == 0 ? two : one);

                Assert.That(result.State.Entities.Single(e => e.Id == 101).Health, Is.Zero);
                Assert.That(result.State.Entities.Single(e => e.Id == 5).Health, Is.Zero);
                Assert.That(result.State.Outcome, Is.EqualTo(losingPlayer == 0 ? BattleOutcome.PlayerTwo : BattleOutcome.PlayerOne));
                Assert.That(result.State.Phase, Is.EqualTo(MatchPhase.Finished));
                state.Entities.Reverse();
                SameState(BattleResolver.Resolve(state, new BattleRules(), losingPlayer == 0 ? one : two, losingPlayer == 0 ? two : one)
                    .State, result.State);
            }
        }

        [Test]
        public void SimultaneousBaseDestructionIsADrawEvenWithSurvivingUnits()
        {
            BattleState state = Combat(new Cell(6, 7), new Cell(1, 2));

            state.Entities.Where(e => e.Kind == EntityKind.Base).ToList().ForEach(e => e.Health = 1);
            TurnResult result = BattleResolver.Resolve(
                state,
                new BattleRules(),
                new[] { ActionOrder.Attack(1, 102) },
                new[] { ActionOrder.Attack(5, 101) });

            Assert.That(result.State.Outcome, Is.EqualTo(BattleOutcome.Draw));
            Assert.That(result.State.Entities.Where(e => e.Kind == EntityKind.Unit).All(e => e.Health > 0), Is.True);
            state.Entities.Add(Entity(2, 0, EntityKind.Unit, 0, 0));
            Assert.That(
                BattleResolver.Resolve(
                    state,
                    new BattleRules(),
                    new[] { ActionOrder.Attack(1, 102) },
                    new[] { ActionOrder.Attack(5, 101) })
                .State.Outcome,
                Is.EqualTo(BattleOutcome.Draw),
                "Different surviving unit counts must not break a simultaneous base destruction draw.");
        }

        [Test]
        public void EitherBaseDestructionOrUnitEliminationWins()
        {
            BattleState eliminated = Combat();

            eliminated.Entities.Find(e => e.Id == 5).Health = 1;
            Assert.That(BattleResolver.Resolve(eliminated, new BattleRules(), new[] { ActionOrder.Attack(1, 5) }, NoOrders)
                .State.Outcome, Is.EqualTo(BattleOutcome.PlayerOne));
            BattleState destroyed = Combat(new Cell(6, 7));

            destroyed.Entities.Find(e => e.Id == 102).Health = 1;
            Assert.That(BattleResolver.Resolve(destroyed, new BattleRules(), new[] { ActionOrder.Attack(1, 102) }, NoOrders)
                .State.Outcome, Is.EqualTo(BattleOutcome.PlayerOne));
        }

        [Test]
        public void OrderAndEntityIterationOrderDoNotChangeResolution()
        {
            BattleState state = Combat();

            state.Entities.Add(Entity(2, 0, EntityKind.Unit, 4, 4));
            ActionOrder[] one = new[] { ActionOrder.Attack(1, 5), ActionOrder.Attack(2, 5) };
            TurnResult expected = BattleResolver.Resolve(state, new BattleRules(), one, new[] { ActionOrder.Attack(5, 1) });

            state.Entities.Reverse();
            TurnResult actual = BattleResolver.Resolve(state, new BattleRules(), one.Reverse().ToArray(), new[] { ActionOrder.Attack(5, 1) });

            SameState(actual.State, expected.State);
            CollectionAssert.AreEqual(expected.Messages[0], actual.Messages[0]);
        }

        [Test]
        public void InvalidDuplicateForeignAndOverBudgetOrdersAreRejected()
        {
            BattleState state = Combat();
            BattleRules rules = new BattleRules();

            Assert.Throws<ArgumentException>(() => BattleResolver.Resolve(state, rules, new[] { ActionOrder.Wait(5) }, NoOrders));
            Assert.Throws<ArgumentException>(() => BattleResolver.Resolve(state, rules, new[] { ActionOrder.Wait(1), ActionOrder.Wait(1) }, NoOrders));
            Assert.Throws<ArgumentException>(() => BattleResolver.Resolve(
                state,
                rules,
                new[]
                {
                    new ActionOrder
                    {
                        UnitId = 1,
                        Kind = (ActionKind)99
                    }
                },
                NoOrders));
            state.AP[0] = 0;
            Assert.Throws<ArgumentException>(() => BattleResolver.Resolve(state, rules, new[] { ActionOrder.Attack(1, 5) }, NoOrders));
        }

        [Test]
        public void HiddenTargetsAndNonexistentTargetsReturnTheSameError()
        {
            BattleState state = Combat(new Cell(1, 2), new Cell(6, 7));
            BattleRules rules = new BattleRules();

            Assert.That(BattleResolver.ValidateOrder(state, rules, 0, ActionOrder.Attack(1, 5)),
                Is.EqualTo(BattleResolver.ValidateOrder(state, rules, 0, ActionOrder.Attack(1, 999))));
        }

        [Test]
        public void VisionIsAManhattanDiamondAndRemovedWhenSourcesDie()
        {
            BattleState state = Combat(new Cell(3, 4));
            BattleRules rules = new BattleRules();
            HashSet<Cell> vision = BattleResolver.Vision(state, 0, rules);

            Assert.That(vision.Contains(new Cell(3, 6)), Is.True);
            Assert.That(vision.Contains(new Cell(5, 6)), Is.False);
            state.Entities.Where(e => e.Owner == 0).ToList().ForEach(e => e.Health = 0);
            Assert.That(BattleResolver.Vision(state, 0, rules), Is.Empty);
        }

        [Test]
        public void PublicViewsAndRecordingsAreDetachedFromAuthorityState()
        {
            BattleMatch match = Start();

            Assert.That(match.View(0, 10).Entities.All(e => e.Owner == 0), Is.True);
            PlayerView view = match.View(0, 10);

            view.Entities[0].Health = 999;
            view.VisibleCells.Clear();
            Assert.That(match.View(0, 10).Entities.Any(e => e.Health == 999), Is.False);
            match.ResolveNow(11);
            BattleRecording exported = match.ExportRecording();

            exported.Turns[0].After.AP[0] = -99;
            Assert.That(match.ExportRecording().Turns[0].After.AP[0], Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void HiddenEnemyKeepsItsLastObservedPositionAndHealth()
        {
            BattleMatch match = Start(
                new BattleRules
                {
                    Height = 12
                },
                new Cell(1, 1),
                new Cell(6, 10));
            // Scout away from the other vision sources so last-seen cells can actually become fogged.
            List<Cell> path = new List<Cell>();

            for (int y = 3; y <= 8; y++)
            {
                path.Add(new Cell(1, y));
            }

            for (int x = 2; x <= 4; x++)
            {
                path.Add(new Cell(x, 8));
            }

            path.Add(new Cell(4, 9));

            foreach (Cell cell in path)
            {
                Assert.That(match.QueueOrder(0, match.Turn, ActionOrder.Move(1, cell), out _), Is.True);
                match.ResolveNow(10 + match.Turn);
            }

            Assert.That(match.View(0, 30).Entities.Any(e => e.Id == 7), Is.True);
            Assert.That(match.QueueOrder(0, match.Turn, ActionOrder.Move(1, new Cell(4, 8)), out _), Is.True);
            Assert.That(match.QueueOrder(1, match.Turn, ActionOrder.Move(7, new Cell(7, 9)), out _), Is.True);
            match.ResolveNow(30);
            PlayerView hidden = match.View(0, 30);

            Assert.That(hidden.Entities.Any(e => e.Id == 7), Is.False);
            Observation memory = hidden.LastSeen.Single(o => o.Entity.Id == 7);

            Assert.That(memory.Entity.Position, Is.EqualTo(new Cell(6, 9)));
            Assert.That(memory.Entity.Health, Is.EqualTo(3));
            Assert.That(hidden.Messages.Any(m => m.Contains("(8,10)")), Is.False);
            Assert.That(match.QueueOrder(0, match.Turn, ActionOrder.Move(1, new Cell(4, 9)), out _), Is.True);
            match.ResolveNow(31);
            Assert.That(
                match.View(0, 31).LastSeen.Any(o => o.Entity.Id == 7),
                Is.False,
                "Revisiting the now-empty last-seen tile must clear its stale marker.");
            Assert.That(match.QueueOrder(0, match.Turn, ActionOrder.Move(1, new Cell(5, 9)), out _), Is.True);
            match.ResolveNow(32);
            Assert.That(
                match.View(0, 32).Entities.Single(e => e.Id == 7).Position,
                Is.EqualTo(new Cell(7, 9)),
                "Re-observation must expose the latest location.");
        }

        [Test]
        public void FinishedMatchRejectsActionsAndDoesNotRecoverAP()
        {
            BattleMatch match = Start(one: new Cell(2, 3), two: new Cell(2, 6));

            for (int hit = 0; hit < 8; hit++)
            {
                Assert.That(match.QueueOrder(0, match.Turn, ActionOrder.Attack(1, 102), out _), Is.True);
                match.ResolveNow(11 + hit);
            }

            Assert.That(match.Phase, Is.EqualTo(MatchPhase.Finished));
            Assert.That(match.View(0, 20).AP, Is.EqualTo(23));
            Assert.That(match.Tick(1000), Is.False);
            Assert.That(match.ResolveNow(1000), Is.False);
            Assert.That(match.QueueOrder(0, match.Turn, ActionOrder.Wait(1), out _), Is.False);
        }

        [Test]
        public void EveryRecordedTurnReplaysToTheSameState()
        {
            BattleMatch match = Start();

            match.QueueOrder(0, 1, ActionOrder.Move(1, new Cell(2, 4)), out _);
            match.ResolveNow(11);
            match.ResolveNow(12);
            BattleRecording recording = match.ExportRecording();

            foreach (TurnRecord turn in recording.Turns)
            {
                SameState(BattleResolver.Resolve(turn.Before, recording.Rules, turn.PlayerOneOrders, turn.PlayerTwoOrders).State, turn.After);
            }
        }

        [Test]
        public void InvalidRulesFailBeforeCreatingAMatch()
        {
            Assert.Throws<ArgumentException>(() => new BattleMatch(new BattleRules
            {
                Height = 9
            }));
            Assert.Throws<ArgumentException>(() => new BattleMatch(new BattleRules
            {
                PlanningSeconds = float.NaN
            }));
            Assert.Throws<ArgumentException>(() => new BattleMatch(new BattleRules
            {
                InitialAP = 26
            }));
        }
    }
}
