using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static TurnZero.BattleUiStyle;
namespace TurnZero
{
    public sealed class BattleCommandPanelView : MonoBehaviour
    {
        [SerializeField] private Text selectionText, selectedHealthText, equipmentText, detailsText, orderTargetText, lockText;
        [SerializeField] private Image selectedPortrait, selectedHealth, equipmentIcon, previewArrow;
        [SerializeField] private Image[] previewCells;
        [SerializeField] private Button moveButton, attackButton, waitButton, lockButton;
        public void Bind(Action<ActionKind> mode, Action wait, Action ready)
        {
            moveButton.onClick.AddListener(() => mode(ActionKind.Move));
            attackButton.onClick.AddListener(() => mode(ActionKind.Attack));
            waitButton.onClick.AddListener(() => wait());
            lockButton.onClick.AddListener(() => ready());
        }
        public void Render(PlayerView view, BattleRules rules, int selectedId, ActionKind mode, BattleUnitCardView card)
        {
            var selected = view.Entities.Find(e => e.Id == selectedId && e.Health > 0);
            int slot = card.Slot;
            selectionText.text = selected == null ? "SELECT A UNIT" : $"{slot + 1:00} {card.RoleName}";
            selectedPortrait.sprite = card.Portrait;
            selectedPortrait.color = selected == null ? new Color(1, 1, 1, 0.45f) : Color.white;
            selectedHealthText.text = selected == null ? "HP -- / --" : $"HP  {selected.Health} / {selected.MaximumHealth}";
            Fill(selectedHealth, selected == null ? 0 : (float)selected.Health / selected.MaximumHealth);
            equipmentText.text = $"{card.WeaponName}\nDMG {rules.AttackDamage}  /  RANGE {rules.AttackRange}";
            equipmentIcon.sprite = card.WeaponIcon;
            var order = view.Orders.Find(o => o.UnitId == selectedId);
            detailsText.text = order == null ? "WAIT  /  0 AP" : $"{order.Kind.ToString().ToUpperInvariant()}  /  {rules.Cost(order.Kind)} AP";
            orderTargetText.text = selected == null ? "Select an allied unit" : order == null ? "Choose a target to queue" : order.Kind == ActionKind.Move ? $"TO TILE {order.Destination}" : $"TARGET #{order.TargetId}";
            bool canPlan = view.Phase == MatchPhase.Planning && !view.Locked;
            moveButton.interactable = attackButton.interactable = waitButton.interactable = canPlan && selected != null;
            Frame(moveButton.gameObject, mode == ActionKind.Move ? Blue : Edge);
            Frame(attackButton.gameObject, mode == ActionKind.Attack ? Enemy : Edge);
            moveButton.image.color = mode == ActionKind.Move ? new Color(0.02f, 0.22f, 0.25f) : Ink;
            attackButton.image.color = mode == ActionKind.Attack ? new Color(0.25f, 0.075f, 0.1f) : Ink;
            lockButton.interactable = canPlan;
            lockText.text = view.Locked ? "READY / LOCKED" : view.Phase == MatchPhase.Placement ? "PLACE BASE" : "READY";

            foreach (var cell in previewCells) cell.color = Edge;
            previewCells[12].color = new Color(0.04f, 0.47f, 0.51f);
            previewArrow.enabled = order != null && order.Kind == ActionKind.Move && selected != null;
            if (previewArrow.enabled)
            {
                int dx = order.Destination.X - selected.Position.X;
                int dy = order.Destination.Y - selected.Position.Y;
                previewCells[12 + dx + dy * 5].color = Blue;
                previewArrow.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(dy, dx) * Mathf.Rad2Deg);
                SetAnchors(previewArrow.rectTransform, new Vector2(0.39f + dx * 0.1f, 0.39f + dy * 0.1f), new Vector2(0.61f + dx * 0.1f, 0.61f + dy * 0.1f));
            }
        }
    }
}
