using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static TurnZero.BattleUiStyle;
namespace TurnZero
{
    public sealed class BattleTopBarView : MonoBehaviour
    {
        [SerializeField] private Text phaseText, timerText, ownBaseText, enemyBaseText;
        [SerializeField] private Image ownBaseBar, enemyBaseBar;
        public void Render(PlayerView view, BattleRules rules)
        {
            phaseText.text = view.Phase == MatchPhase.Placement ? $"P{view.Player + 1} / PLACE YOUR BASE" : view.Phase == MatchPhase.Finished ? Outcome(view.Outcome) : $"TURN {view.Turn:00}  |  PLAN ORDERS";
            phaseText.color = view.Phase == MatchPhase.Finished ? Gold : Muted;
            Timer(view.Phase, view.SecondsLeft);
            Gauge(view, rules, view.Player, ownBaseText, ownBaseBar, Health);
            Gauge(view, rules, 1 - view.Player, enemyBaseText, enemyBaseBar, Enemy);
        }
        public void Timer(MatchPhase phase, double seconds) => timerText.text = phase == MatchPhase.Planning ? $"00:{Math.Ceiling(seconds):00}" : "--:--";
        private static void Gauge(PlayerView view, BattleRules rules, int owner, Text text, Image bar, Color color)
        {
            var entity = view.Entities.Find(e => e.Owner == owner && e.Kind == EntityKind.Base);
            var memory = view.LastSeen.Find(o => o.Entity.Owner == owner && o.Entity.Kind == EntityKind.Base);
            var known = entity ?? memory?.Entity;
            text.text = known == null ? $"? / {rules.BaseHealth}" : $"{known.Health}{(entity == null ? "*" : "")} / {known.MaximumHealth}";
            bar.color = entity == null ? Muted : color;
            Fill(bar, known == null ? 0 : (float)known.Health / known.MaximumHealth);
        }
    }
}
