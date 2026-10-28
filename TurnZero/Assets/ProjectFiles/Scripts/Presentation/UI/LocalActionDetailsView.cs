using TMPro;
using UnityEngine;

namespace TurnZero
{
    public sealed class LocalActionDetailsView : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text actionInfo;

        private BattleSession session;

        public void Bind(BattleSession value)
        {
            session = value;
        }

        public void Render(BattleView view)
        {
            GameRules rules = session.Rules;
            BattleUnit unit = session.Selected;

            if (unit == null)
            {
                actionInfo.text = "Select an ally to inspect actions and reserve a command.";
            }
            else if (!unit.Alive)
            {
                actionInfo.text = "REVIVE / Target your field inside base vision. Requires another living ally. Full HP; no second action this turn.";
            }
            else
            {
                ActionSpec spec = BattleCommands.Spec(rules, unit, session.Action);
                bool movement = session.Action == BattleAction.Move ||
                    session.Action == BattleAction.Skill &&
                    rules.Characters[unit.Slot].SkillEffect == SkillEffect.StealthMove;
                bool globalSwap = session.Action == BattleAction.Equipment &&
                    unit.Equipment >= 0 &&
                    rules.Equipment[unit.Equipment].Effect == EquipmentEffect.Swap;
                string scope = spec == null
                    ? ""
                    : globalSwap
                    ? "RANGE GLOBAL / 1 ALLY"
                    : movement ? $"PATH {spec.Range}" : $"RANGE {spec.Range} / AREA {spec.Area}";

                actionInfo.text = spec == null
                    ? "Equip an item using a crafting material below. QWER chooses actions; T waits for free."
                    : $"<color=#E7B768>{spec.Name.ToUpperInvariant()}</color>\n{spec.Cost} AP  /  {scope}\nPHASE {spec.Priority}  /  COOLDOWN {spec.Cooldown} / READY T{((int)session.Action < 4 ? unit.ReadyOn[(int)session.Action] : view.Turn)}\n{spec.Description}";
            }

        }
    }
}
