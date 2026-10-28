using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TurnZero
{
    public sealed class LocalBattleResultView : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text resultText;
        [SerializeField]
        private Button rematchButton;

        private BattleSession session;

        public void Bind(BattleSession value)
        {
            Unbind();
            session = value;
            rematchButton.onClick.AddListener(session.Events.Restart);
        }

        public void Render(BattleView view)
        {
            bool finished = view.Phase == BattlePhase.Finished;

            gameObject.SetActive(finished);

            if (finished)
            {
                resultText.text = view.Winner == BattleWinner.Draw
                    ? "DRAW\n<size=28%>THE BATTLE IS OVER</size>"
                    : $"PLAYER {(view.Winner == BattleWinner.PlayerOne ? 1 : 2)} WINS\n<size=28%>BATTLE COMPLETE / TURN {view.Turn:00}</size>";
            }
        }

        private void Unbind()
        {
            if (session == null)
            {
                return;
            }

            if (rematchButton != null)
            {
                rematchButton.onClick.RemoveListener(session.Events.Restart);
            }
            session = null;
        }

        private void OnDestroy()
        {
            Unbind();
        }
    }
}
