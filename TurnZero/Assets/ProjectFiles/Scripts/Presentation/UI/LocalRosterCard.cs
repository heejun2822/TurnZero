using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TurnZero
{
    public sealed class LocalRosterCard : MonoBehaviour
    {
        [SerializeField]
        private int slot;
        [SerializeField]
        private bool enemy;
        [SerializeField]
        private Button button;
        [SerializeField]
        private Image portrait;
        [SerializeField]
        private Image accent;
        [SerializeField]
        private Image background;
        [SerializeField]
        private Image healthFill;
        [SerializeField]
        private TMP_Text nameText;
        [SerializeField]
        private TMP_Text healthText;
        [SerializeField]
        private TMP_Text statusText;
        [SerializeField]
        private TMP_Text equipmentText;
        [SerializeField]
        private TMP_Text orderText;
        private BattleSession session;
        private double lastClick = -10;

        public void Bind(BattleSession value)
        {
            button.onClick.RemoveListener(Click);
            session = value;
            button.onClick.AddListener(Click);
        }

        private void Click()
        {
            bool twice = Time.unscaledTimeAsDouble - lastClick <= 0.32;

            lastClick = Time.unscaledTimeAsDouble;

            session.Events.Select((enemy ? 1 - session.Player : session.Player) * GameDefinitions.CharactersPerTeam + slot + 1, twice);
        }

        private void OnDestroy()
        {
            button.onClick.RemoveListener(Click);
        }

        public void Render(BattleView view)
        {
            int owner = enemy ? 1 - view.Player : view.Player;
            int id = owner * GameDefinitions.CharactersPerTeam + slot + 1;
            CharacterDefinition definition = session.Configuration.Characters[slot];
            BattleUnit unit = view.Units.FirstOrDefault(u => u.Id == id);
            UnitObservation observation = view.KnownEnemies.FirstOrDefault(o => o.Unit.Id == id);
            BattleUnit known = unit ?? observation?.Unit;

            portrait.sprite = definition.Portrait;
            portrait.color = known == null ? new Color(0.18f, 0.23f, 0.27f) : unit == null ? new Color(0.45f, 0.5f, 0.55f) : Color.white;

            accent.color = LocalBattleWorld.Team(owner);
            background.color = session.SelectedId == id ? new Color(0.12f, 0.26f, 0.29f) : new Color(0.055f, 0.077f, 0.10f);
            nameText.text = $"{(enemy ? "" : (slot + 1) + "  ")}{definition.Values.Name.ToUpperInvariant()}";
            healthText.text = known == null ? "? / ?" : $"{known.Health} / {definition.Values.Health}" + (unit == null ? " *" : "");
            float healthRatio = known == null ? 0 : Mathf.Clamp01((float)known.Health / definition.Values.Health);

            healthFill.rectTransform.anchorMax = new Vector2(healthRatio, 1);
            healthFill.enabled = healthRatio > 0;
            healthFill.color = known != null && known.Health <= 0 && !known.DefeatPending ? Color.gray : LocalBattleWorld.Team(owner);
            statusText.text = known == null
                ? "UNOBSERVED"
                : known.Health <= 0 && !known.DefeatPending
                ? enemy ? "DEFEATED" : "DEFEATED / REVIVE"
                : unit == null
                ? $"LAST SEEN {view.Turn - observation.Turn}T AGO"
                : !unit.Placed
                ? "DEPLOYING"
                : unit.Rooted(view.Turn)
                ? "ROOT"
                : BattleStatusEffects.Has(unit, StatusEffectKind.Stealth, view.Turn) ? "STEALTH" : "READY";
            equipmentText.text = known == null || known.Equipment < 0
                ? "NO EQUIPMENT"
                : session.Rules.Equipment[known.Equipment].Name.ToUpperInvariant();
            BattleOrder order = view.Orders.Find(o => o.UnitId == id);

            orderText.text = enemy
                ? ""
                : order == null
                ? "WAIT / 0 AP"
                : $"{order.Action.ToString().ToUpperInvariant()} / {BattleCommands.Cost(session.Rules, known, order.Action, view.Turn)} AP";
            button.interactable = !session.Running && view.Phase != BattlePhase.Setup;
        }
    }
}
