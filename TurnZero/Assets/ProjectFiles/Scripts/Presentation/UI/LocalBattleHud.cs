using UnityEngine;

namespace TurnZero
{
    public sealed class LocalBattleHud : MonoBehaviour
    {
        [SerializeField]
        private LocalMatchHeaderView header;
        [SerializeField]
        private LocalTurnTimerView timer;
        [SerializeField]
        private LocalBattleMessageView messages;
        [SerializeField]
        private LocalSelectionView selection;
        [SerializeField]
        private LocalActionButtonsView actions;
        [SerializeField]
        private LocalActionDetailsView details;
        [SerializeField]
        private LocalEquipmentView equipment;
        [SerializeField]
        private LocalEquipmentConfirmationView confirmation;
        [SerializeField]
        private LocalBattleToolbarView toolbar;
        [SerializeField]
        private LocalReadyButtonView ready;
        [SerializeField]
        private LocalBattleSetupView setup;
        [SerializeField]
        private LocalBattleHelpView help;
        [SerializeField]
        private LocalBattleResultView result;
        [SerializeField]
        private LocalTurnBannerView banner;
        [SerializeField]
        private LocalRosterCard[] cards;
        [SerializeField]
        private LocalApPanel apPanel;
        private BattleSession session;

        public void Bind(BattleSession value)
        {
            if (session != null)
            {
                session.Events.Changed -= Render;
                session.Events.CueRequested -= Cue;
            }

            session = value;
            header.Bind(value);
            timer.Bind(value);
            messages.Bind(value);
            selection.Bind(value);
            actions.Bind(value);
            details.Bind(value);
            equipment.Bind(value);
            confirmation.Bind(value);
            toolbar.Bind(value);
            ready.Bind(value);
            setup.Bind(value);
            help.Bind(value);
            result.Bind(value);
            banner.Bind(value);

            foreach (LocalRosterCard card in cards)
            {
                card.Bind(value);
            }

            session.Events.Changed += Render;
            session.Events.CueRequested += Cue;
        }

        private void Render()
        {
            BattleView view = session.View;

            header.Render(view);
            timer.Render(view);
            messages.Render();
            selection.Render(view);
            actions.Render(view);
            details.Render(view);
            equipment.Render(view);
            confirmation.Render(view);
            toolbar.Render(view);
            ready.Render(view);
            setup.Render(view);
            help.Render();
            result.Render(view);
            banner.Render(view);

            foreach (LocalRosterCard card in cards)
            {
                card.Render(view);
            }

            apPanel.Render(view, session.Rules.MaximumAP);
        }

        private void Cue(BattleCue cue)
        {
            if (cue.Kind == CueKind.Reward && cue.Owner == session.Player)
            {
                apPanel.FlashReward(cue.APDelta);
            }

            messages.Render();
        }

        private void OnDestroy()
        {
            if (session != null)
            {
                session.Events.Changed -= Render;
                session.Events.CueRequested -= Cue;
            }
        }
    }
}
