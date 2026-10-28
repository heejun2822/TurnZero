using TMPro;
using UnityEngine;

namespace TurnZero
{
    public sealed class LocalTurnBannerView : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text phaseBannerText;

        private BattleSession session;

        public void Bind(BattleSession value)
        {
            session = value;
        }

        public void Render(BattleView view)
        {
            bool starting = view.Phase == BattlePhase.TurnStart;

            gameObject.SetActive(starting);

            if (starting)
            {
                phaseBannerText.text = $"TURN {view.Turn:00}\n<size=35%>+{session.Rules.TurnIncome} AP  /  PLAN YOUR NEXT MOVE</size>";
            }
        }
    }
}
