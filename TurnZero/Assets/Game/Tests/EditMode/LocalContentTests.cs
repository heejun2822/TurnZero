using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
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
    }
}
