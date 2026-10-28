using System.Collections.Generic;
using System.Linq;

namespace TurnZero
{
    public sealed class BattleTimeline
    {
        public List<ResolutionStep> Steps = new List<ResolutionStep>();
        public MatchState Final;
        public void Record(MatchState state, GameRules rules, ResolutionStep step)
        {
            BattleVisibility.Observe(state,rules); step.After = state.Copy(); Steps.Add(step);
        }
    }
}
