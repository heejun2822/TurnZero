using System.Linq;
using NUnit.Framework;

namespace TurnZero.Game.Tests
{
    public sealed class SpecificationTests
    {
        [Test] public void AttackAreaUsesTheDeclaredManhattanRadius()
        {
            var rules = LocalBattleTests.Rules(); var state = LocalBattleTests.Arena(rules);
            state.Units.Single(u => u.Id == 6).Cell = new GridCell(3,9);
            state.Orders.Add(new BattleOrder { UnitId = 1, Action = BattleAction.Attack, Target = new GridCell(2,9) });
            var result = BattleSimulation.Resolve(state,rules).Final;
            Assert.That(result.Units.Single(u => u.Id == 6).Health,Is.EqualTo(11));
        }

        [Test] public void AnAcceptedAttackKeepsItsTargetAfterAnEarlierSwapMovesTheAttacker()
        {
            var rules = LocalBattleTests.Rules(); rules.Equipment[1].Action.Priority = 1;
            var state = LocalBattleTests.Arena(rules); var ally = state.Units.Single(u => u.Id == 2);
            ally.Cell = new GridCell(10,8); ally.Equipment = 1;
            state.Orders.Add(new BattleOrder { UnitId = 2, Action = BattleAction.Equipment, TargetUnit = 1, Target = new GridCell(2,8) });
            state.Orders.Add(new BattleOrder { UnitId = 1, Action = BattleAction.Attack, Target = new GridCell(2,9) });
            var result = BattleSimulation.Resolve(state,rules).Final;
            Assert.That(result.Units.Single(u => u.Id == 1).Cell,Is.EqualTo(new GridCell(10,8)));
            Assert.That(result.Units.Single(u => u.Id == 6).Health,Is.EqualTo(11));
            Assert.That(result.AP[0],Is.EqualTo(3));
        }

        [Test] public void DeathClearsAllCooldownsBeforeRevival()
        {
            var rules = LocalBattleTests.Rules(); var state = LocalBattleTests.Arena(rules);
            var target = state.Units.Single(u => u.Id == 6); target.Health = 1; target.ReadyOn = new[] { 5,6,7,8 };
            state.Orders.Add(new BattleOrder { UnitId = 1, Action = BattleAction.Attack, Target = target.Cell });
            var result = BattleSimulation.Resolve(state,rules).Final.Units.Single(u => u.Id == 6);
            Assert.That(result.Alive,Is.False); CollectionAssert.AreEqual(new[] { 0,0,0,0 },result.ReadyOn);
        }

        [Test] public void SurvivingBaseWinsEvenIfBothTeamsHaveNoLivingCharacters()
        {
            var state = LocalBattleTests.Arena(LocalBattleTests.Rules());
            foreach (var unit in state.Units) unit.Health = 0;
            state.Bases[0].Health = 0;
            Assert.That(BattleVictory.Determine(state),Is.EqualTo(BattleWinner.PlayerTwo));
            state.Bases[0].Health = 30; state.Bases[1].Health = 0;
            Assert.That(BattleVictory.Determine(state),Is.EqualTo(BattleWinner.PlayerOne));
        }

        [Test] public void RewardsWaitUntilWrapAndDeathWaitsUntilPhaseEnd()
        {
            var rules = LocalBattleTests.Rules(); var state = LocalBattleTests.Arena(rules);
            state.Units.Single(u => u.Id == 6).Health = 1;
            state.Orders.Add(new BattleOrder { UnitId = 1, Action = BattleAction.Attack, Target = new GridCell(2,9) });
            var timeline = BattleSimulation.Resolve(state,rules);
            var attack = timeline.Steps.Single(s => s.Cues.Any(c => c.Kind == CueKind.Attack));
            var pending = attack.After.Units.Single(u => u.Id == 6);
            Assert.That(pending.Health,Is.Zero); Assert.That(pending.Alive,Is.True);
            Assert.That(attack.After.AP[0],Is.EqualTo(8));
            Assert.That(timeline.Steps.Last().Cues.Single(c => c.Kind == CueKind.Reward).APDelta,Is.EqualTo(4));
            Assert.That(timeline.Final.AP[0],Is.EqualTo(12)); Assert.That(timeline.Final.Units.Single(u => u.Id == 6).Alive,Is.False);
        }

