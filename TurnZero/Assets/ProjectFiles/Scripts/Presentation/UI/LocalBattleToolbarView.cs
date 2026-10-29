using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TurnZero
{
    public sealed class LocalBattleToolbarView : MonoBehaviour
    {
        [SerializeField]
        private Button switchButton;
        [SerializeField]
        private TMP_Text switchLabel;
        [SerializeField]
        private Button resolveButton;
        [SerializeField]
        private TMP_Text resolveLabel;
        [SerializeField]
        private Button restartButton;
        [SerializeField]
        private Button pauseButton;
        [SerializeField]
        private TMP_Text pauseLabel;
        [SerializeField]
        private Button revealButton;
        [SerializeField]
        private TMP_Text revealLabel;
        [SerializeField]
        private Button helpButton;

        private BattleSession session;

        public void Bind(BattleSession value)
        {
            Unbind();
            session = value;
            switchButton.onClick.AddListener(session.Events.Switch);
            resolveButton.onClick.AddListener(session.Events.Resolve);
            restartButton.onClick.AddListener(session.Events.Restart);
            pauseButton.onClick.AddListener(session.Events.Pause);
            revealButton.onClick.AddListener(session.Events.Reveal);
            helpButton.onClick.AddListener(session.Events.Help);
        }

        public void Render(BattleView view)
        {
            switchButton.interactable = !session.IsBotMatch;
            switchLabel.text = session.IsBotMatch ? "VS BOT" : $"VIEW P{2 - view.Player}";
        }

        private void Unbind()
        {
            if (session == null)
            {
                return;
            }

            if (switchButton != null)
            {
                switchButton.onClick.RemoveListener(session.Events.Switch);
            }

            if (resolveButton != null)
            {
                resolveButton.onClick.RemoveListener(session.Events.Resolve);
            }

            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(session.Events.Restart);
            }

            if (pauseButton != null)
            {
                pauseButton.onClick.RemoveListener(session.Events.Pause);
            }

            if (revealButton != null)
            {
                revealButton.onClick.RemoveListener(session.Events.Reveal);
            }

            if (helpButton != null)
            {
                helpButton.onClick.RemoveListener(session.Events.Help);
            }
            session = null;
        }

        private void OnDestroy()
        {
            Unbind();
        }
    }
}
