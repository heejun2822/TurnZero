using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TurnZero
{
    public sealed class LocalReadyButtonView : MonoBehaviour
    {
        [SerializeField]
        private Button readyButton;
        [SerializeField]
        private TMP_Text readyLabel;

        private BattleSession session;

        public void Bind(BattleSession value)
        {
            Unbind();
            session = value;
            readyButton.onClick.AddListener(session.Events.Ready);
        }

        public void Render(BattleView view)
        {
            readyLabel.text = view.Ready ? "READY / CLICK TO UNLOCK" : "ENTER / READY";
            readyButton.interactable = view.Phase == BattlePhase.Planning && !session.Running;
        }

        private void Unbind()
        {
            if (session == null)
            {
                return;
            }

            if (readyButton != null)
            {
                readyButton.onClick.RemoveListener(session.Events.Ready);
            }
            session = null;
        }

        private void OnDestroy()
        {
            Unbind();
        }
    }
}
