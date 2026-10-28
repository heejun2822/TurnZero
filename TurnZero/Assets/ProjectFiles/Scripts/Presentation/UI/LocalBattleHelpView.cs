using UnityEngine;
using UnityEngine.UI;

namespace TurnZero
{
    public sealed class LocalBattleHelpView : MonoBehaviour
    {
        [SerializeField]
        private Button closeHelpButton;

        private BattleSession session;

        public void Bind(BattleSession value)
        {
            Unbind();
            session = value;
            closeHelpButton.onClick.AddListener(session.Events.Help);
        }

        public void Render()
        {
            gameObject.SetActive(session.ShowHelp);
        }

        private void Unbind()
        {
            if (session == null)
            {
                return;
            }

            if (closeHelpButton != null)
            {
                closeHelpButton.onClick.RemoveListener(session.Events.Help);
            }
            session = null;
        }

        private void OnDestroy()
        {
            Unbind();
        }
    }
}
