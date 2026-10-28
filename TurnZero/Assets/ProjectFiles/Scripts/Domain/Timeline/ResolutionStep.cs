using System;
using System.Collections.Generic;
using System.Linq;

namespace TurnZero
{
    public sealed class ResolutionStep
    {
        public MatchState Before;
        public MatchState After;
        public int Phase;
        public bool SimultaneousMovement;
        public List<BattleCue> Cues = new List<BattleCue>();

        public IEnumerable<BattleCue> VisibleCues(GameRules rules, int player, bool revealAll)
        {
            HashSet<GridCell> before = BattleVisibility.Vision(Before, rules, player);
            HashSet<GridCell> after = BattleVisibility.Vision(After, rules, player);

            foreach (BattleCue cue in Cues.OrderBy(c => c.Owner == player ? 0 : 1)
                .ThenBy(c => c.Persistent ? 0 : 1)
                .ThenBy(c => c.CreatedTurn)
                .ThenBy(c => c.ActorId)
                .ThenBy(c => c.EffectId))
            {
                if (cue.Kind == CueKind.Reward && cue.Owner != player && !revealAll)
                {
                    continue;
                }

                BattleUnit a = Before.Units.Find(u => u.Id == cue.ActorId);
                BattleUnit b = After.Units.Find(u => u.Id == cue.ActorId);

                bool own = cue.Owner == player || revealAll;
                bool source = own || a != null && BattleVisibility.CanSee(a, Before, player, before);
                bool target = own || b != null && BattleVisibility.CanSee(b, After, player, after);
                bool motion = cue.Kind == CueKind.Move ||
                    cue.Kind == CueKind.Portal ||
                    cue.Kind == CueKind.Swap ||
                    cue.Kind == CueKind.Revive;
                List<GridCell> visibleCells = BattleVisibility.Area(rules, cue.Target, cue.Area)
                    .Where(c => own || before.Contains(c) || after.Contains(c))
                    .ToList();

                if (!own && (motion ? !source && !target : visibleCells.Count == 0))
                {
                    continue;
                }

                BattleCue copy = cue.Copy();

                copy.SourceVisible = source && !cue.Persistent;

                copy.VisibleCells = visibleCells;

                if (!revealAll)
                {
                    foreach (int id in copy.UnitDamage.Keys.ToArray())
                    {
                        BattleUnit unit = After.Units.Find(u => u.Id == id);

                        if (unit.Owner != player && !BattleVisibility.CanSee(unit, After, player, after))
                        {
                            copy.UnitDamage.Remove(id);
                        }
                    }
                }

                if (!own)
                {
                    copy.APDelta = 0;

                    foreach (int id in copy.Positions.Keys.ToArray())
                    {
                        BattleUnit unit = After.Units.Find(u => u.Id == id);

                        if (unit.Owner != player && !BattleVisibility.CanSee(unit, After, player, after))
                        {
                            copy.Positions.Remove(id);
                        }
                    }

                    foreach (int id in copy.StructureDamage.Keys.ToArray())
                    {
                        BattleStructure structure = After.Structures.Find(s => s.Id == id);

                        if (structure != null &&
                            structure.Owner != player &&
                            !before.Contains(structure.Cell) &&
                            !after.Contains(structure.Cell))
                        {
                            copy.StructureDamage.Remove(id);
                        }
                    }

                    for (int team = 0; team < GameDefinitions.PlayerCount; team++)
                    {
                        if (team != player && !before.Contains(After.Bases[team].Cell) && !after.Contains(After.Bases[team].Cell))
                        {
                            copy.BaseDamage[team] = 0;
                        }
                    }

                    if (!target && motion)
                    {
                        copy.Target = copy.Source;
                    }
                    else if (!motion && !after.Contains(copy.Target) && !before.Contains(copy.Target) && visibleCells.Count > 0)
                    {
                        copy.Target = visibleCells[0];
                    }

                    if (!source)
                    {
                        copy.Source = copy.Target;

                        if (!motion)
                        {
                            copy.ActorId = 0;
                            copy.Label = cue.Persistent ? "BURN" : "UNSEEN ATTACK";
                        }
                    }
                }

                yield return copy;
            }
        }

        public MatchState ReplayStart()
        {
            MatchState state = Before.Copy();

            state.Phase = BattlePhase.Resolving;
            state.Winner = BattleWinner.None;

            return state;
        }

        public void ApplyCue(MatchState display, BattleCue cue, GameRules rules)
        {
            if ((cue.Kind == CueKind.Attack || cue.Kind == CueKind.Skill) && !cue.Persistent)
            {
                display.Revealed[cue.Owner].UnionWith(BattleVisibility.Area(rules, cue.Target, cue.Area));
            }

            BattleCombat.ApplyDamage(display, cue);
            display.AP[cue.Owner] = Math.Max(0, Math.Min(rules.MaximumAP, display.AP[cue.Owner] + cue.APDelta));

            foreach (KeyValuePair<int, GridCell> position in cue.Positions)
            {
                display.Units.Find(u => u.Id == position.Key).Cell = position.Value;
            }

            BattleVisibility.Observe(display, rules);
        }
    }
}
