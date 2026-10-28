using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TurnZero
{
    public sealed class LocalBattleSetupView : MonoBehaviour
    {
        [SerializeField]
        private Button[] fieldButtons;
        [SerializeField]
        private TMP_Text[] fieldLabels;
        [SerializeField]
        private Button[] deckButtons;
        [SerializeField]
        private TMP_Text[] deckLabels;
        [SerializeField]
        private Button startButton;
        [SerializeField]
        private Button autoDeployButton;
        [SerializeField]
        private TMP_Text[] rosterTexts;
        private UnityAction[] fieldHandlers;
        private UnityAction[] deckHandlers;

        private BattleSession session;

        public void Bind(BattleSession value)
        {
            Unbind();
            session = value;
            startButton.onClick.AddListener(session.Events.Deploy);
            autoDeployButton.onClick.AddListener(session.Events.AutoDeploy);
            fieldHandlers = new UnityAction[fieldButtons.Length];
            deckHandlers = new UnityAction[deckButtons.Length];

            for (int i = 0; i < fieldButtons.Length; i++)
            {
                int index = i;

                fieldHandlers[i] = () => session.Events.Field(index / 3, index % 3);
                fieldButtons[i].onClick.AddListener(fieldHandlers[i]);
            }

            for (int i = 0; i < deckButtons.Length; i++)
            {
                int index = i;

                deckHandlers[i] = () => session.Events.Deck(index / GameDefinitions.EquipmentDeckSlots, index % GameDefinitions.EquipmentDeckSlots);
                deckButtons[i].onClick.AddListener(deckHandlers[i]);
            }
        }

        public void Render(BattleView view)
        {
            gameObject.SetActive(view.Phase == BattlePhase.Setup);
            GameRules rules = session.Rules;

            for (int i = 0; i < fieldButtons.Length; i++)
            {
                int field = i % 3;

                fieldButtons[i].interactable = field < session.Configuration.Fields.Length;

                fieldLabels[i].text = (session.Fields[i / 3] == field ? "> " : "") + (field < session.Configuration.Fields.Length
                    ? session.Configuration.Fields[field].DisplayName.ToUpperInvariant()
                    : "EMPTY");
            }

            for (int i = 0; i < deckButtons.Length; i++)
            {
                int type = session.EquipmentDecks[i / GameDefinitions.EquipmentDeckSlots][i % GameDefinitions.EquipmentDeckSlots];

                deckLabels[i].text = $"{i % GameDefinitions.EquipmentDeckSlots + 1}\n" + (type < 0 ? "EMPTY" : rules.Equipment[type].Name.ToUpperInvariant());
            }

            foreach (TMP_Text text in rosterTexts)
            {
                text.text = "STARTER ROSTER / FIVE DISTINCT CHARACTERS\n" + string.Join(" / ", rules.Characters.Select(c => $"{c.Name.ToUpperInvariant()} {c.Health}HP"));
            }

        }

        private void Unbind()
        {
            if (session == null)
            {
                return;
            }

            if (startButton != null)
            {
                startButton.onClick.RemoveListener(session.Events.Deploy);
            }

            if (autoDeployButton != null)
            {
                autoDeployButton.onClick.RemoveListener(session.Events.AutoDeploy);
            }
            if (fieldHandlers != null)
            {
                for (int i = 0; i < fieldHandlers.Length; i++)
                {
                    if (fieldButtons[i] != null)
                    {
                        fieldButtons[i].onClick.RemoveListener(fieldHandlers[i]);
                    }
                }

                fieldHandlers = null;
            }

            if (deckHandlers != null)
            {
                for (int i = 0; i < deckHandlers.Length; i++)
                {
                    if (deckButtons[i] != null)
                    {
                        deckButtons[i].onClick.RemoveListener(deckHandlers[i]);
                    }
                }

                deckHandlers = null;
            }

            session = null;
        }

        private void OnDestroy()
        {
            Unbind();
        }
    }
}
