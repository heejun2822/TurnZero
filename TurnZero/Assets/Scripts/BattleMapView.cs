using System.Linq;
using UnityEngine;

namespace TurnZero
{
    public sealed class BattleMapView : MonoBehaviour
    {
        [SerializeField] private int width = 8;
        [SerializeField] private int height = 10;
        [SerializeField] private float spacing = 1.1f;
        [SerializeField] private BattleTileView[] tiles;
        public int Width => width;
        public int Height => height;
        public float Spacing => spacing;
        public Vector3 Position(Cell cell) => transform.TransformPoint(new Vector3(
            (cell.X - (width - 1) * 0.5f) * spacing, 0, (cell.Y - (height - 1) * 0.5f) * spacing));

        public void Bind(System.Action<Cell> click)
        {
            foreach (var tile in tiles) tile.Bind(click);
        }

        public void Render(PlayerView view, BattleRules rules, int selectedId, ActionKind mode)
        {
            var selected = view.Entities.Find(e => e.Id == selectedId && e.Health > 0);
            bool canPlan = view.Phase == MatchPhase.Planning && !view.Locked && selected != null;
            foreach (var tile in tiles)
            {
                var cell = tile.Cell;
                bool visible = view.VisibleCells.Contains(cell);
                var entity = view.Entities.Find(e => e.Position == cell && e.Health > 0);
                bool range = canPlan && (mode == ActionKind.Move ? selected.Position.Distance(cell) == 1 :
                    visible && selected.Position.Distance(cell) <= rules.AttackRange && cell != selected.Position);
                bool ordered = view.Orders.Any(o => o.Kind == ActionKind.Move && o.Destination == cell ||
                    o.Kind == ActionKind.Attack && entity != null && o.TargetId == entity.Id);
                bool selection = entity != null && entity.Id == selectedId;
                tile.Render(visible, view.Phase == MatchPhase.Placement && rules.CanPlaceBase(view.Player, cell),
                    range || ordered || selection, selection);
            }
        }
    }
}
