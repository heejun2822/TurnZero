using TMPro;
using UnityEngine;

namespace TurnZero
{
    public sealed class LocalMatchHeaderView : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text phaseText;
        [SerializeField]
        private TMP_Text baseText;
        [SerializeField]
        private TMP_Text perspectiveText;

        private BattleSession session;

        public void Bind(BattleSession value)
        {
            session = value;
        }

        public void Render(BattleView view)
        {
            GameRules rules = session.Rules;

            phaseText.text = view.Phase == BattlePhase.Setup
                ? "MATCH SETUP"
                : view.Phase == BattlePhase.BasePlacement
                ? "DEPLOY BASE"
                : view.Phase == BattlePhase.UnitPlacement
                ? $"DEPLOY {view.PlacementSlot + 1} / 5"
                : $"TURN {view.Turn:00}  /  {view.Phase.ToString().ToUpperInvariant()}";
            perspectiveText.text = $"P{view.Player + 1} PERSPECTIVE" + (session.RevealAll ? "  /  DEBUG VISION" : "");
            baseText.text = $"P{view.Player + 1}  {view.OwnBase.Health}/{rules.BaseHealth}     VS     P{2 - view.Player}  " + (view.EnemyBase == null
                ? "?/?"
                : $"{view.EnemyBase.Health}/{rules.BaseHealth}{(view.EnemyBaseVisible ? "" : " *")}");
        }
    }
}
