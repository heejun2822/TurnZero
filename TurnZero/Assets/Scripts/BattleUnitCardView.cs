using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static TurnZero.BattleUiStyle;
namespace TurnZero
{
    public sealed class BattleUnitCardView : MonoBehaviour
    {
        [SerializeField] private int slot;
        [SerializeField] private string roleName;
        [SerializeField] private string weaponName;
        [SerializeField] private Sprite portrait, weaponIcon;
        [SerializeField] private Sprite moveIcon, attackIcon, waitIcon;
        [SerializeField] private Button unitButton;
        [SerializeField] private Text unitLabel, unitHealthText, unitOrderText;
        [SerializeField] private Image unitHealth, unitOrderPanel, unitOrderIcon;
        public int Slot => slot;
        public string RoleName => roleName;
        public string WeaponName => weaponName;
        public Sprite Portrait => portrait;
        public Sprite WeaponIcon => weaponIcon;
        public void Bind(Action<int> select) => unitButton.onClick.AddListener(() => select(slot));
        public void Render(PlayerView view, BattleRules rules, int selectedId)
        {
            int id = view.Player * 4 + slot + 1;
            var unit = view.Entities.Find(e => e.Id == id);
            var order = view.Orders.Find(o => o.UnitId == id);
            var kind = order == null ? ActionKind.Wait : order.Kind;
            Color accent = kind == ActionKind.Move ? Blue : kind == ActionKind.Attack ? Enemy : Muted;
            unitLabel.text = $"{slot + 1:00} {roleName}";
            unitHealthText.text = unit == null ? "HP -- / --" : unit.Health == 0 ? "DEFEATED" : $"HP  {unit.Health} / {unit.MaximumHealth}";
            Fill(unitHealth, unit == null ? 0 : (float)unit.Health / unit.MaximumHealth);
            unitButton.interactable = view.Phase == MatchPhase.Planning && unit != null && unit.Health > 0;
            Frame(unitButton.gameObject, unit != null && unit.Id == selectedId ? Blue : Edge);
            unitOrderText.text = kind == ActionKind.Wait ? "WAIT" : $"{kind.ToString().ToUpperInvariant()} / {rules.Cost(kind)} AP";
            unitOrderText.color = accent;
            unitOrderIcon.sprite = kind == ActionKind.Move ? moveIcon : kind == ActionKind.Attack ? attackIcon : waitIcon;
            unitOrderIcon.color = accent;
            Frame(unitOrderPanel.gameObject, kind == ActionKind.Wait ? Edge : accent);
            unitOrderPanel.color = kind == ActionKind.Move ? new Color(0.025f, 0.18f, 0.23f) : kind == ActionKind.Attack ? new Color(0.2f, 0.055f, 0.08f) : Panel;
        }
    }
}