        [Test] public void ReviveRunsAtPhaseZeroBeforeAnAttackAndClearsPendingEffects()
        {
            var rules = LocalBattleTests.Rules(); var state = LocalBattleTests.Arena(rules); state.Turn = 2; state.AP[0] = 25;
            var unit = state.Units.Single(u => u.Id == 1); unit.Health = 0; unit.DiedTurn = 1;
            BattleStatusEffects.Apply(unit,BattleStatusEffects.Root,1,6);
            state.Units.Single(u => u.Id == 6).Cell = new GridCell(0,2);
            state.Orders.Add(new BattleOrder { UnitId = 1, Action = BattleAction.Revive, Target = new GridCell(0,1) });
            state.Orders.Add(new BattleOrder { UnitId = 6, Action = BattleAction.Attack, Target = new GridCell(0,1) });
            var timeline = BattleSimulation.Resolve(state,rules);
            Assert.That(timeline.Steps.First(s => s.Cues.Count > 0).Phase,Is.Zero);
            Assert.That(timeline.Final.Units.Single(u => u.Id == 1).Health,Is.EqualTo(11));
            Assert.That(timeline.Final.Units.Single(u => u.Id == 1).Effects,Is.Empty);
        }

        [Test] public void EarlierItemContactWinsAndLaterContactCannotReceiveTheRemovedItem()
        {
            var rules = LocalBattleTests.Rules(); var state = LocalBattleTests.Arena(rules);
            state.Units.Single(u => u.Id == 6).Cell = new GridCell(3,10);
            state.Items.Add(new GroundItem { Id = 900, Cell = new GridCell(2,9) });
            state.Orders.Add(new BattleOrder { UnitId = 1, Action = BattleAction.Move, Target = new GridCell(2,9),
                Path = new System.Collections.Generic.List<GridCell> { new GridCell(2,9) } });
            state.Orders.Add(new BattleOrder { UnitId = 6, Action = BattleAction.Move, Target = new GridCell(2,9),
                Path = new System.Collections.Generic.List<GridCell> { new GridCell(3,9),new GridCell(2,9) } });
            var result = BattleSimulation.Resolve(state,rules).Final;
            CollectionAssert.AreEqual(new[] { 3,2 },result.Materials); Assert.That(result.Items,Is.Empty);
        }

        [Test] public void PortalExitAndOrdinaryArrivalInTheSameStepBothReceiveTheItem()
        {
            var rules = LocalBattleTests.Rules(); var state = LocalBattleTests.Arena(rules); state.Turn = 3;
            state.Units.Single(u => u.Id == 6).Cell = new GridCell(7,10);
            state.Structures.Add(new BattleStructure { Id = 901, Kind = StructureKind.PortalEntrance, Owner = 0, Health = 5,
                Completed = true, Cell = new GridCell(2,9), Destination = new GridCell(7,9) });
            state.Items.Add(new GroundItem { Id = 900, Cell = new GridCell(7,9) });
            state.Orders.Add(new BattleOrder { UnitId = 1, Action = BattleAction.Move, Target = new GridCell(2,9),
                Path = new System.Collections.Generic.List<GridCell> { new GridCell(2,9) } });
            state.Orders.Add(new BattleOrder { UnitId = 6, Action = BattleAction.Move, Target = new GridCell(7,9),
                Path = new System.Collections.Generic.List<GridCell> { new GridCell(7,9) } });
            var one = BattleSimulation.Resolve(state,rules).Final; state.Orders.Reverse(); state.Units.Reverse();
            var two = BattleSimulation.Resolve(state,rules).Final;
            CollectionAssert.AreEqual(new[] { 3,3 },one.Materials); CollectionAssert.AreEqual(one.Materials,two.Materials);
            Assert.That(one.Items,Is.Empty);
        }

        [Test] public void OneMovementStepCommitsBothPositionsBeforeObserving()
        {
            var rules = LocalBattleTests.Rules(); var state = LocalBattleTests.Arena(rules);
            state.Units.Single(u => u.Id == 6).Cell = new GridCell(2,10);
            state.Orders.Add(new BattleOrder { UnitId = 1, Action = BattleAction.Move, Target = new GridCell(2,9),
                Path = new System.Collections.Generic.List<GridCell> { new GridCell(2,9) } });
            state.Orders.Add(new BattleOrder { UnitId = 6, Action = BattleAction.Move, Target = new GridCell(2,9),
                Path = new System.Collections.Generic.List<GridCell> { new GridCell(2,9) } });
            var step = BattleSimulation.Resolve(state,rules).Steps.Single(s => s.SimultaneousMovement);
            Assert.That(step.Cues.Count,Is.EqualTo(2));
            Assert.That(step.After.Units.Single(u => u.Id == 1).Cell,Is.EqualTo(new GridCell(2,9)));
            Assert.That(step.After.Units.Single(u => u.Id == 6).Cell,Is.EqualTo(new GridCell(2,9)));
        }

