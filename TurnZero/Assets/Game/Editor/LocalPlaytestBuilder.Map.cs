using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TurnZero.Game.Editor
{
    internal sealed partial class LocalPlaytestBuilder
    {
        private LocalFieldMap BuildMap(GameObject board, GameObject tilePrefab)
        {
            var grid = new GameObject("Field Grid",typeof(Grid)).GetComponent<Grid>();
            grid.transform.SetParent(board.transform,false);
            float spacing = configuration.Fields[0].TileSpacing;
            grid.transform.localPosition = new Vector3(-rules.Width*spacing/2,0,-rules.Height*spacing/2);
            grid.cellSize = new Vector3(spacing,spacing,1); grid.cellSwizzle = GridLayout.CellSwizzle.XZY;
            var allied = FieldLayer(grid,"Allied Field");
            var neutral = FieldLayer(grid,"Neutral Field");
            var enemy = FieldLayer(grid,"Enemy Field");
            var map = board.GetComponent<LocalFieldMap>();
            Wire(map,("definition",configuration.Fields[0]),("alliedField",allied),("neutralField",neutral),("enemyField",enemy));
            var brush = AssetDatabase.LoadAssetAtPath<GameObjectBrush>(LocalFieldMapAuthoring.BrushPath);
            if (brush == null)
            {
                brush = ScriptableObject.CreateInstance<GameObjectBrush>();
                AssetDatabase.CreateAsset(brush,LocalFieldMapAuthoring.BrushPath);
            }
            // Keep the shared brush empty; a temporary brush paints the authored scene and palette.
            var painter = ScriptableObject.CreateInstance<GameObjectBrush>();
            try
            {
                painter.SetGameObject(Vector3Int.zero,tilePrefab);
                for (int y = 0; y < rules.Height; y++) for (int x = 0; x < rules.Width; x++)
                {
                    var layer = map.FieldFor(new GridCell(x,y));
                    painter.Paint(layer,layer.gameObject,new Vector3Int(x,y,0));
                }
                if (AssetDatabase.LoadAssetAtPath<GameObject>(LocalFieldMapAuthoring.PalettePath) == null)
                {
                    var palette = new GameObject("Local field tiles",typeof(Grid));
                    try
                    {
                        var paletteGrid = palette.GetComponent<Grid>(); paletteGrid.cellSize = grid.cellSize; paletteGrid.cellSwizzle = grid.cellSwizzle;
                        var layer = FieldLayer(paletteGrid,"Floor prefabs");
                        painter.Paint(layer,layer.gameObject,Vector3Int.zero);
                        PrefabUtility.SaveAsPrefabAsset(palette,LocalFieldMapAuthoring.PalettePath);
                    }
                    finally { Object.DestroyImmediate(palette); }
                }
                if (AssetDatabase.LoadAssetAtPath<GridPalette>(LocalFieldMapAuthoring.PalettePath) == null)
                {
                    var settings = ScriptableObject.CreateInstance<GridPalette>();
                    settings.name = "Palette Settings"; settings.cellSizing = GridPalette.CellSizing.Manual;
                    AssetDatabase.AddObjectToAsset(settings,LocalFieldMapAuthoring.PalettePath);
                }
            }
            finally { Object.DestroyImmediate(painter); }
            string error = map.LayoutError();
            if (error != null) throw new System.InvalidOperationException(error);
            return map;
        }
        private static Tilemap FieldLayer(Grid grid, string name)
        {
            var layer = new GameObject(name,typeof(Tilemap),typeof(TilemapRenderer)).GetComponent<Tilemap>();
            layer.transform.SetParent(grid.transform,false); layer.orientation = Tilemap.Orientation.XZ;
            layer.tileAnchor = new Vector3(0.5f,0.5f,0); layer.GetComponent<TilemapRenderer>().enabled = false;
            return layer;
        }
    }

    public static class LocalFieldMapAuthoring
    {
        public const string PalettePath = "Assets/Game/Prefabs/World/FieldPalette.prefab";
        public const string BrushPath = "Assets/Game/Data/GameObjectBrush.asset";
        public static void OpenPalette(Tilemap target)
        {
            EditorApplication.ExecuteMenuItem("Window/2D/Tile Palette");
            GridPaintingState.gridBrush = AssetDatabase.LoadAssetAtPath<GameObjectBrush>(BrushPath);
            GridPaintingState.palette = AssetDatabase.LoadAssetAtPath<GameObject>(PalettePath);
            Selection.activeGameObject = target.gameObject; GridPaintingState.scenePaintTarget = target.gameObject;
        }
    }

    [CustomEditor(typeof(LocalFieldMap))]
    public sealed class LocalFieldMapEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector(); var map = (LocalFieldMap)target;
            EditorGUILayout.HelpBox("Paint 3D floor prefabs with GameObject Brush at Z = 0. Erase a floor before replacing it. Edit field colors in the FieldDefinition assets.",MessageType.Info);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
                foreach (var layer in map.Fields)
                    if (layer != null && GUILayout.Button("Paint " + layer.name)) LocalFieldMapAuthoring.OpenPalette(layer);
            string error = map.LayoutError();
            if (error != null) EditorGUILayout.HelpBox(error,MessageType.Warning);
        }
    }
}
