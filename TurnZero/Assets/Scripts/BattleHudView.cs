using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static TurnZero.BattleUiStyle;
namespace TurnZero
{
    public sealed class BattleHudView : MonoBehaviour
    {
        [SerializeField] private BattleTopBarView topBar;
        [SerializeField] private BattleCommandPanelView commands;
        [SerializeField] private BattleToolbarView toolbar;
        [SerializeField] private BattleApPanelView apPanel;
        [SerializeField] private BattleUnitCardView[] cards;
        [SerializeField] private Text messageText, logText;
        public string Message { set => messageText.text = value; }
        public void Bind(Action<int> selectSlot, Action<ActionKind> mode, Action wait, Action ready,
            Action switchPlayer, BattleCameraRig camera, Action resolve, Action save, Action restart)
        {
            foreach (var card in cards) card.Bind(selectSlot);
            commands.Bind(mode, wait, ready);
            toolbar.Bind(switchPlayer, camera, resolve, save, restart);
        }
        public void Timer(MatchPhase phase, double seconds) => topBar.Timer(phase, seconds);
        public void Render(PlayerView view, BattleRules rules, int selectedId, ActionKind mode)
        {
            topBar.Render(view, rules);
            apPanel.Render(view, rules);
            toolbar.Render(view);
            foreach (var card in cards) card.Render(view, rules, selectedId);
            var selected = view.Entities.Find(e => e.Id == selectedId && e.Health > 0);
            commands.Render(view, rules, selectedId, mode, cards[selected == null ? 1 : selected.Id - view.Player * 4 - 1]);
            logText.text = view.Messages.LastOrDefault() ?? "";
            if (view.Phase == MatchPhase.Finished) messageText.text = Outcome(view.Outcome) + ". Restart to play again.";
        }
    }
}
