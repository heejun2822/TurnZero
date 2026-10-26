using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace TurnZero.Editor
{
    public static class BattleTilemapAssets
    {
        public const string Folder = "Assets/ProjectFiles/Prototype/Tiles/Prototype/";
        public const string PalettePath = Folder + "TurnZero 3D.prefab";
        public const string BrushPath = Folder + "GameObject Brush.asset";
        private const string MapPath = "Assets/ProjectFiles/Prototype/Prefabs/Map/FixedBattleMap.prefab";
        private static readonly string[] Names = { "StoneLight", "StoneDark", "Grass", "TreeTile", "RuinedWallTile" };

        [MenuItem("TurnZero/Open 3D Tile Palette")]
        public static void OpenPalette()
        {
            var map = Selection.activeGameObject == null ? null : Selection.activeGameObject.GetComponentInParent<BattleMapView>();
            var layer = Selection.activeGameObject == null ? null : Selection.activeGameObject.GetComponentInParent<Tilemap>();
            OpenPalette(layer != null ? layer : map == null ? null : map.AlliedField);
        }
        public static void OpenPalette(Tilemap target)
        {
            EditorApplication.ExecuteMenuItem("Window/2D/Tile Palette");
            GridPaintingState.gridBrush = AssetDatabase.LoadAssetAtPath<GameObjectBrush>(BrushPath);
            GridPaintingState.palette = AssetDatabase.LoadAssetAtPath<GameObject>(PalettePath);
            if (target != null) { Selection.activeGameObject = target.gameObject; GridPaintingState.scenePaintTarget = target.gameObject; }
        }

        public static void MigrateBatch()
        {
            var root = PrefabUtility.LoadPrefabContents(MapPath);
            try { BuildMap(root.GetComponent<BattleMapView>()); PrefabUtility.SaveAsPrefabAsset(root, MapPath); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            var session = PrefabUtility.LoadPrefabContents("Assets/ProjectFiles/Prototype/Prefabs/BattleSession.prefab");
            try
            {
                var map = session.GetComponentInChildren<BattleMapView>();
                var rules = new SerializedObject(session.GetComponent<BattleController>()).FindProperty("rules");
                rules.FindPropertyRelative("Width").intValue = map.Width;
                rules.FindPropertyRelative("Height").intValue = map.Height;
                rules.FindPropertyRelative("NeutralHeight").intValue = map.NeutralHeight;
                rules.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                var cells = new[] { new Cell(7,4), new Cell(8,3), new Cell(7,2), new Cell(6,3), new Cell(7,3) };
                foreach (var actor in session.GetComponentsInChildren<BattleActorView>(true))
                {
                    int id = actor.EntityId, owner = id == 102 || id >= 5 && id <= 8 ? 1 : 0;
                    var cell = cells[id > 100 ? 4 : id - owner * 4 - 1];
                    if (owner == 1) cell = new Cell(map.Width - 1 - cell.X, map.Height - 1 - cell.Y);
                    actor.transform.position = map.Position(cell);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(actor.transform);
                }
                session.GetComponentInChildren<BattleCameraRig>().Reframe();
                PrefabUtility.SaveAsPrefabAsset(session, "Assets/ProjectFiles/Prototype/Prefabs/BattleSession.prefab");
            }
            finally { PrefabUtility.UnloadPrefabContents(session); }
            AssetDatabase.SaveAssets();
            var scene = EditorSceneManager.OpenScene("Assets/ProjectFiles/Prototype/Scenes/Battle.unity");
            var field = Object.FindFirstObjectByType<BattleMapView>();
            if (field.LayoutError() != null) throw new InvalidOperationException(field.LayoutError());
            Object.FindFirstObjectByType<BattleCameraRig>().Reframe();
            EditorSceneManager.SaveScene(scene);
            Debug.Log("TurnZero: 16x24 field saved as Allied 16x10 / Neutral 16x4 / Enemy 16x10. Exterior removed.");
        }

        public static void BuildMap(BattleMapView map)
        {
            foreach (var child in map.transform.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);
            var serialized = new SerializedObject(map);
            serialized.FindProperty("width").intValue = 16;
            serialized.FindProperty("homeHeight").intValue = 10;
            serialized.FindProperty("neutralHeight").intValue = 4;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            BattleMapAuthoring.CreateGrid(map, 2.2f);
            CreateDecoratedTile("TreeTile", "Grass", "Tree");
            CreateDecoratedTile("RuinedWallTile", "StoneDark", "RuinedWall");
            var prefabs = Names.Select(n => AssetDatabase.LoadAssetAtPath<GameObject>(Folder + n + ".prefab")).ToArray();
            if (prefabs.Any(p => p == null)) throw new InvalidOperationException("Missing field palette prefab.");
            var brush = ScriptableObject.CreateInstance<GameObjectBrush>();
            try
            {
                for (int y = 0; y < map.Height; y++)
                    for (int x = 0; x < map.Width; x++)
                    {
                        bool upper = y >= map.HomeHeight + map.NeutralHeight;
                        int sx = upper ? map.Width - 1 - x : x, sy = upper ? map.Height - 1 - y : y;
                        bool neutral = y >= map.HomeHeight && y < map.HomeHeight + map.NeutralHeight;
                        int type = neutral ? (x == 7 || x == 8 ? 0 : 2) : (sx * 13 + sy * 7) % 5 < 2 ? 2 : (sx + sy) % 2;
                        if (neutral)
                        {
                            if (y >= 12) { sx = map.Width - 1 - x; sy = map.Height - 1 - y; }
                            if (sx == 3 && sy == 10 || sx == 5 && sy == 11) type = 3;
                            if (sx == 4 && sy == 10 || sx == 6 && sy == 11) type = 4;
                        }
                        Paint(brush, map.FieldFor(new Cell(x,y)), new Vector3Int(x,y,0), prefabs[type]);
                    }
            }
            finally { Object.DestroyImmediate(brush); }
            CreatePalette(prefabs);
            if (map.LayoutError() != null) throw new InvalidOperationException(map.LayoutError());
        }

        private static void CreateDecoratedTile(string name, string floor, string prop)
        {
            var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Folder + floor + ".prefab"));
            try
            {
                PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                root.name = name;
                PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Folder + prop + ".prefab"), root.transform);
                PrefabUtility.SaveAsPrefabAsset(root, Folder + name + ".prefab");
            }
            finally { Object.DestroyImmediate(root); }
        }
        private static void CreatePalette(GameObject[] prefabs)
        {
            var palette = PrefabUtility.LoadPrefabContents(PalettePath);
            var brush = ScriptableObject.CreateInstance<GameObjectBrush>();
            try
            {
                var layer = palette.GetComponentInChildren<Tilemap>();
                foreach (var child in layer.transform.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);
                for (int i = 0; i < prefabs.Length; i++) Paint(brush, layer, new Vector3Int(i % 4, -(i / 4), 0), prefabs[i]);
                PrefabUtility.SaveAsPrefabAsset(palette, PalettePath);
            }
            finally { Object.DestroyImmediate(brush); PrefabUtility.UnloadPrefabContents(palette); }
        }
        private static void Paint(GameObjectBrush brush, Tilemap layer, Vector3Int cell, GameObject prefab)
        {
            brush.SetGameObject(Vector3Int.zero, prefab);
            brush.Paint(layer, layer.gameObject, cell);
        }
    }
}
