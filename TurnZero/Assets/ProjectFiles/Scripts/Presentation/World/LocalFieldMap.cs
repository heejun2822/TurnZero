using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TurnZero.Game
{
    public sealed class LocalFieldMap : MonoBehaviour
    {
        [SerializeField] private FieldDefinition definition;
        [SerializeField] private Tilemap alliedField, neutralField, enemyField;
        public Tilemap AlliedField => alliedField;
        public Tilemap NeutralField => neutralField;
        public Tilemap EnemyField => enemyField;
        public Tilemap[] Fields => new[] { alliedField,neutralField,enemyField };
        public LocalTileView[] Tiles => Fields.SelectMany(f => f.GetComponentsInChildren<LocalTileView>(true)).ToArray();
        public float Spacing => alliedField.layoutGrid.cellSize.x;
        public Vector3 Position(GridCell cell) => alliedField.GetCellCenterWorld(new Vector3Int(cell.X,cell.Y,0));
        public GridCell WorldToCell(Vector3 point)
        {
            var cell = alliedField.WorldToCell(point);
            return new GridCell(cell.x,cell.y);
        }
        public Tilemap FieldFor(GridCell cell) => cell.Y < definition.HomeHeight ? alliedField :
            cell.Y < definition.HomeHeight + definition.NeutralHeight ? neutralField : enemyField;
        public string LayoutError()
        {
            if (definition == null || Fields.Any(f => f == null || f.layoutGrid == null)) return "Assign a field definition and three Tilemaps.";
            if (Fields.Any(f => f.layoutGrid != alliedField.layoutGrid)) return "Field Tilemaps must share one Grid.";
            var grid = alliedField.layoutGrid;
            if (grid.cellSwizzle != GridLayout.CellSwizzle.XZY || (grid.cellSize - new Vector3(definition.TileSpacing,definition.TileSpacing,1)).sqrMagnitude > 0.0001f)
                return "Use an XZY Grid with the field definition's tile spacing.";
            int height = definition.HomeHeight * 2 + definition.NeutralHeight;
            var tiles = Tiles;
            if (tiles.Length != definition.Width * height || Fields.Sum(f => f.transform.childCount) != tiles.Length)
                return "Fill every cell with one floor prefab using GameObject Brush.";
            var cells = new HashSet<GridCell>();
            foreach (var tile in tiles)
            {
                var cell = tile.Cell;
                if (cell.X < 0 || cell.X >= definition.Width || cell.Y < 0 || cell.Y >= height || !cells.Add(cell))
                    return "The field contains duplicate or out-of-bounds tiles.";
                if (tile.transform.parent != FieldFor(cell).transform || (tile.transform.position - Position(cell)).sqrMagnitude > 0.0001f)
                    return $"Tile {cell} is off its field Grid. Paint at Z = 0.";
            }
            if (Fields.Any(f => f.GetUsedTilesCount() != 0)) return "Use GameObject Brush for 3D floor prefabs.";
            return null;
        }
    }
}