        [Test] public void FireDoesNotDamageOnEntryAndTicksAtItsSkillPhase()
        {
            var rules = LocalBattleTests.Rules(); var state = LocalBattleTests.Arena(rules); state.Turn = 2;
            state.Fires.Add(new FireZone { Id = 900, Owner = 1, CreatorId = 8, CreatedTurn = 1, ThroughTurn = 3,
                Cell = new GridCell(2,9), Area = 0, Damage = 2 });
            state.Orders.Add(new BattleOrder { UnitId = 1, Action = BattleAction.Move, Target = new GridCell(2,9),
                Path = new System.Collections.Generic.List<GridCell> { new GridCell(2,9) } });
            var timeline = BattleSimulation.Resolve(state,rules);
            Assert.That(timeline.Steps.Single(s => s.SimultaneousMovement).After.Units.Single(u => u.Id == 1).Health,Is.EqualTo(14));
            Assert.That(timeline.Steps.Single(s => s.Cues.Any(c => c.Persistent)).Phase,Is.EqualTo(3));
            Assert.That(timeline.Final.Units.Single(u => u.Id == 1).Health,Is.EqualTo(12));
        }

        [Test] public void RootRefreshKeepsTheActiveTurnAndTheNextPendingTurn()
        {
            var unit = new BattleUnit(); BattleStatusEffects.Apply(unit,BattleStatusEffects.Root,4,1);
            BattleStatusEffects.Apply(unit,BattleStatusEffects.Root,5,2);
            BattleStatusEffects.Apply(unit,BattleStatusEffects.Stealth,5,3);
            Assert.That(unit.Rooted(5),Is.True); Assert.That(unit.Rooted(6),Is.True); Assert.That(unit.Rooted(7),Is.False);
            Assert.That(BattleStatusEffects.Has(unit,StatusEffectKind.Stealth,5),Is.True);
            Assert.That(unit.Effects.Count(e => e.Kind == StatusEffectKind.Root),Is.EqualTo(2));
        }

        [Test] public void BeaconDetectsStealthOnlyWhileItsVisionIsOperational()
        {
            var rules = LocalBattleTests.Rules(); var state = LocalBattleTests.Arena(rules); var enemy = state.Units.Single(u => u.Id == 6);
            BattleStatusEffects.Apply(enemy,BattleStatusEffects.Stealth,1,6);
            state.Structures.Add(new BattleStructure { Id = 900, Owner = 0, Kind = StructureKind.Beacon, Cell = new GridCell(2,8), Vision = 4, Health = 3 });
            Assert.That(BattleVisibility.CanSee(enemy,state,0,BattleVisibility.Vision(state,rules,0)),Is.True);
            state.Structures[0].Health = 0;
            Assert.That(BattleVisibility.CanSee(enemy,state,0,BattleVisibility.Vision(state,rules,0)),Is.False);
            Assert.That(BattleStatusEffects.Has(enemy,StatusEffectKind.Stealth,1),Is.True);
        }

        [Test] public void HiddenDeathPublishesNoHiddenPositionAndHiddenRevivalStaysUnknown()
        {
            var rules = LocalBattleTests.Rules(); var state = LocalBattleTests.Arena(rules);
            var enemy = state.Units.Single(u => u.Id == 6); enemy.Cell = new GridCell(11,18); enemy.Health = 0; enemy.DiedTurn = 1;
            state.Memory[0].Clear(); BattleVisibility.Observe(state,rules);
            var dead = BattleVisibility.View(state,rules,0,0).KnownEnemies.Single(o => o.Unit.Id == 6);
            Assert.That(dead.Unit.Health,Is.Zero); Assert.That(dead.HasPosition,Is.False);
            Assert.That(dead.Unit.Cell,Is.Not.EqualTo(enemy.Cell));
            enemy.Health = 14; enemy.DiedTurn = 0; state.Turn = 2; BattleVisibility.Observe(state,rules);
            Assert.That(BattleVisibility.View(state,rules,0,0).KnownEnemies.Single(o => o.Unit.Id == 6).Unit.Health,Is.Zero);
        }

