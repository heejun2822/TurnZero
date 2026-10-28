using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace TurnZero
{
    public sealed class BattlePlayback
    {
        private readonly BattleConfiguration configuration;
        private readonly BattleEvents events;

        public MatchState Display { get; private set; }
        public bool FastForward { get; set; }

        public BattlePlayback(BattleConfiguration configuration, BattleEvents events)
        {
            this.configuration = configuration;
            this.events = events;
        }

        public void Reset()
        {
            Display = null;
            FastForward = false;
        }

        public IEnumerator Play(BattleTimeline timeline, GameRules rules, int player, bool revealAll, Action<string> announce)
        {
            foreach (ResolutionStep step in timeline.Steps)
            {
                Display = step.ReplayStart();
                events.Refresh();

                BattleCue[] cues = step.VisibleCues(rules, player, revealAll).ToArray();

                if (step.SimultaneousMovement)
                {
                    if (cues.Length > 0)
                    {
                        events.Movement();

                        foreach (BattleCue cue in cues)
                        {
                            announce($"P{cue.Owner + 1} / {cue.Label}");
                            events.Cue(cue);
                        }

                        yield return Delay(configuration.MoveSeconds);
                    }
                }
                else
                {
                    foreach (BattleCue cue in cues)
                    {
                        announce($"P{cue.Owner + 1} / {cue.Label}");

                        if (cue.Kind == CueKind.Reward)
                        {
                            step.ApplyCue(Display, cue, rules);
                            events.Refresh();
                        }
                        else
                        {
                            events.Focus(cue.Target);
                        }

                        events.Cue(cue);
                        yield return Delay(configuration.ActionSeconds);

                        if (cue.Kind != CueKind.Reward)
                        {
                            step.ApplyCue(Display, cue, rules);
                            events.Refresh();
                        }
                    }
                }

                Display = step.After.Copy();
                Display.Phase = BattlePhase.Resolving;
                Display.Winner = BattleWinner.None;
                events.Refresh();
            }

            Display = null;
        }

        private IEnumerator Delay(float seconds)
        {
            float elapsed = 0;

            while (!FastForward && elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            yield return null;
        }
    }
}
