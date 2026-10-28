using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TurnZero.Tests
{
    public sealed class PresentationComponentTests
    {
        [Test]
        public void SessionPrefabAttachesViewsToTheirDisplayedObjects()
        {
            GameObject root = PrefabUtility.LoadPrefabContents("Assets/ProjectFiles/Prefabs/LocalBattleSession.prefab");

            try
            {
                Assert.That(root.GetComponentInChildren<LocalMatchHeaderView>(true).name, Is.EqualTo("Match header"));
                Assert.That(root.GetComponentInChildren<LocalTurnTimerView>(true).name, Is.EqualTo("Timer"));
                Assert.That(root.GetComponentInChildren<LocalSelectionView>(true).name, Is.EqualTo("Selection header"));
                Assert.That(root.GetComponentInChildren<LocalActionButtonsView>(true).name, Is.EqualTo("Action buttons"));
                Assert.That(root.GetComponentInChildren<LocalEquipmentView>(true).name, Is.EqualTo("Equipment crafting"));
                Assert.That(root.GetComponentInChildren<LocalReadyButtonView>(true).name, Is.EqualTo("Ready"));
                Assert.That(root.GetComponentInChildren<LocalWorldMarkersView>(true).name, Is.EqualTo("Field markers"));
                Assert.That(root.GetComponentInChildren<LocalBattleEffectsView>(true).name, Is.EqualTo("Battle effects"));
                Assert.That(root.GetComponentInChildren<LocalPathView>(true).GetComponent<LineRenderer>(), Is.Not.Null);

                LocalTileHighlightsView highlights = root.GetComponentInChildren<LocalTileHighlightsView>(true);

                Assert.That(highlights.GetComponent<LocalFieldMap>(), Is.Not.Null);
                AssertSerializedReferences(root);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [Test]
        public void HudAuthoringCreatesTheSameWiredComponentHierarchy()
        {
            string pipPath = GameDefinitions.AssetRoot + "Prefabs/UI/ApPip.prefab";
            byte[] pipBytes = File.ReadAllBytes(pipPath);
            SceneSetup[] previous = EditorSceneManager.GetSceneManagerSetup();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = null;

            try
            {
                root = new GameObject("HUD authoring check");
                BattleConfiguration configuration = AssetDatabase.LoadAssetAtPath<BattleConfiguration>(
                    "Assets/ProjectFiles/Data/LocalBattleConfiguration.asset");
                TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                    "Assets/External/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
                Type builderType = typeof(TurnZero.Editor.LocalPlaytestAuthoring).Assembly.GetType("TurnZero.Editor.LocalHudBuilder");
                object builder = Activator.CreateInstance(builderType, root, configuration, configuration.CreateRules(), font);
                LocalBattleHud hud = (LocalBattleHud)builderType.GetMethod("Build", BindingFlags.Public | BindingFlags.Instance)
                    .Invoke(builder, null);

                Assert.That(hud.GetComponentsInChildren<LocalTurnTimerView>(true).Length, Is.EqualTo(1));
                Assert.That(hud.GetComponentsInChildren<LocalEquipmentConfirmationView>(true).Length, Is.EqualTo(1));
                Assert.That(hud.GetComponentsInChildren<LocalRosterCard>(true).Length, Is.EqualTo(10));
                AssertSerializedReferences(root);
            }
            finally
            {
                if (root != null)
                {
                    Object.DestroyImmediate(root);
                }

                File.WriteAllBytes(pipPath, pipBytes);
                AssetDatabase.ImportAsset(pipPath);

                if (previous.Any(scene => scene.isLoaded && scene.isActive && !string.IsNullOrEmpty(scene.path)))
                {
                    EditorSceneManager.RestoreSceneManagerSetup(previous);
                }
                else
                {
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                }
            }
        }

        private static void AssertSerializedReferences(GameObject root)
        {
            foreach (MonoBehaviour component in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component.GetType().Namespace != "TurnZero")
                {
                    continue;
                }

                SerializedObject serialized = new SerializedObject(component);
                SerializedProperty property = serialized.GetIterator();

                while (property.NextVisible(true))
                {
                    if (property.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        Assert.That(property.objectReferenceValue, Is.Not.Null, component.name + " / " + property.propertyPath);
                    }
                }
            }
        }
    }
}