        [Test] public void UnobservedPickupKeepsTheOpponentsGrayItemMemory()
        {
            var rules = LocalBattleTests.Rules(); var state = LocalBattleTests.Arena(rules);
            state.Items.Add(new GroundItem { Id = 900, Cell = new GridCell(2,9) }); BattleVisibility.Observe(state,rules);
            foreach (var unit in state.Units.Where(u => u.Owner == 0)) unit.Cell = new GridCell(0,0);
            var enemy = state.Units.Single(u => u.Id == 6); enemy.Cell = new GridCell(2,9);
            BattleItems.Collect(state,new[] { enemy }); BattleVisibility.Observe(state,rules);
            Assert.That(state.Items,Is.Empty);
            Assert.That(BattleVisibility.View(state,rules,0,0).Items.Any(i => i.Id == 900),Is.True);
            Assert.That(BattleVisibility.View(state,rules,1,0).Items.Any(i => i.Id == 900),Is.False);
        }

        [Test] public void ConflictingNewInstallationsAllFailWithoutCosts()
        {
            var rules = LocalBattleTests.Rules(); var state = LocalBattleTests.Arena(rules);
            foreach (int id in new[] { 1,2 })
            {
                state.Units.Single(u => u.Id == id).Equipment = 2;
                state.Orders.Add(new BattleOrder { UnitId = id, Action = BattleAction.Equipment, Target = new GridCell(3,8) });
            }
            var result = BattleSimulation.Resolve(state,rules).Final;
            Assert.That(result.Structures,Is.Empty); Assert.That(result.AP[0],Is.EqualTo(10));
            Assert.That(result.Units.Single(u => u.Id == 1).ReadyOn[3],Is.Zero);
        }

        [Test] public void OverlappingSwapRequestsAllFailWithoutCosts()
        {
            var rules = LocalBattleTests.Rules(); var state = LocalBattleTests.Arena(rules);
            state.Units.Single(u => u.Id == 1).Equipment = 1; state.Units.Single(u => u.Id == 2).Equipment = 1;
            state.Orders.Add(new BattleOrder { UnitId = 1, Action = BattleAction.Equipment, TargetUnit = 2 });
            state.Orders.Add(new BattleOrder { UnitId = 2, Action = BattleAction.Equipment, TargetUnit = 3 });
            var result = BattleSimulation.Resolve(state,rules).Final;
            Assert.That(result.AP[0],Is.EqualTo(10));
            Assert.That(result.Units.Single(u => u.Id == 1).Cell,Is.EqualTo(new GridCell(2,8)));
        }

        [Test] public void ReadyCanBeReopenedBeforeTheOtherTeamFinishesWithoutResettingTime()
        {
            var match = new LocalBattleMatch(LocalBattleTests.Rules()); match.AutoDeploy(0); match.Tick(2);
            Assert.That(match.Ready(0,3),Is.True); Assert.That(match.Ready(0,5),Is.True);
            Assert.That(match.View(0,5).Ready,Is.False); Assert.That(match.View(0,5).SecondsLeft,Is.EqualTo(27));
            Assert.That(match.View(0,5).AP,Is.EqualTo(10)); Assert.That(match.Ready(0,32),Is.False);
        }

        [Test] public void DeadlineRejectsCraftAndOrdersWithoutChangingState()
        {
            var match = new LocalBattleMatch(LocalBattleTests.Rules()); match.AutoDeploy(0); match.Tick(2);
            Assert.That(match.Craft(0,1,0,false,out _,32),Is.False);
            Assert.That(match.Queue(0,1,new BattleOrder { UnitId = 1, Action = BattleAction.Wait },out _,32),Is.False);
            Assert.That(match.View(0,32).Materials,Is.EqualTo(2)); Assert.That(match.View(0,32).Orders,Is.Empty);
        }

        [Test] public void DecksAreDistinctReusableCraftListsAndFreezeAfterDeployment()
        {
            var match = new LocalBattleMatch(LocalBattleTests.Rules());
            Assert.That(match.SetEquipmentDeck(0,new[] { 0,0 }),Is.False);
            Assert.That(match.SetEquipmentDeck(0,new int[0]),Is.False); Assert.That(match.SetEquipmentDeck(0,new[] { 2 }),Is.True);
            match.AutoDeploy(0); match.Tick(2);
            Assert.That(match.SetEquipmentDeck(0,new[] { 1 }),Is.False);
            Assert.That(match.Craft(0,1,0,false,out _),Is.False);
            Assert.That(match.Craft(0,1,2,false,out _),Is.True); Assert.That(match.Craft(0,2,2,false,out _),Is.True);
        }

