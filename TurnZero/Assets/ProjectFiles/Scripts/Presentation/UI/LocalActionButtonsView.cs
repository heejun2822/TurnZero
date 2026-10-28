using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TurnZero
{
    public sealed class LocalActionButtonsView : MonoBehaviour
    {
        [SerializeField]
        private Button[] actionButtons;
        [SerializeField]
        private TMP_Text[] actionLabels;

        private static readonly string[] ActionNames = { "Q  MOVE", "W  ATTACK", "E  SKILL", "R  EQUIPMENT", "T  WAIT / CANCEL" };
        private BattleSession session;
        private UnityAction[] handlers;

        public void Bind(BattleSession value)
        {
            Unbind();
            session = value;
            handlers = new UnityAction[actionButtons.Length];

            for (int i = 0; i < actionButtons.Length; i++)
            {
                int index = i;

                handlers[i] = () => session.Events.SetAction((BattleAction)index);
                actionButtons[i].onClick.AddListener(handlers[i]);
            }
        }

        public void Render(BattleView view)
        {
            GameRules rules = session.Rules;
            BattleUnit unit = session.Selected;

            bool own = unit != null && unit.Owner == view.Player;
            bool edit = own && view.Phase == BattlePhase.Planning && !session.Running;

            for (int i = 0; i < actionButtons.Length; i++)
            {
                ActionSpec spec = unit == null || i == 4 ? null : BattleCommands.Spec(rules, unit, (BattleAction)i);
                bool revive = own && !unit.Alive && i == 0;
                bool rooted = unit != null &&
                    unit.Rooted(view.Turn) &&
                    (i == 0 ||
                    i == 2 &&
                    rules.Characters[unit.Slot].SkillEffect == SkillEffect.StealthMove ||
                    i == 3 &&
                    unit.Equipment >= 0 &&
                    rules.Equipment[unit.Equipment].Effect == EquipmentEffect.Swap);

                actionLabels[i].text = revive
                    ? $"REVIVE / {BattleCommands.Cost(rules, unit, BattleAction.Revive, view.Turn)} AP"
                    : ActionNames[i] + (i < 4 ? $"\n{(spec == null ? "--" : spec.Cost.ToString())} AP" : "");
                actionButtons[i].interactable = edit && !rooted && (revive || unit.Alive && (i == 4 || spec != null && unit.ReadyOn[i] <= view.Turn));

                if (rooted)
                {
                    actionLabels[i].text += " / ROOT";
                }

                if (unit != null && i < 4 && unit.ReadyOn[i] > view.Turn && !revive)
                {
                    actionLabels[i].text += $" / {unit.ReadyOn[i] - view.Turn}T";
                }
            }

        }

        private void Unbind()
        {
            if (handlers == null)
            {
                return;
            }

            for (int i = 0; i < handlers.Length; i++)
            {
                if (actionButtons[i] != null)
                {
                    actionButtons[i].onClick.RemoveListener(handlers[i]);
                }
            }

            handlers = null;
        }

        private void OnDestroy()
        {
            Unbind();
        }
    }
}
