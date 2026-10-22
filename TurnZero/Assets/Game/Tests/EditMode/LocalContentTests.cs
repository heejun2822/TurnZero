using System.Linq;
using NUnit.Framework;
using TMPro;
using TurnZero.Game.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace TurnZero.Game.Tests
{
    public sealed class LocalContentTests
    {
        private const string ConfigPath = "Assets/Game/Data/LocalBattleConfiguration.asset";
        [Test] public void AuthoredContentMatchesDevelopmentPlanAndCopiesRulesWithoutMutatingAssets()
        {
            var config = AssetDatabase.LoadAssetAtPath<BattleConfiguration>(ConfigPath); Assert.That(config,Is.Not.Null);
            var rules = config.CreateRules();
            Assert.That(rules.Width,Is.EqualTo(12)); Assert.That(rules.Height,Is.EqualTo(20));
            CollectionAssert.AreEqual(new[] { 14,7,10,9,6 },rules.Characters.Select(c => c.Health));
            CollectionAssert.AreEqual(new[] { 3,6,4,4,2 },rules.Characters.Select(c => c.Move.Range));
            CollectionAssert.AreEqual(new[] { 3,4,5,5,8 },rules.Characters.Select(c => c.Attack.Range));
            CollectionAssert.AreEqual(new[] { 4,3,5,5,4 },rules.Characters.Select(c => c.Skill.Cost));
            Assert.That(rules.BaseHealth,Is.EqualTo(30)); Assert.That(rules.BaseVision,Is.EqualTo(3));
            Assert.That(config.Fields.Length,Is.EqualTo(3)); Assert.That(config.Equipment.Length,Is.EqualTo(3));
            Assert.That(config.Fields.All(f => Mathf.Approximately(f.TileSpacing,2.2f)),Is.True);
            rules.Characters[0].Health = 999; Assert.That(config.Characters[0].Values.Health,Is.EqualTo(14));
            Assert.That(config.Characters.All(c => c.Model != null && c.Portrait != null),Is.True);
            Assert.That(config.LocalProfileNotes,Does.Contain("provisional"));
        }
        [Test] public void LocalSceneUsesComposedServicesFiveCardsPerTeamAndTwentyFivePips()
        {
            var previous = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene = EditorSceneManager.OpenScene("Assets/Game/Scenes/LocalBattle.unity");
                var root = scene.GetRootGameObjects().Single(g => g.name == "Local Battle");
                Assert.That(root.GetComponent<BattleBootstrap>(),Is.Not.Null);
                Assert.That(root.GetComponent<BattleSession>(),Is.Not.Null);
                Assert.That(root.GetComponentsInChildren<LocalTileView>(true).Length,Is.EqualTo(240));
                var map = root.GetComponentInChildren<LocalFieldMap>(); Assert.That(map.LayoutError(),Is.Null);
                CollectionAssert.AreEqual(new[] { 96,48,96 },map.Fields.Select(f => f.transform.childCount));
                Assert.That(map.GetComponentInChildren<Grid>().cellSize,Is.EqualTo(new Vector3(2.2f,2.2f,1)));
                Assert.That(root.GetComponentsInChildren<Tilemap>(true).Length,Is.EqualTo(3));
                foreach (var tile in map.Tiles)
                    Assert.That(tile.transform.Find("Surface").localScale,Is.EqualTo(new Vector3(2,0.16f,2)));
                Assert.That(root.GetComponentInChildren<Canvas>().renderMode,Is.EqualTo(RenderMode.ScreenSpaceOverlay));
                Assert.That(root.GetComponentsInChildren<LocalRosterCard>(true).Length,Is.EqualTo(10));
                Assert.That(root.GetComponentsInChildren<Image>(true).Count(i => i.name.StartsWith("AP slot ")),Is.EqualTo(25));
                Assert.That(root.GetComponentsInChildren<LocalActorView>(true).Length,Is.EqualTo(22));
                foreach (var child in root.GetComponentsInChildren<Transform>(true)) Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject),Is.Zero,child.name);
                foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true).Where(c => c.GetType().Namespace == "TurnZero.Game"))
                {
                    var serialized = new SerializedObject(component); var iterator = serialized.GetIterator();
                    while (iterator.NextVisible(true)) if (iterator.propertyType == SerializedPropertyType.ObjectReference)
                        Assert.That(iterator.objectReferenceValue,Is.Not.Null,component.name+" / "+iterator.propertyPath);
                }
                Assert.That(root.GetComponentsInChildren<Text>(true),Is.Empty);
                Assert.That(root.GetComponentsInChildren<TMP_Text>(true).All(t => t.font != null),Is.True);
                Assert.That(EditorBuildSettings.scenes.First(s => s.enabled).path,Is.EqualTo("Assets/Game/Scenes/LocalBattle.unity"));
            }
            finally
            {
                if (previous.Any(s => s.isLoaded && s.isActive)) EditorSceneManager.RestoreSceneManagerSetup(previous);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            }
        }
        [Test] public void NativeGameObjectBrushEditsRetainCellCoordinatesAndPrefabReferences()
        {
            const string path = "Assets/Game/Tests/EditMode/LocalFieldRoundTrip.prefab";
            var root = PrefabUtility.LoadPrefabContents("Assets/Game/Prefabs/LocalBattleSession.prefab");
            var brush = ScriptableObject.CreateInstance<GameObjectBrush>();
            try
            {
                var map = root.GetComponentInChildren<LocalFieldMap>(); var layer = map.AlliedField;
                var cell = new Vector3Int(0,1,0); var gridCell = new GridCell(0,1);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/World/Tile.prefab");
                brush.SetGameObject(Vector3Int.zero,prefab);
                brush.Erase(layer,layer.gameObject,cell); Assert.That(map.LayoutError(),Is.Not.Null);
                brush.Paint(layer,layer.gameObject,cell); brush.Paint(layer,layer.gameObject,cell);
                Assert.That(map.LayoutError(),Is.Null); Assert.That(layer.transform.childCount,Is.EqualTo(96));
                var painted = layer.GetComponentsInChildren<LocalTileView>().Single(t => t.Cell == gridCell);
                Assert.That(Vector3.Distance(painted.transform.position,map.Position(gridCell)),Is.LessThan(0.001f));
                Assert.That(painted.transform.Find("Surface").GetComponent<BoxCollider>().bounds.size.x,Is.EqualTo(2).Within(0.001f));
                PrefabUtility.SaveAsPrefabAsset(root,path);
                var reloaded = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var copy = reloaded.GetComponentInChildren<LocalFieldMap>(); Assert.That(copy.LayoutError(),Is.Null);
                    var tile = copy.Tiles.Single(t => t.Cell == gridCell);
                    Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(tile.gameObject),Is.EqualTo("Assets/Game/Prefabs/World/Tile.prefab"));
                }
                finally { PrefabUtility.UnloadPrefabContents(reloaded); }
                Assert.That(AssetDatabase.LoadAssetAtPath<GameObjectBrush>(LocalFieldMapAuthoring.BrushPath),Is.Not.Null);
                Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(LocalFieldMapAuthoring.PalettePath).GetComponentInChildren<LocalTileView>(),Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(brush); PrefabUtility.UnloadPrefabContents(root); AssetDatabase.DeleteAsset(path);
            }
        }
        [Test] public void TilePaletteOpensWithTheNativeBrushAndSelectedField()
        {
            var previousBrush = GridPaintingState.gridBrush; var previousPalette = GridPaintingState.palette;
            var previousTarget = GridPaintingState.scenePaintTarget; var previousSelection = Selection.activeGameObject;
            var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/LocalBattleSession.prefab"));
            try
            {
                var layer = root.GetComponentInChildren<LocalFieldMap>().NeutralField;
                LocalFieldMapAuthoring.OpenPalette(layer);
                Assert.That(GridPaintingState.gridBrush.GetType(),Is.EqualTo(typeof(GameObjectBrush)));
                Assert.That(GridPaintingState.scenePaintTarget,Is.EqualTo(layer.gameObject));
                Assert.That(GridPaintingState.palette,Is.EqualTo(AssetDatabase.LoadAssetAtPath<GameObject>(LocalFieldMapAuthoring.PalettePath)));
                Assert.That(AssetDatabase.LoadAssetAtPath<GridPalette>(LocalFieldMapAuthoring.PalettePath).cellSizing,Is.EqualTo(GridPalette.CellSizing.Manual));
            }
            finally
            {
                foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>().Where(w => w.GetType().Name == "GridPaintPaletteWindow")) window.Close();
                GridPaintingState.gridBrush = previousBrush;
                if (previousPalette != null) GridPaintingState.palette = previousPalette;
                GridPaintingState.scenePaintTarget = previousTarget; Selection.activeGameObject = previousSelection;
                Object.DestroyImmediate(root);
            }
        }
    }
}
