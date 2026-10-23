using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TurnZero.Editor
{
    public static class BattleMapAuthoring
    {
        public static void CreateGrid(BattleMapView map, float spacing)
        {
            var grid = new GameObject("Field Grid", typeof(Grid)).GetComponent<Grid>();
            grid.transform.SetParent(map.transform, false);
            grid.transform.localPosition = new Vector3(-map.Width * spacing * 0.5f, 0, -map.Height * spacing * 0.5f);
            grid.cellSize = new Vector3(spacing, spacing, 1);
            grid.cellSwizzle = GridLayout.CellSwizzle.XZY;
            var serialized = new SerializedObject(map);
            serialized.FindProperty("alliedField").objectReferenceValue = CreateLayer(grid, "Allied Field");
            serialized.FindProperty("neutralField").objectReferenceValue = CreateLayer(grid, "Neutral Field");
            serialized.FindProperty("enemyField").objectReferenceValue = CreateLayer(grid, "Enemy Field");
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Tilemap CreateLayer(Grid grid, string name)
        {
            var layer = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer)).GetComponent<Tilemap>();
            layer.transform.SetParent(grid.transform, false);
            layer.orientation = Tilemap.Orientation.XZ;
            layer.tileAnchor = new Vector3(0.5f, 0.5f, 0);
            layer.GetComponent<TilemapRenderer>().enabled = false;
            return layer;
        }
    }

    [CustomEditor(typeof(BattleMapView))]
    public sealed class BattleMapViewEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var map = (BattleMapView)target;
            EditorGUILayout.HelpBox("GameObject Brush: X 0-15, Allied Y 0-9, Neutral Y 10-13, Enemy Y 14-23. Erase before replacing a floor. TreeTile and RuinedWallTile include the floor.", MessageType.Info);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || map.AlliedField == null))
            {
                if (GUILayout.Button("Paint Allied Field")) BattleTilemapAssets.OpenPalette(map.AlliedField);
                if (GUILayout.Button("Paint Neutral Field")) BattleTilemapAssets.OpenPalette(map.NeutralField);
                if (GUILayout.Button("Paint Enemy Field")) BattleTilemapAssets.OpenPalette(map.EnemyField);
            }
            var error = map.LayoutError();
            if (error != null) EditorGUILayout.HelpBox(error, MessageType.Warning);
        }
    }
}
