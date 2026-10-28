using System.Collections.Generic;
using NUnit.Framework;

namespace TurnZero.Tests
{
    public sealed class BattleViewLookupTests
    {
        [Test]
        public void UnitCyclingKeepsOwnTeamFirstThenIdAndWrapsForEitherPerspective()
        {
            GridCell cell = new GridCell(4, 5);
            BattleView view = new BattleView
            {
                Player = 1,
                Units = new List<BattleUnit>
                {
                    new BattleUnit { Id = 2, Owner = 0, Placed = true, Health = 10, Cell = cell },
                    new BattleUnit { Id = 7, Owner = 1, Placed = true, Health = 10, Cell = cell },
                    new BattleUnit { Id = 8, Owner = 1, Placed = true, Health = 10, Cell = new GridCell(5, 5) },
                    new BattleUnit { Id = 5, Owner = 0, Placed = true, Health = 0, Cell = cell },
                    new BattleUnit { Id = 1, Owner = 0, Placed = true, Health = 10, Cell = cell },
                    new BattleUnit { Id = 6, Owner = 1, Placed = true, Health = 10, Cell = cell }
                }
            };

            int current = 0;

            foreach (int expected in new[] { 6, 7, 1, 2, 6 })
            {
                BattleUnit next = view.FindNextUnitAt(cell, current);

                Assert.That(next.Id, Is.EqualTo(expected));
                current = next.Id;
            }

            Assert.That(view.FindNextUnitAt(cell, 5).Id, Is.EqualTo(6));
            Assert.That(view.FindNextUnitAt(cell, 8).Id, Is.EqualTo(6));
            Assert.That(view.FindNextUnitAt(new GridCell(0, 0), 6), Is.Null);

            view.Player = 0;
            current = 0;

            foreach (int expected in new[] { 1, 2, 6, 7, 1 })
            {
                current = view.FindNextUnitAt(cell, current).Id;

                Assert.That(current, Is.EqualTo(expected));
            }
        }

        [Test]
        public void LookupsKeepObservedAndRememberedEnemiesSeparate()
        {
            BattleUnit observed = new BattleUnit { Id = 1 };
            UnitObservation remembered = new UnitObservation { Unit = new BattleUnit { Id = 6, Health = 7 } };
            UnitObservation lastSeen = new UnitObservation { Unit = new BattleUnit { Id = 6, Health = 7 } };
            BattleOrder order = new BattleOrder { UnitId = 1 };
            BattleView view = new BattleView
            {
                Units = new List<BattleUnit> { observed },
                KnownEnemies = new List<UnitObservation> { remembered },
                LastSeen = new List<UnitObservation> { lastSeen },
                Orders = new List<BattleOrder> { order }
            };

            Assert.That(view.FindUnit(1), Is.SameAs(observed));
            Assert.That(view.FindUnit(6), Is.Null);
            Assert.That(view.FindKnownEnemy(6), Is.SameAs(remembered));
            Assert.That(view.FindKnownEnemy(1), Is.Null);
            Assert.That(view.FindLastSeen(6), Is.SameAs(lastSeen));
            Assert.That(view.FindLastSeen(1), Is.Null);
            Assert.That(view.FindOrder(1), Is.SameAs(order));
            Assert.That(view.FindOrder(6), Is.Null);
        }
    }
}