        [Test] public void ReplayUsesTheViewersOrderAndDefersDeathUntilThePhaseEnds()
        {
            var rules = LocalBattleTests.Rules(); var state = LocalBattleTests.Arena(rules);
            state.Units.Single(u => u.Id == 1).Health = 3; state.Units.Single(u => u.Id == 6).Health = 3;
            state.Orders.Add(new BattleOrder { UnitId = 1, Action = BattleAction.Attack, Target = new GridCell(2,9) });
            state.Orders.Add(new BattleOrder { UnitId = 6, Action = BattleAction.Attack, Target = new GridCell(2,8) });
            var timeline = BattleSimulation.Resolve(state,rules);
            var attack = timeline.Steps.Single(s => s.Cues.Any(c => c.Kind == CueKind.Attack));
            foreach (int viewer in new[] { 0,1 })
            {
                var cues = attack.VisibleCues(rules,viewer,false).ToArray();
                Assert.That(cues.First().Owner,Is.EqualTo(viewer));
                var display = attack.ReplayStart();
                attack.ApplyCue(display,cues[0],rules);
                var victim = display.Units.Single(u => u.Id == (viewer == 0 ? 6 : 1));
                Assert.That(victim.Health,Is.Zero); Assert.That(victim.Alive,Is.True);
                attack.ApplyCue(display,cues[1],rules);
                Assert.That(display.Units.Single(u => u.Id == 1).Health,Is.Zero);
                Assert.That(display.Units.Single(u => u.Id == 6).Health,Is.Zero);
                Assert.That(display.Units.Single(u => u.Id == 1).Alive,Is.True);
            }
            var end = timeline.Steps.Last(s => s.Phase == 3);
            Assert.That(end.After.Units.Single(u => u.Id == 1).Alive,Is.False);
            Assert.That(end.After.Units.Single(u => u.Id == 6).Alive,Is.False);
        }

        [Test] public void AnUnseenAttackOnlyExposesTheVisiblePartOfItsArea()
        {
            var rules = LocalBattleTests.Rules(); var state = LocalBattleTests.Arena(rules);
            rules.BaseVision = rules.UnitVision = 1;
            foreach (var unit in state.Units.Where(u => u.Owner == 0)) unit.Cell = new GridCell(0,0);
            var enemy = state.Units.Single(u => u.Id == 6); enemy.Cell = new GridCell(10,10);
            var step = new ResolutionStep { Before = state.Copy(), After = state.Copy() };
            step.Cues.Add(new BattleCue { Kind = CueKind.Attack, ActorId = 6, Owner = 1,
                Source = enemy.Cell, Target = new GridCell(3,0), Area = 3, Damage = 3 });
            var cue = step.VisibleCues(rules,0,false).Single(); var vision = BattleVisibility.Vision(state,rules,0);
            Assert.That(cue.SourceVisible,Is.False); Assert.That(cue.ActorId,Is.Zero);
            Assert.That(cue.VisibleCells.Count > 0,Is.True);
            Assert.That(cue.VisibleCells.All(c => vision.Contains(c)),Is.True);
            Assert.That(vision.Contains(cue.Source) && vision.Contains(cue.Target),Is.True);
        }

        [Test] public void MovementAcrossTheVisionBoundaryDoesNotExposeItsHiddenEndpoint()
        {
            var rules = LocalBattleTests.Rules(); var state = LocalBattleTests.Arena(rules);
            rules.BaseVision = rules.UnitVision = 1;
            foreach (var unit in state.Units.Where(u => u.Owner == 0)) unit.Cell = new GridCell(0,0);
            foreach (bool entering in new[] { false,true })
            {
                var seen = new GridCell(1,0); var hidden = new GridCell(2,0);
                var source = entering ? hidden : seen; var target = entering ? seen : hidden;
                state.Units.Single(u => u.Id == 6).Cell = source;
                var step = new ResolutionStep { Before = state.Copy(), After = state.Copy(), SimultaneousMovement = true };
                step.After.Units.Single(u => u.Id == 6).Cell = target;
                step.Cues.Add(new BattleCue { Kind = CueKind.Move, ActorId = 6, Owner = 1, Source = source, Target = target });
                var cue = step.VisibleCues(rules,0,false).Single();
                Assert.That(cue.Source,Is.EqualTo(seen)); Assert.That(cue.Target,Is.EqualTo(seen));
            }
        }

