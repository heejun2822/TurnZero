using TMPro;
using UnityEngine;

namespace TurnZero
{
    public sealed class LocalTurnTimerView : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text timerText;

        private BattleSession session;
        private BattlePhase phase;
        private BattlePhase renderedPhase;
        private int renderedSeconds = -1;
        private bool rendered;

        public void Bind(BattleSession value)
        {
            session = value;
        }

        public void Render(BattleView view)
        {
            phase = view.Phase;
            RenderTime();
        }

        private void Update()
        {
            if (session != null)
            {
                RenderTime();
            }
        }

        private void RenderTime()
        {
            int seconds = Mathf.CeilToInt((float)session.SecondsLeft);

            if (rendered && renderedPhase == phase && renderedSeconds == seconds)
            {
                return;
            }

            rendered = true;
            renderedPhase = phase;
            renderedSeconds = seconds;
            timerText.text = phase == BattlePhase.Planning
                ? $"{seconds:00}s"
                : phase == BattlePhase.TurnStart ? "GET READY" : "--";
        }
    }
}
