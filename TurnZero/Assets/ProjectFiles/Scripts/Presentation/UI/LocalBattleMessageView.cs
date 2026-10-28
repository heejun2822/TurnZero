using System.Linq;
using TMPro;
using UnityEngine;

namespace TurnZero
{
    public sealed class LocalBattleMessageView : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text messageText;
        [SerializeField]
        private TMP_Text logText;

        private BattleSession session;

        public void Bind(BattleSession value)
        {
            session = value;
        }

        public void Render()
        {
            messageText.text = session.Message;
            logText.text = string.Join("\n", session.History.Skip(Mathf.Max(0, session.History.Count - 3)));
        }
    }
}
