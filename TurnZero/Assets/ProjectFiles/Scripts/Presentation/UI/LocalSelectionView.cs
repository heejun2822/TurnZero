using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TurnZero
{
    public sealed class LocalSelectionView : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text selectedName;
        [SerializeField]
        private TMP_Text selectedHealth;
        [SerializeField]
        private TMP_Text selectedStatus;
        [SerializeField]
        private Image selectedPortrait;
        [SerializeField]
        private TMP_Text reservedInfo;

        private BattleSession session;

        public void Bind(BattleSession value)
        {
            session = value;
        }

        public void Render(BattleView view)
        {
            GameRules rules = session.Rules;
            BattleUnit unit = session.Selected;
            CharacterDefinition definition = unit == null ? null : session.Configuration.Characters[unit.Slot];

            selectedName.text = definition == null ? "SELECT A CHARACTER" : definition.Values.Name.ToUpperInvariant();
            selectedName.color = definition == null ? Color.white : definition.Accent;
            selectedHealth.text = unit == null ? "-- / -- HP" : $"{unit.Health} / {definition.Values.Health} HP";
            selectedStatus.text = unit == null
                ? "CLICK A MODEL OR CARD"
                : unit.Owner != view.Player && view.FindUnit(unit.Id) == null
                ? "LAST OBSERVED / READ ONLY"
                : !unit.Alive
                ? "DEFEATED / REVIVE NEXT TURN"
                : unit.Rooted(view.Turn)
                ? "ROOT / CANNOT MOVE"
                : BattleStatusEffects.Has(unit, StatusEffectKind.Stealth, view.Turn)
                ? "STEALTH / ENEMY CANNOT SEE"
                : "NO STATUS EFFECTS";

            selectedPortrait.sprite = definition?.Portrait;
            selectedPortrait.color = definition == null ? Color.clear : Color.white;

            BattleOrder selectedOrder = unit == null ? null : view.FindOrder(unit.Id);

            reservedInfo.text = selectedOrder == null
                ? "RESERVED  /  WAIT"
                : $"RESERVED  /  {selectedOrder.Action.ToString().ToUpperInvariant()}  /  {BattleCommands.Cost(rules, unit, selectedOrder.Action, view.Turn)} AP";

        }
    }
}
