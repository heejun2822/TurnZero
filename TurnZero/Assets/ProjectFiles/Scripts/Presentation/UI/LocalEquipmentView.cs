using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TurnZero
{
    public sealed class LocalEquipmentView : MonoBehaviour
    {
        [SerializeField]
        private Button[] craftButtons;
        [SerializeField]
        private TMP_Text[] craftLabels;
        [SerializeField]
        private TMP_Text craftInfo;
        [SerializeField]
        private LocalEquipmentConfirmationView confirmation;

        private BattleSession session;
        private UnityAction[] handlers;

        public void Bind(BattleSession value)
        {
            Unbind();
            session = value;
            handlers = new UnityAction[craftButtons.Length];

            for (int i = 0; i < craftButtons.Length; i++)
            {
                int index = i;

                handlers[i] = () => Craft(index);
                craftButtons[i].onClick.AddListener(handlers[i]);
            }
        }

        public void Render(BattleView view)
        {
            GameRules rules = session.Rules;
            BattleUnit unit = session.Selected;
            bool edit = unit != null && unit.Owner == view.Player && view.Phase == BattlePhase.Planning && !session.Running;

            craftInfo.text = $"INVENTORY / {view.Materials} CRAFT MATERIALS\nChoose equipment to craft. Replacement destroys the old item.";

            for (int i = 0; i < craftButtons.Length; i++)
            {
                craftLabels[i].text = i < rules.Equipment.Length
                    ? rules.Equipment[i].Name.ToUpperInvariant() + $"\n{rules.Equipment[i].Action.Cost} AP"
                    : "--";
                craftButtons[i].interactable = edit &&
                    unit.Alive &&
                    view.Materials > 0 &&
                    i < rules.Equipment.Length &&
                    session.EquipmentDecks[view.Player].Contains(i);
            }
        }

        private void Craft(int index)
        {
            if (session.Selected?.Equipment >= 0)
            {
                confirmation.Show(index);
            }
            else
            {
                session.Events.Craft(index, false);
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
                if (craftButtons[i] != null)
                {
                    craftButtons[i].onClick.RemoveListener(handlers[i]);
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
