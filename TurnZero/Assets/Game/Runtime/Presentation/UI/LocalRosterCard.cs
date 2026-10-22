using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TurnZero.Game
{
    public sealed class LocalRosterCard : MonoBehaviour
    {
        [SerializeField] private int slot;
        [SerializeField] private bool enemy;
        [SerializeField] private Button button;
        [SerializeField] private Image portrait, accent, background, healthFill;
        [SerializeField] private TMP_Text nameText, healthText, statusText, equipmentText, orderText;
        private BattleSession session;
        private double lastClick = -10;
        public void Bind(BattleSession value) { session = value; button.onClick.AddListener(Click); }
        private void Click()
        {
            bool twice = Time.unscaledTimeAsDouble - lastClick <= 0.32; lastClick = Time.unscaledTimeAsDouble;
            session.Events.Select((enemy ? 1-session.Player : session.Player) * 5 + slot + 1,twice);
        }
        public void Render(BattleView view)
        {
            int owner = enemy ? 1-view.Player : view.Player, id = owner * 5 + slot + 1;
            var definition = session.Configuration.Characters[slot];
            var unit = view.Units.FirstOrDefault(u => u.Id == id);
            var observation = view.KnownEnemies.FirstOrDefault(o => o.Unit.Id == id);
            var known = unit ?? observation?.Unit;
            portrait.sprite = definition.Portrait; portrait.color = known == null ? new Color(0.18f,0.23f,0.27f) : unit == null ? new Color(0.45f,0.5f,0.55f) : Color.white;
            accent.color = LocalBattleWorld.Team(owner);
            background.color = session.SelectedId == id ? new Color(0.12f,0.26f,0.29f) : new Color(0.055f,0.077f,0.10f);
            nameText.text = $"{(enemy ? "" : (slot+1)+"  ")}{definition.Values.Name.ToUpperInvariant()}";
            healthText.text = known == null ? "? / ?" : $"{known.Health} / {definition.Values.Health}" + (unit == null ? " *" : "");
            healthFill.fillAmount = known == null ? 0 : (float)known.Health / definition.Values.Health;
            healthFill.color = known != null && known.Health <= 0 ? Color.gray : LocalBattleWorld.Team(owner);
            statusText.text = known == null ? "UNOBSERVED" : known.Health <= 0 ? enemy ? "DEFEATED" : "DEFEATED / REVIVE" : unit == null ? $"LAST SEEN {view.Turn-observation.Turn}T AGO" : !unit.Placed ? "DEPLOYING" : unit.Rooted(view.Turn) ? "ROOT" : unit.HiddenThrough >= view.Turn && unit.HiddenThrough > 0 ? "STEALTH" : "READY";
            equipmentText.text = known == null || known.Equipment < 0 ? "NO EQUIPMENT" : session.Rules.Equipment[known.Equipment].Name.ToUpperInvariant();
            var order = view.Orders.Find(o => o.UnitId == id);
            orderText.text = enemy ? "" : order == null ? "WAIT / 0 AP" : $"{order.Action.ToString().ToUpperInvariant()} / {BattleCommands.Cost(session.Rules,known,order.Action,view.Turn)} AP";
            button.interactable = !session.Running && view.Phase != BattlePhase.Setup;
        }
    }
}
