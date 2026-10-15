using System.Collections;
using System.Linq;
using NUnit.Framework;
using TurnZero.Editor;
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

namespace TurnZero.Tests
{
    public sealed class TilemapAuthoringTests
    {
        private GameObject root;
        private BattleMapView map;
        private GameObjectBrush brush;
        [SetUp]
        public void SetUp()
        {
            root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Map/FixedBattleMap.prefab"));
            map = root.GetComponent<BattleMapView>();
            brush = ScriptableObject.CreateInstance<GameObjectBrush>();
        }
        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(brush);
            Object.DestroyImmediate(root);
            Undo.ClearAll();
        }

        private void Pick(string name)
        {
            var palette = PrefabUtility.LoadPrefabContents(BattleTilemapAssets.PalettePath);
            try
            {
                var layer = palette.GetComponentInChildren<Tilemap>();
                var sample = layer.transform.Cast<Transform>().Single(t => t.name == name);
                var cell = layer.WorldToCell(sample.position);
                brush.Pick(layer, layer.gameObject, new BoundsInt(cell, Vector3Int.one), Vector3Int.zero);
                Assert.That(brush.cells[0].gameObject, Is.Not.Null);
                Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(brush.cells[0].gameObject),
                    Is.EqualTo(BattleTilemapAssets.Folder + name + ".prefab"));
            }
            finally { PrefabUtility.UnloadPrefabContents(palette); }
        }

        [Test]
        public void TilemapCoordinatesRetainTheBoardCenterAndRoundTripUnderTransforms()
        {
            Assert.That(Vector3.Distance(map.Position(new Cell(0,0)), new Vector3(-16.5f,0,-25.3f)), Is.LessThan(0.001f));
            root.transform.SetPositionAndRotation(new Vector3(5,2,-3), Quaternion.Euler(0,90,0));
            root.transform.localScale = Vector3.one * 2;
            Assert.That(map.LayoutError(), Is.Null);
            for (int y = 0; y < map.Height; y++)
                for (int x = 0; x < map.Width; x++)
                    Assert.That(map.WorldToCell(map.Position(new Cell(x,y))), Is.EqualTo(new Cell(x,y)));
        }

        [Test]
        public void DoubledFloorMeshesCollidersAndPaletteMatchTheFieldSpacing()
        {
            Physics.SyncTransforms();
            Assert.That(map.Spacing, Is.EqualTo(2.2f));
            Assert.That(map.AlliedField.layoutGrid.cellSize, Is.EqualTo(new Vector3(2.2f, 2.2f, 1)));
            Assert.That(Vector3.Distance(map.Position(new Cell(0,0)), map.Position(new Cell(1,0))), Is.EqualTo(2.2f).Within(0.001f));
            Assert.That(Vector3.Distance(map.Position(new Cell(0,0)), map.Position(new Cell(0,1))), Is.EqualTo(2.2f).Within(0.001f));
            foreach (var tile in map.GetComponentsInChildren<BattleTileView>())
            {
                var floor = tile.transform.Find("Surface");
                Assert.That(floor.GetComponent<Renderer>().bounds.size.x, Is.EqualTo(2).Within(0.001f));
                Assert.That(floor.GetComponent<Renderer>().bounds.size.z, Is.EqualTo(2).Within(0.001f));
                Assert.That(floor.GetComponent<BoxCollider>().bounds.size.x, Is.EqualTo(2).Within(0.001f));
                Assert.That(floor.GetComponent<BoxCollider>().bounds.size.z, Is.EqualTo(2).Within(0.001f));
                Assert.That(floor.localScale.y, Is.EqualTo(0.16f));
                Assert.That(tile.transform.Find("Selection Border").localScale, Is.EqualTo(new Vector3(2.14f, 0.08f, 2.14f)));
            }
            var palette = AssetDatabase.LoadAssetAtPath<GameObject>(BattleTilemapAssets.PalettePath);
            Assert.That(palette.GetComponent<Grid>().cellSize, Is.EqualTo(new Vector3(2.2f, 2.2f, 1)));
            foreach (var name in new[] { "StoneLight", "StoneDark", "Grass", "TreeTile", "RuinedWallTile" })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BattleTilemapAssets.Folder + name + ".prefab");
                Assert.That(prefab.transform.Find("Surface").localScale, Is.EqualTo(new Vector3(2, 0.16f, 2)), name);
            }
            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Map/BattleTile.prefab")
                .transform.Find("Surface").localScale, Is.EqualTo(new Vector3(2, 0.16f, 2)));
        }

        [UnityTest]
        public IEnumerator NativeGameObjectBrushPaintEraseAndUndoDoNotDuplicateFloors()
        {
            var cell = new Vector3Int(0,1,0);
            Pick("Grass");
            brush.Paint(map.AlliedField, map.AlliedField.gameObject, cell);
            Assert.That(map.AlliedField.transform.childCount, Is.EqualTo(160), "Occupied cells are not overwritten.");
            Undo.IncrementCurrentGroup();
            brush.Erase(map.AlliedField, map.AlliedField.gameObject, cell);
            yield return null;
            Assert.That(map.AlliedField.transform.childCount, Is.EqualTo(159));
            Assert.That(map.LayoutError(), Is.Not.Null);
            Assert.Throws<System.InvalidOperationException>(() => map.Bind(_ => {}));
            Undo.FlushUndoRecordObjects();
            Undo.PerformUndo();
            yield return null;
            Assert.That(map.LayoutError(), Is.Null);
            Undo.PerformRedo();
            yield return null;
            Assert.That(map.AlliedField.transform.childCount, Is.EqualTo(159));
            Undo.IncrementCurrentGroup();
            brush.Paint(map.AlliedField, map.AlliedField.gameObject, cell);
            brush.Paint(map.AlliedField, map.AlliedField.gameObject, cell);
            yield return null;
            Assert.That(map.LayoutError(), Is.Null);
            Assert.That(map.AlliedField.transform.childCount, Is.EqualTo(160));
            Assert.That(map.AlliedField.GetComponentsInChildren<BattleTileView>().Single(t => t.Cell == new Cell(0,1)).TerrainColor,
                Is.EqualTo(AssetDatabase.LoadAssetAtPath<GameObject>(BattleTilemapAssets.Folder + "Grass.prefab").GetComponent<BattleTileView>().TerrainColor));
            Undo.FlushUndoRecordObjects();
            Undo.PerformUndo();
            yield return null;
            Assert.That(map.AlliedField.transform.childCount, Is.EqualTo(159));
            Undo.PerformRedo();
            yield return null;
            Assert.That(map.LayoutError(), Is.Null);
        }

        [Test]
        public void DecoratedNeutralTilesUseTheSameLayerWithoutDuplicatingTheFloor()
        {
            var palette = AssetDatabase.LoadAssetAtPath<GameObject>(BattleTilemapAssets.PalettePath);
            Assert.That(palette.GetComponentInChildren<Tilemap>().transform.childCount, Is.EqualTo(5));
            Assert.That(palette.GetComponent<Grid>().cellSwizzle, Is.EqualTo(GridLayout.CellSwizzle.XZY));
            var cell = new Vector3Int(7,10,0);
            Pick("TreeTile");
            brush.Erase(map.NeutralField, map.NeutralField.gameObject, cell);
            brush.Paint(map.NeutralField, map.NeutralField.gameObject, cell);
            brush.Paint(map.NeutralField, map.NeutralField.gameObject, cell);
            Assert.That(map.NeutralField.transform.childCount, Is.EqualTo(64));
            Assert.That(map.GetComponentsInChildren<BattleTileView>().Length, Is.EqualTo(384));
            Assert.That(map.LayoutError(), Is.Null);
            var decorated = map.NeutralField.GetComponentsInChildren<BattleTileView>().Single(t => t.Cell == new Cell(7,10));
            Assert.That(decorated.transform.Find("Tree"), Is.Not.Null);
            brush.Erase(map.NeutralField, map.NeutralField.gameObject, cell);
            Pick("RuinedWallTile");
            brush.Paint(map.NeutralField, map.NeutralField.gameObject, cell);
            Assert.That(map.NeutralField.transform.childCount, Is.EqualTo(64));
            Assert.That(map.LayoutError(), Is.Null);
        }

        [Test]
        public void PaletteMenuSelectsPackagedGameObjectBrushAndTerrainTarget()
        {
            var previousBrush = GridPaintingState.gridBrush;
            var previousPalette = GridPaintingState.palette;
            var previousTarget = GridPaintingState.scenePaintTarget;
            var previousSelection = Selection.activeGameObject;
            try
            {
                BattleTilemapAssets.OpenPalette(map.AlliedField);
                Assert.That(GridPaintingState.gridBrush.GetType(), Is.EqualTo(typeof(GameObjectBrush)));
                Assert.That(GridPaintingState.palette, Is.EqualTo(AssetDatabase.LoadAssetAtPath<GameObject>(BattleTilemapAssets.PalettePath)));
                Assert.That(GridPaintingState.scenePaintTarget, Is.EqualTo(map.AlliedField.gameObject));
                foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>().Where(w => w.GetType().Name == "GridPaintPaletteWindow")) window.Close();
            }
            finally
            {
                GridPaintingState.gridBrush = previousBrush;
                if (previousPalette != null) GridPaintingState.palette = previousPalette;
                GridPaintingState.scenePaintTarget = previousTarget;
                Selection.activeGameObject = previousSelection;
            }
        }

        [Test]
        public void EditedBrushPrefabsSurvivePrefabSaveAndReload()
        {
            const string path = "Assets/Tests/Editor/TilemapRoundTrip.prefab";
            try
            {
                Pick("Grass");
                var cell = new Vector3Int(0,1,0);
                brush.Erase(map.AlliedField, map.AlliedField.gameObject, cell);
                brush.Paint(map.AlliedField, map.AlliedField.gameObject, cell);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                var reloaded = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var copy = reloaded.GetComponent<BattleMapView>();
                    Assert.That(copy.LayoutError(), Is.Null);
                    var tile = copy.AlliedField.GetComponentsInChildren<BattleTileView>().Single(t => t.Cell == new Cell(0,1));
                    Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(tile.gameObject), Is.EqualTo(BattleTilemapAssets.Folder + "Grass.prefab"));
                }
                finally { PrefabUtility.UnloadPrefabContents(reloaded); }
            }
            finally { AssetDatabase.DeleteAsset(path); }
        }
    }
}
