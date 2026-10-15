using System.Linq;
using TMPro;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TurnZero.Tests
{
    public sealed class SceneAuthoringTests
    {
        [Test]
        public void EverySceneAndPrefabUsesTmpWithSavedFontResources()
        {
            var previous = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (var path in AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/") && (p.EndsWith(".prefab") || p.EndsWith(".unity"))))
                {
                    GameObject prefab = null;
                    var roots = path.EndsWith(".unity") ? EditorSceneManager.OpenScene(path).GetRootGameObjects() :
                        new[] { prefab = PrefabUtility.LoadPrefabContents(path) };
                    try
                    {
                        foreach (var root in roots)
                        {
                            Assert.That(root.GetComponentsInChildren<Text>(true), Is.Empty, path);
                            Assert.That(root.GetComponentsInChildren<TextMesh>(true), Is.Empty, path);
                            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
                            {
                                Assert.That(AssetDatabase.Contains(text.font), Is.True, path + ": " + text.name);
                                Assert.That(AssetDatabase.Contains(text.fontSharedMaterial), Is.True, path + ": " + text.name);
                            }
                        }
                    }
                    finally { if (prefab != null) PrefabUtility.UnloadPrefabContents(prefab); }
                }
            }
            finally
            {
                if (previous.Any(s => s.isLoaded && s.isActive)) EditorSceneManager.RestoreSceneManagerSetup(previous);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        [Test]
        public void BattleSceneHasAuthoredMapActorsHudAndNestedPrefabsBeforePlay()
        {
            var previous = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene = EditorSceneManager.OpenScene("Assets/Scenes/Battle.unity");
                var root = scene.GetRootGameObjects().Single(g => g.name == "Battle Session");
                Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root), Is.EqualTo("Assets/Prefabs/BattleSession.prefab"));
                Assert.That(root.GetComponentsInChildren<BattleTileView>(true).Length, Is.EqualTo(384));
                Assert.That(root.GetComponentsInChildren<BattleActorView>(true).Length, Is.EqualTo(10));
                Assert.That(root.GetComponentsInChildren<BattleActorView>().Length, Is.EqualTo(10));
                Assert.That(root.GetComponentsInChildren<BattleLastSeenView>(true).Length, Is.EqualTo(10));
                Assert.That(root.GetComponentsInChildren<Canvas>().Length, Is.EqualTo(1));
                Assert.That(root.GetComponentsInChildren<BattleUnitCardView>().Length, Is.EqualTo(4));
                var map = root.GetComponentInChildren<BattleMapView>();
                Assert.That(map.AlliedField, Is.Not.Null);
                Assert.That(map.AlliedField.layoutGrid.cellSwizzle, Is.EqualTo(GridLayout.CellSwizzle.XZY));
                Assert.That(map.LayoutError(), Is.Null);
                Assert.That(map.Width, Is.EqualTo(16));
                Assert.That(map.Height, Is.EqualTo(24));
                Assert.That(map.GetComponentsInChildren<UnityEngine.Tilemaps.Tilemap>().Length, Is.EqualTo(3));
                Assert.That(map.AlliedField.transform.childCount, Is.EqualTo(160));
                Assert.That(map.NeutralField.transform.childCount, Is.EqualTo(64));
                Assert.That(map.EnemyField.transform.childCount, Is.EqualTo(160));
                Assert.That(map.transform.childCount, Is.EqualTo(1));
                Assert.That(map.GetComponentsInChildren<MeshRenderer>().All(r => r.GetComponentInParent<BattleTileView>() != null), Is.True, "Only field tiles and their decorations remain.");
                Assert.That(map.NeutralField.transform.Cast<Transform>().Count(t => t.name == "TreeTile"), Is.EqualTo(4));
                Assert.That(map.NeutralField.transform.Cast<Transform>().Count(t => t.name == "RuinedWallTile"), Is.EqualTo(4));
                Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(map.gameObject), Is.EqualTo("Assets/Prefabs/Map/FixedBattleMap.prefab"));
                var cells = root.GetComponentsInChildren<BattleTileView>().Select(t => t.Cell).ToArray();
                Assert.That(cells.Distinct().Count(), Is.EqualTo(384));
                Assert.That(cells.All(c => c.X >= 0 && c.X < map.Width && c.Y >= 0 && c.Y < map.Height), Is.True);
                foreach (var tile in root.GetComponentsInChildren<BattleTileView>())
                {
                    Assert.That(map.WorldToCell(tile.transform.position), Is.EqualTo(tile.Cell));
                    Assert.That(Vector3.Distance(map.Position(tile.Cell), tile.transform.position), Is.LessThan(0.001f));
                    Assert.That(PrefabUtility.IsPartOfPrefabInstance(tile), Is.True);
                    Assert.That(tile.transform.parent, Is.EqualTo(map.FieldFor(tile.Cell).transform));
                }
                foreach (var child in root.GetComponentsInChildren<Transform>(true))
                    Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject), Is.Zero, child.name);
                foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true).Where(c => c.GetType().Namespace == "TurnZero"))
                {
                    var serialized = new SerializedObject(component);
                    var property = serialized.GetIterator();
                    while (property.NextVisible(true))
                        if (property.propertyType == SerializedPropertyType.ObjectReference)
                            Assert.That(property.objectReferenceValue, Is.Not.Null, component.name + ": " + property.propertyPath);
                }
                foreach (var card in root.GetComponentsInChildren<BattleUnitCardView>())
                {
                    Assert.That(AssetDatabase.Contains(card.Portrait), Is.True);
                    Assert.That(card.Portrait.texture, Is.Not.Null);
                    Assert.That(PrefabUtility.IsPartOfPrefabInstance(card), Is.True);
                }
                Assert.That(root.GetComponentsInChildren<Text>(true), Is.Empty);
                Assert.That(root.GetComponentsInChildren<TextMesh>(true), Is.Empty);
                Assert.That(root.GetComponentsInChildren<TextMeshProUGUI>(true).Length, Is.GreaterThan(40));
                Assert.That(root.GetComponentsInChildren<TextMeshPro>(true).Length, Is.EqualTo(20));
                foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    Assert.That(AssetDatabase.Contains(text.font), Is.True, text.name);
                    Assert.That(AssetDatabase.Contains(text.fontSharedMaterial), Is.True, text.name);
                }
                Assert.That(EditorApplication.isPlaying, Is.False);
            }
            finally
            {
                if (previous.Any(s => s.isLoaded && s.isActive)) EditorSceneManager.RestoreSceneManagerSetup(previous);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }
    }
}
