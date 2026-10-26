using System.Collections.Generic;
using System.Linq;

namespace TurnZero.Game
{
    public enum CueKind { Move, Attack, Skill, Structure, Revive, Portal }
    public sealed class BattleCue
    {
        public CueKind Kind;
        public int ActorId, Owner, Area, Damage;
        public GridCell Source, Target;
        public string Label;
        public bool SourceVisible;
        public bool HiddenMovement;
        public BattleCue Copy() => (BattleCue)MemberwiseClone();
    }
    public sealed class ResolutionStep
    {
        public MatchState Before, After;
        public List<BattleCue> Cues = new List<BattleCue>();
        public IEnumerable<BattleCue> VisibleCues(GameRules rules, int player, bool revealAll)
        {
            var before = BattleVisibility.Vision(Before, rules, player);
            var after = BattleVisibility.Vision(After, rules, player);
            foreach (var cue in Cues.OrderBy(c => c.Owner == player ? 0 : 1).ThenBy(c => c.ActorId))
            {
                if (!revealAll && cue.Owner != player && cue.HiddenMovement) continue;
                bool sourceVisible = cue.Owner == player || before.Contains(cue.Source) &&
                    Before.Units.Any(u => u.Id == cue.ActorId && BattleVisibility.CanSee(u, Before, player, before));
                bool targetVisible = BattleVisibility.Area(rules, cue.Target, System.Math.Max(0, cue.Area - 1)).Any(c => before.Contains(c) || after.Contains(c));
                if (!revealAll && cue.Owner != player && !targetVisible) continue;
                var copy = cue.Copy(); copy.SourceVisible = revealAll || sourceVisible;
                if (!copy.SourceVisible) { copy.Source = copy.Target; copy.ActorId = 0; copy.Label = cue.Kind == CueKind.Move || cue.Kind == CueKind.Portal ? "Movement observed" : "Unseen attack"; }
                yield return copy;
            }
        }
    }
    public sealed class BattleTimeline
    {
        public List<ResolutionStep> Steps = new List<ResolutionStep>();
        public MatchState Final;
    }
}