        [Test] public void PortalCompletionOccursAtTheEndOfPhaseFourOfTheThirdTurn()
        {
            var rules = LocalBattleTests.Rules(); var state = LocalBattleTests.Arena(rules);
            state.Units.Single(u => u.Id == 1).Equipment = 0;
            state.Orders.Add(new BattleOrder { UnitId = 1, Action = BattleAction.Equipment,
                Target = new GridCell(2,9), Exit = new GridCell(7,9) });
            var first = BattleSimulation.Resolve(state,rules); var installed = first.Steps.Single(s => s.Cues.Any(c => c.Kind == CueKind.Structure));
            Assert.That(installed.After.Structures.All(s => !s.Active && !s.Completed),Is.True);
            state = first.Final; Assert.That(state.Structures.All(s => s.Active && !s.Completed && s.ReadyTurn == 3),Is.True);
            state.Orders.Clear(); state.Turn = 2; state = BattleSimulation.Resolve(state,rules).Final;
            Assert.That(state.Structures.All(s => !s.Completed),Is.True);
            state.Turn = 3; var third = BattleSimulation.Resolve(state,rules);
            var completion = third.Steps.Last(s => s.Phase == 4);
            Assert.That(completion.Before.Structures.All(s => !s.Completed),Is.True);
            Assert.That(completion.After.Structures.All(s => s.Completed),Is.True);
        }

        [Test] public void ANewBeaconDetectsStealthOnlyAfterItsInstallationPhaseEnds()
        {
            var rules = LocalBattleTests.Rules(); rules.UnitVision = 1;
            var state = LocalBattleTests.Arena(rules);
            foreach (var unit in state.Units.Where(u => u.Owner == 0 && u.Id != 1)) unit.Cell = new GridCell(0,0);
            var enemy = state.Units.Single(u => u.Id == 6); enemy.Cell = new GridCell(6,8);
            BattleStatusEffects.Apply(enemy,BattleStatusEffects.Stealth,1,6);
            state.Units.Single(u => u.Id == 1).Equipment = 2;
            state.Orders.Add(new BattleOrder { UnitId = 1, Action = BattleAction.Equipment, Target = new GridCell(3,8) });
            var timeline = BattleSimulation.Resolve(state,rules);
            var installed = timeline.Steps.Single(s => s.Cues.Any(c => c.Kind == CueKind.Structure));
            var end = timeline.Steps.Last(s => s.Phase == 1);
            Assert.That(BattleVisibility.CanSee(enemy,installed.After,0,BattleVisibility.Vision(installed.After,rules,0)),Is.False);
            Assert.That(BattleVisibility.CanSee(enemy,end.After,0,BattleVisibility.Vision(end.After,rules,0)),Is.True);
        }


        [Test] public void AnAttackCueDoesNotPublishAnUnobservedStealthVictimsDamage()
        {
            var rules = LocalBattleTests.Rules(); var state = LocalBattleTests.Arena(rules);
            var enemy = state.Units.Single(u => u.Id == 6);
            BattleStatusEffects.Apply(enemy,BattleStatusEffects.Stealth,1,6);
            state.Orders.Add(new BattleOrder { UnitId = 1, Action = BattleAction.Attack, Target = enemy.Cell });
            var timeline = BattleSimulation.Resolve(state,rules);
            var step = timeline.Steps.Single(s => s.Cues.Any(c => c.Kind == CueKind.Attack));
            Assert.That(step.VisibleCues(rules,0,false).Single().UnitDamage.ContainsKey(6),Is.False);
            Assert.That(timeline.Final.Units.Single(u => u.Id == 6).Health,Is.EqualTo(11));
        }


        [Test] public void TemporaryAttackVisionClearsWhenTheMatchEndsAsWellAsBetweenTurns()
        {
            var rules = LocalBattleTests.Rules(); rules.Characters[4].Skill.Damage = 100;
            var match = new LocalBattleMatch(rules); match.AutoDeploy(0); match.Tick(2);
            var target = match.DebugSnapshot().Bases[1].Cell;
            Assert.That(match.Queue(0,1,new BattleOrder { UnitId = 5, Action = BattleAction.Skill, Target = target },out _),Is.True);
            match.Resolve(); Assert.That(match.DebugSnapshot().Revealed[0].Contains(target),Is.True);
            match.CompleteResolution(3);
            Assert.That(match.Phase,Is.EqualTo(BattlePhase.Finished));
            Assert.That(match.DebugSnapshot().Revealed[0],Is.Empty);
            Assert.That(match.DebugSnapshot().Revealed[1],Is.Empty);
        }

    }
}
