using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace TurnZero.Tests
{
    public sealed class LocalBattleTests
    {
        public static GameRules Rules()
        {
            ActionSpec A(int range, int area, int damage, int cost, int priority, int cooldown = 1) =>
                new ActionSpec { Name = "Test action", Range = range, Area = area, Damage = damage, Cost = cost, Priority = priority, Cooldown = cooldown };
            var hp = new[] { 14,7,10,9,6 }; var move = new[] { 3,6,4,4,2 }; var range = new[] { 3,4,5,5,8 };
            var damage = new[] { 3,2,3,3,3 }; var skillRange = new[] { 2,4,5,5,12 }; var skillDamage = new[] { 6,0,2,4,5 };
            var rules = new GameRules { Characters = new CharacterSpec[5], Equipment = new[] {
                new EquipmentSpec { Id = "EQP-01", Name = "Portal", Effect = EquipmentEffect.Portal, Action = A(1,1,0,5,4), Health = 5, Duration = 3, ExitRange = 10 },
                new EquipmentSpec { Id = "EQP-02", Name = "Swap", Effect = EquipmentEffect.Swap, Action = A(100,1,0,5,4) },
                new EquipmentSpec { Id = "EQP-03", Name = "Beacon", Effect = EquipmentEffect.Beacon, Action = A(2,4,0,4,1,2), Health = 3 }
            } };
            var effects = new[] { SkillEffect.Damage, SkillEffect.StealthMove, SkillEffect.BurningGround, SkillEffect.RootDamage, SkillEffect.Precision };
            for (int i = 0; i < 5; i++) rules.Characters[i] = new CharacterSpec { Id = $"CHR-0{i+1}", Name = $"Character {i+1}", Health = hp[i], SkillEffect = effects[i],
                Move = A(move[i],0,0,i == 4 ? 3 : 2,2), Attack = A(range[i], i == 2 ? 2 : 1,damage[i],i == 4 ? 3 : 2,3),
                Skill = A(skillRange[i], i == 4 ? 0 : i >= 2 ? 3 : 1, skillDamage[i], new[] { 4,3,5,5,4 }[i],i == 1 ? 1 : 3,i >= 3 ? 2 : 1) };
            return rules;
        }
        public static MatchState Arena(GameRules rules)
        {
            var match = new LocalBattleMatch(rules); match.AutoDeploy(0); match.Tick(2);
            var state = match.DebugSnapshot();
            state.Bases[0].Cell = new GridCell(0,0); state.Bases[1].Cell = new GridCell(11,19);
            foreach (var unit in state.Units) unit.Cell = new GridCell(unit.Slot + 2, unit.Owner == 0 ? 8 : 9);
            return state;
        }
        private static BattleOrder Attack(int id, int x, int y, bool skill = false) => new BattleOrder { UnitId = id, Action = skill ? BattleAction.Skill : BattleAction.Attack, Target = new GridCell(x,y) };
        private static BattleOrder Move(int id, params GridCell[] path) => new BattleOrder { UnitId = id, Action = BattleAction.Move, Target = path.Last(), Path = path.ToList() };

        [Test] public void DeploymentUsesFiveManualPositionsAndStartsWithTenAPExactlyOnce()
        {
            var rules = Rules(); var match = new LocalBattleMatch(rules);
            Assert.That(match.View(0,0).AP, Is.Zero); match.BeginPlacement();
            Assert.That(match.Place(0,new GridCell(3,9),0,out _), Is.False);
            match.AutoDeploy(0);
            Assert.That(match.Phase, Is.EqualTo(BattlePhase.TurnStart));
            Assert.That(match.View(0,0).Units.Count(u => u.Owner == 0 && u.Alive), Is.EqualTo(5));
            Assert.That(match.View(0,0).AP, Is.EqualTo(10)); Assert.That(match.Tick(1.99), Is.False);
            Assert.That(match.Tick(2), Is.True); Assert.That(match.Tick(2), Is.False);
            Assert.That(match.View(0,2).AP, Is.EqualTo(10)); Assert.That(match.View(0,2).SecondsLeft, Is.EqualTo(30));
            match.Resolve(); match.CompleteResolution(3); Assert.That(match.View(0,3).AP, Is.EqualTo(20));
            match.Tick(5); match.Resolve(); match.CompleteResolution(6); Assert.That(match.View(0,6).AP, Is.EqualTo(25));
        }
        [Test] public void ReservationsReplaceCancelAndPreserveValidOrdersWhenOverBudget()
        {
            var rules = Rules(); rules.TurnIncome = 4; var match = new LocalBattleMatch(rules); match.AutoDeploy(0); match.Tick(2);
            Assert.That(match.Queue(0,1,Attack(1,4,6),out _), Is.True);
            Assert.That(match.Queue(0,1,Attack(2,5,6),out _), Is.True);
            Assert.That(match.Queue(0,1,Attack(1,4,6,true),out _), Is.False);
            Assert.That(match.View(0,2).Orders.Single(o => o.UnitId == 1).Action, Is.EqualTo(BattleAction.Attack));
            Assert.That(match.View(0,2).AP, Is.EqualTo(4)); Assert.That(match.View(0,2).ReservedAP, Is.EqualTo(4));
            match.Queue(0,1,new BattleOrder { UnitId = 2, Action = BattleAction.Wait },out _);
            Assert.That(match.View(0,2).ReservedAP, Is.EqualTo(2));
            Assert.That(match.Queue(0,0,Attack(1,4,6),out _), Is.False);
        }
        [Test] public void BothReadyEndsPlanningOnceAndLocksLateEdits()
        {
            var match = new LocalBattleMatch(Rules()); match.AutoDeploy(0); match.Tick(2);
            Assert.That(match.Ready(0), Is.True); Assert.That(match.ShouldResolve(2), Is.False);
            Assert.That(match.Queue(0,1,Attack(1,5,6),out _), Is.False);
            Assert.That(match.Ready(1), Is.True); Assert.That(match.ShouldResolve(2), Is.True);
            Assert.That(match.Resolve(), Is.Not.Null); Assert.That(match.Resolve(), Is.Null);
            Assert.That(match.CompleteResolution(3), Is.True); Assert.That(match.CompleteResolution(3), Is.False);
            Assert.That(match.Turn, Is.EqualTo(2));
        }
        [Test] public void AlliedAndEnemyUnitsCanShareTilesAndPathsWithoutOverwriting()
        {
            var rules = Rules(); var state = Arena(rules);
            state.Units.Find(u => u.Id == 1).Cell = new GridCell(3,8); state.Units.Find(u => u.Id == 6).Cell = new GridCell(3,10);
            state.Orders.Add(Move(1,new GridCell(3,9))); state.Orders.Add(Move(6,new GridCell(3,9)));
            var result = BattleSimulation.Resolve(state,rules).Final;
            Assert.That(result.Units.Count(u => u.Cell == new GridCell(3,9) && u.Alive), Is.GreaterThanOrEqualTo(2));
            CollectionAssert.AreEqual(new[] { 8,8 },result.AP);
            Assert.That(state.Units.Find(u => u.Id == 1).Cell, Is.EqualTo(new GridCell(3,8)));
        }
        [Test] public void ManualPathsRejectJumpsOverRangeAndBaseTiles()
        {
            var rules = Rules(); var state = Arena(rules);
            Assert.That(BattleCommands.Validate(state,rules,0,Move(1,new GridCell(2,10))), Is.Not.Null);
            state.Bases[0].Cell = new GridCell(2,9);
            Assert.That(BattleCommands.Validate(state,rules,0,Move(1,new GridCell(2,9))), Is.Not.Null);
            Assert.That(BattlePathfinding.FindPath(state,rules,new GridCell(2,8),new GridCell(2,10),3,0), Is.Empty);
            var reachable = BattlePathfinding.Reachable(state,rules,new GridCell(2,8),3,0);
            Assert.That(reachable.Contains(new GridCell(2,10)),Is.False); Assert.That(reachable.Contains(new GridCell(2,9)),Is.False);
            Assert.That(reachable.Contains(new GridCell(1,8)),Is.True);
        }
        [Test] public void SimultaneousLethalAttacksBothLandAndPlaybackIsViewerFirst()
        {
            var rules = Rules(); var state = Arena(rules); state.Units.Find(u => u.Id == 1).Health = 3; state.Units.Find(u => u.Id == 6).Health = 3;
            state.Orders.Add(Attack(1,2,9)); state.Orders.Add(Attack(6,2,8));
            var timeline = BattleSimulation.Resolve(state,rules);
            Assert.That(timeline.Final.Units.Find(u => u.Id == 1).Health, Is.Zero); Assert.That(timeline.Final.Units.Find(u => u.Id == 6).Health, Is.Zero);
            var batch = timeline.Steps.Single(s => s.Cues.Any(c => c.Kind == CueKind.Attack));
            Assert.That(batch.VisibleCues(rules,0,false).First().ActorId, Is.EqualTo(1));
            Assert.That(batch.VisibleCues(rules,1,false).First().ActorId, Is.EqualTo(6));
            Assert.That(batch.Before.Units.Find(u => u.Id == 6).Health, Is.EqualTo(3));
            CollectionAssert.AreEqual(new[] { 12,12 }, timeline.Final.AP);
        }
        [Test] public void AValidBlindMissSpendsOnceAndTargetsATileInsteadOfTrackingAUnit()
        {
            var rules = Rules(); var state = Arena(rules);
            state.Orders.Add(Attack(1,2,9)); state.Orders.Add(Move(6,new GridCell(2,10),new GridCell(2,11)));
            var result = BattleSimulation.Resolve(state,rules).Final;
            Assert.That(result.Units.Find(u => u.Id == 6).Health, Is.EqualTo(14));
            Assert.That(result.AP[0], Is.EqualTo(8));
        }
        [Test] public void SamePhaseFireDeathPreservesTheAcceptedAttackAndItsCost()
        {
            var rules = Rules(); var state = Arena(rules); state.Units.Find(u => u.Id == 1).Health = 2;
            state.Fires.Add(new FireZone { Id = 999, Owner = 1, Cell = new GridCell(2,8), Area = 1, Damage = 2, ThroughTurn = 3 });
            state.Orders.Add(Attack(1,2,9));
            var result = BattleSimulation.Resolve(state,rules).Final;
            Assert.That(result.Units.Find(u => u.Id == 1).Health, Is.Zero); Assert.That(result.AP[0], Is.EqualTo(8));
            Assert.That(result.Units.Find(u => u.Id == 6).Health, Is.EqualTo(11));
        }
        [Test] public void AreaDamageHitsEveryEnemyStackAndRewardsEachDefeatOnce()
        {
            var rules = Rules(); var state = Arena(rules);
            foreach (var unit in state.Units.Where(u => u.Owner == 1)) { unit.Cell = new GridCell(4,9); unit.Health = 2; }
            state.Orders.Add(Attack(3,4,9)); state.Orders.Add(Attack(4,4,9));
            var result = BattleSimulation.Resolve(state,rules).Final;
            Assert.That(result.Units.Where(u => u.Owner == 1).All(u => u.Health == 0), Is.True); Assert.That(result.AP[0], Is.EqualTo(25));
        }
        [Test] public void RootStartsNextTurnAndCooldownTwoSkipsOneTurn()
        {
            var rules = Rules(); var state = Arena(rules); state.Orders.Add(Attack(4,2,9,true));
            var result = BattleSimulation.Resolve(state,rules).Final; var target = result.Units.Find(u => u.Id == 6);
            Assert.That(target.Rooted(1), Is.False); Assert.That(target.Rooted(2), Is.True); Assert.That(target.Rooted(3), Is.False);
            Assert.That(result.Units.Find(u => u.Id == 4).ReadyOn[(int)BattleAction.Skill], Is.EqualTo(3));
        }
        [Test] public void StealthHidesAnEnemyInVisibleTilesAndExpiresAfterNextTurn()
        {
            var rules = Rules(); var state = Arena(rules); var unit = state.Units.Find(u => u.Id == 2);
            var order = Move(2,new GridCell(3,9)); order.Action = BattleAction.Skill; state.Orders.Add(order);
            var timeline = BattleSimulation.Resolve(state,rules); var result = timeline.Final;
            Assert.That(BattleVisibility.View(result,rules,1,0).Units.Any(u => u.Id == 2), Is.False);
            Assert.That(timeline.Steps.SelectMany(s => s.VisibleCues(rules,1,false)).Any(c => c.Owner == 0 && c.Kind == CueKind.Move),Is.False,"Hidden movement must not reach the opponent's effects or camera.");
            Assert.That(timeline.Steps.SelectMany(s => s.VisibleCues(rules,0,false)).Any(c => c.ActorId == 2),Is.True);
            result.Turn = 3; Assert.That(BattleVisibility.View(result,rules,1,0).Units.Any(u => u.Id == 2), Is.True);
            Assert.That(unit.Cell, Is.EqualTo(new GridCell(3,8)));
        }
        [Test] public void ReviveDiscountStartsTheTurnAfterDeathAndEquipmentIsLost()
        {
            var rules = Rules(); var state = Arena(rules); var unit = state.Units.Find(u => u.Id == 1); unit.Health = 0; unit.DiedTurn = 1;
            var order = new BattleOrder { UnitId = 1, Action = BattleAction.Revive, Target = new GridCell(1,0) };
            Assert.That(BattleCommands.Validate(state,rules,0,order), Is.Not.Null);
            state.Turn = 2; state.AP[0] = 25; Assert.That(BattleCommands.Cost(state,rules,order), Is.EqualTo(15));
            state.Turn = 20; Assert.That(BattleCommands.Cost(state,rules,order), Is.EqualTo(10)); state.Turn = 2;
            state.Orders.Add(order); var result = BattleSimulation.Resolve(state,rules).Final;
            Assert.That(result.Units.Find(u => u.Id == 1).Health, Is.EqualTo(14)); Assert.That(result.AP[0], Is.EqualTo(10));
        }
        [Test] public void CraftingConsumesMaterialsAndRequiresExplicitReplacementConfirmation()
        {
            var match = new LocalBattleMatch(Rules()); match.AutoDeploy(0); match.Tick(2);
            Assert.That(match.ShouldResolve(31.999),Is.False); Assert.That(match.ShouldResolve(32),Is.True);
            Assert.That(match.Craft(0,1,2,false,out _), Is.True); Assert.That(match.View(0,2).Materials, Is.EqualTo(1));
            Assert.That(match.Craft(0,1,0,false,out _), Is.False); Assert.That(match.View(0,2).Materials, Is.EqualTo(1));
            Assert.That(match.Craft(0,1,0,true,out _), Is.True); Assert.That(match.Craft(0,2,1,false,out _), Is.False);
        }
        [Test] public void ABeaconUsesPriorityOneAndPortalSurvivesItsOwnersDeath()
        {
            var rules = Rules(); var state = Arena(rules); var unit = state.Units.Find(u => u.Id == 1); unit.Equipment = 0;
            state.Orders.Add(new BattleOrder { UnitId = 1, Action = BattleAction.Equipment, Target = new GridCell(2,7), Exit = new GridCell(5,7) });
            var result = BattleSimulation.Resolve(state,rules).Final;
            Assert.That(result.Structures.Count, Is.EqualTo(2)); Assert.That(result.Structures.All(s => s.ReadyTurn == 3), Is.True);
            result.Units.Find(u => u.Id == 1).Health = 0; Assert.That(result.Structures.All(s => s.Health == 5), Is.True);
            var beaconState = Arena(rules); beaconState.Units.Find(u => u.Id == 1).Equipment = 2;
            beaconState.Orders.Add(new BattleOrder { UnitId = 1, Action = BattleAction.Equipment, Target = new GridCell(2,7) });
            Assert.That(BattleSimulation.Resolve(beaconState,rules).Final.Structures.Single().Vision, Is.EqualTo(4));
        }
        [Test] public void BothPlayersCollectTheSameItemOnceEvenWhenTheyOverlap()
        {
            var rules = Rules(); var state = Arena(rules); state.Units.Find(u => u.Id == 1).Cell = new GridCell(3,8); state.Units.Find(u => u.Id == 6).Cell = new GridCell(3,10);
            state.Items.Add(new GroundItem { Id = 900, Cell = new GridCell(3,9) });
            state.Orders.Add(Move(1,new GridCell(3,9))); state.Orders.Add(Move(6,new GridCell(3,9)));
            var result = BattleSimulation.Resolve(state,rules).Final; CollectionAssert.AreEqual(new[] { 3,3 },result.Materials);
            Assert.That(result.Items, Is.Empty);
        }
        [Test] public void HiddenCardsKeepTheirObservedHealthInsteadOfReceivingLiveState()
        {
            var rules = Rules(); var state = Arena(rules); BattleVisibility.Observe(state,rules); var enemy = state.Units.Find(u => u.Id == 6);
            enemy.Cell = new GridCell(10,18); enemy.Health = 1;
            foreach (var ally in state.Units.Where(u => u.Owner == 0)) ally.Cell = new GridCell(0,2);
            var view = BattleVisibility.View(state,rules,0,0);
            Assert.That(view.Units.Any(u => u.Id == 6), Is.False); Assert.That(view.LastSeen.Single(o => o.Unit.Id == 6).Unit.Health, Is.EqualTo(14));
            view.LastSeen.Single(o => o.Unit.Id == 6).Unit.Health = 999; Assert.That(state.Memory[0][6].Unit.Health, Is.EqualTo(14));
        }
        [Test] public void ConfirmedVictoryCasesAreSymmetricAndNoEliminationGetsAGraceTurn()
        {
            var rules = Rules(); var state = Arena(rules);
            foreach (var unit in state.Units) unit.Health = 0;
            state.Bases[0].Health = 20; state.Bases[1].Health = 10; Assert.That(BattleVictory.Determine(state), Is.EqualTo(BattleWinner.PlayerOne));
            state.Bases[0].Health = 5; Assert.That(BattleVictory.Determine(state), Is.EqualTo(BattleWinner.PlayerTwo));
            state.Bases[0].Health = 10; Assert.That(BattleVictory.Determine(state), Is.EqualTo(BattleWinner.Draw));
            state.Units[0].Health = 14; state.Bases[0].Health = 0; Assert.That(BattleVictory.Determine(state), Is.EqualTo(BattleWinner.PlayerTwo));
            state.Bases[0].Health = 30; Assert.That(BattleVictory.Determine(state), Is.EqualTo(BattleWinner.PlayerOne));
        }
        [Test] public void ReversingOrdersAndEntitiesDoesNotChangeResolution()
        {
            var rules = Rules(); var state = Arena(rules); state.Orders.Add(Attack(1,2,9)); state.Orders.Add(Attack(2,3,9)); state.Orders.Add(Attack(6,2,8));
            var expected = BattleSimulation.Resolve(state,rules).Final;
            state.Orders.Reverse(); state.Units.Reverse(); var actual = BattleSimulation.Resolve(state,rules).Final;
            CollectionAssert.AreEqual(expected.AP,actual.AP);
            CollectionAssert.AreEqual(expected.Units.OrderBy(u => u.Id).Select(u => u.Health),actual.Units.OrderBy(u => u.Id).Select(u => u.Health));
        }
        [Test] public void ResolutionDoesNotSubtractReservationsFromAlreadySpentAP()
        {
            var rules = Rules(); var state = Arena(rules); state.AP[0] = 2; state.Orders.Add(Attack(1,2,9));
            var result = BattleSimulation.Resolve(state,rules).Final; result.Phase = BattlePhase.Resolving;
            var view = BattleVisibility.View(result,rules,0,0); Assert.That(view.AP,Is.Zero); Assert.That(view.ReservedAP,Is.Zero); Assert.That(view.Orders,Is.Empty);
        }
        [Test] public void ObservedDeathsRemainKnownWithoutLeavingGhostMarkers()
        {
            var rules = Rules(); var state = Arena(rules); BattleVisibility.Observe(state,rules);
            state.Units.Find(u => u.Id == 6).Health = 0; BattleVisibility.Observe(state,rules);
            var view = BattleVisibility.View(state,rules,0,0);
            Assert.That(view.KnownEnemies.Single(o => o.Unit.Id == 6).Unit.Health,Is.Zero);
            Assert.That(view.LastSeen.Any(o => o.Unit.Id == 6),Is.False);
        }
        [Test] public void SwapCanceledByAnEarlierTargetDeathSpendsNoEquipmentAP()
        {
            var rules = Rules(); var state = Arena(rules); state.Units.Find(u => u.Id == 1).Equipment = 1; state.Units.Find(u => u.Id == 2).Health = 3;
            state.Orders.Add(new BattleOrder { UnitId = 1, Action = BattleAction.Equipment, TargetUnit = 2, Target = new GridCell(3,8) });
            state.Orders.Add(Attack(6,3,8));
            var result = BattleSimulation.Resolve(state,rules).Final; Assert.That(result.Units.Find(u => u.Id == 2).Health,Is.Zero); Assert.That(result.AP[0],Is.EqualTo(10));
        }
        [Test] public void ItemSpawningIsReproducibleAndGeneratesOneMaterialPerRegionOnTurnFive()
        {
            var rules = Rules(); var one = new LocalBattleMatch(rules); var two = new LocalBattleMatch(rules);
            foreach (var match in new[] { one,two })
            {
                match.AutoDeploy(0);
                while (match.Turn < 5) { match.Tick(match.Turn*10); match.Resolve(); match.CompleteResolution(match.Turn*10+1); }
            }
            var items = one.DebugSnapshot().Items; Assert.That(items.Count,Is.EqualTo(3));
            Assert.That(items.Count(i => i.Cell.Y < rules.HomeHeight),Is.EqualTo(1));
            Assert.That(items.Count(i => i.Cell.Y >= rules.HomeHeight && i.Cell.Y < rules.HomeHeight+rules.NeutralHeight),Is.EqualTo(1));
            CollectionAssert.AreEqual(items.Select(i => i.Cell),two.DebugSnapshot().Items.Select(i => i.Cell));
        }
        [Test] public void CompletedPortalTriggersOnArrivalOnlyAndCollectsAtTheExit()
        {
            var rules = Rules(); var state = Arena(rules); state.Turn = 3;
            state.Structures.Add(new BattleStructure { Id = 901, Link = 900, Owner = 0, Kind = StructureKind.PortalEntrance, Health = 5, Completed = true, ReadyTurn = 3, Cell = new GridCell(2,9), Destination = new GridCell(7,9) });
            state.Structures.Add(new BattleStructure { Id = 902, Link = 900, Owner = 0, Kind = StructureKind.PortalExit, Health = 5, Completed = true, ReadyTurn = 3, Cell = new GridCell(7,9), Destination = new GridCell(2,9) });
            state.Items.Add(new GroundItem { Id = 903, Cell = new GridCell(7,9) });
            state.Orders.Add(Move(1,new GridCell(2,9)));
            var arrived = BattleSimulation.Resolve(state,rules).Final;
            Assert.That(arrived.Units.Find(u => u.Id == 1).Cell,Is.EqualTo(new GridCell(7,9))); Assert.That(arrived.Materials[0],Is.EqualTo(3));
            state.Orders.Clear(); state.Orders.Add(Move(1,new GridCell(2,9),new GridCell(3,9)));
            Assert.That(BattleSimulation.Resolve(state,rules).Final.Units.Find(u => u.Id == 1).Cell,Is.EqualTo(new GridCell(3,9)));
        }
        [Test] public void DestroyingOnePortalEndpointRemovesBothWithoutACharacterKillReward()
        {
            var rules = Rules(); var state = Arena(rules);
            state.Structures.Add(new BattleStructure { Id = 901, Link = 900, Owner = 0, Kind = StructureKind.PortalEntrance, Health = 2, ReadyTurn = 3, Cell = new GridCell(2,9), Destination = new GridCell(7,9) });
            state.Structures.Add(new BattleStructure { Id = 902, Link = 900, Owner = 0, Kind = StructureKind.PortalExit, Health = 5, Completed = true, ReadyTurn = 3, Cell = new GridCell(7,9), Destination = new GridCell(2,9) });
            state.Orders.Add(Attack(6,2,9)); var result = BattleSimulation.Resolve(state,rules).Final;
            Assert.That(result.Structures.All(s => s.Health == 0),Is.True); Assert.That(result.AP[1],Is.EqualTo(8));
        }
    }
}
