using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TurnZero
{
    public sealed class BattleMapView : MonoBehaviour
    {
        [SerializeField] private int width = 16;
        [SerializeField] private int homeHeight = 10;
        [SerializeField] private int neutralHeight = 4;
        [SerializeField] private Tilemap alliedField;
        [SerializeField] private Tilemap neutralField;
        [SerializeField] private Tilemap enemyField;
        private BattleTileView[] tiles;
        public int Width => width;
        public int Height => homeHeight * 2 + neutralHeight;
        public int HomeHeight => homeHeight;
        public int NeutralHeight => neutralHeight;
        public float Spacing => alliedField.layoutGrid.cellSize.x;
        public Tilemap AlliedField => alliedField;
        public Tilemap NeutralField => neutralField;
        public Tilemap EnemyField => enemyField;
        public Tilemap[] Fields => new[] { alliedField, neutralField, enemyField };
        public Tilemap FieldFor(Cell cell) => cell.Y < homeHeight ? alliedField : cell.Y < homeHeight + neutralHeight ? neutralField : enemyField;
        public Vector3 Position(Cell cell) => alliedField.GetCellCenterWorld(new Vector3Int(cell.X, cell.Y, 0));
        public Cell WorldToCell(Vector3 point)
        {
            var cell = alliedField.WorldToCell(point);
            return new Cell(cell.x, cell.y);
        }

        public string LayoutError()
        {
            if (Fields.Any(f => f == null || f.layoutGrid == null)) return "Assign all three field Tilemaps to the same Grid.";
            if (Fields.Any(f => f.layoutGrid != alliedField.layoutGrid)) return "All field Tilemaps must share one Grid.";
            var authored = GetComponentsInChildren<BattleTileView>(true);
            if (authored.Length != width * Height) return "Fill every board cell with a floor prefab using GameObject Brush.";
            if (Fields.Sum(f => f.transform.childCount) != authored.Length) return "Use floor or decorated floor prefabs on the field layers.";
            var seen = new System.Collections.Generic.HashSet<Cell>();
            foreach (var tile in authored)
            {
                var cell = tile.Cell;
                if (cell.X < 0 || cell.Y < 0 || cell.X >= width || cell.Y >= Height || !seen.Add(cell))
                    return "The board contains an out-of-bounds or duplicate 3D tile.";
                if (tile.transform.parent != FieldFor(cell).transform) return $"Tile at {cell} is on the wrong field layer.";
                if ((tile.transform.position - Position(cell)).sqrMagnitude > 0.0001f)
                    return $"3D tile at {cell} is off the Grid. Paint at Z = 0 with the default brush anchor.";
            }
            if (Fields.Any(f => f.GetUsedTilesCount() != 0)) return "Use GameObject Brush for this 3D map.";
            return null;
        }

        public void Bind(Action<Cell> click)
        {
            var error = LayoutError();
            if (error != null) throw new InvalidOperationException(error);
            tiles = GetComponentsInChildren<BattleTileView>(true);
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
                tile.Render(view.Visibility(cell), view.Phase == MatchPhase.Placement && rules.CanPlaceBase(view.Player, cell),
                    range || ordered || selection, selection);
            }
        }
    }
}
