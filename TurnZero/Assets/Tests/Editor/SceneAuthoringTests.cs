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
                Assert.That(root.GetComponentsInChildren<BattleTileView>(true).Length, Is.EqualTo(80));
                Assert.That(root.GetComponentsInChildren<BattleActorView>(true).Length, Is.EqualTo(10));
                Assert.That(root.GetComponentsInChildren<BattleActorView>().Length, Is.EqualTo(10));
                Assert.That(root.GetComponentsInChildren<BattleLastSeenView>(true).Length, Is.EqualTo(10));
                Assert.That(root.GetComponentsInChildren<Canvas>().Length, Is.EqualTo(1));
                Assert.That(root.GetComponentsInChildren<BattleUnitCardView>().Length, Is.EqualTo(4));
                var map = root.GetComponentInChildren<BattleMapView>();
                Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(map.gameObject), Is.EqualTo("Assets/Prefabs/Map/FixedBattleMap.prefab"));
                var cells = root.GetComponentsInChildren<BattleTileView>().Select(t => t.Cell).ToArray();
                Assert.That(cells.Distinct().Count(), Is.EqualTo(80));
                Assert.That(cells.All(c => c.X >= 0 && c.X < map.Width && c.Y >= 0 && c.Y < map.Height), Is.True);
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
                Assert.That(root.GetComponentInChildren<BattleHudView>().GetComponentInChildren<TMP_Text>().font, Is.Not.Null);
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
