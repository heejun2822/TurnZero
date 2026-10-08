using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TurnZero.Editor
{
    public static class PrototypeTools
    {
        [MenuItem("TurnZero/Open Battle Prototype")]
        public static void OpenPrototype()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene("Assets/Scenes/Battle.unity");
        }

        [MenuItem("TurnZero/Verify Debug Recording")]
        public static void VerifyRecording()
        {
            string path = EditorUtility.OpenFilePanel("Select a TurnZero debug replay", Application.persistentDataPath, "json");
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                var recording = JsonUtility.FromJson<BattleRecording>(File.ReadAllText(path));
                if (recording == null || recording.FormatVersion != 1 || recording.Rules == null)
                    throw new ArgumentException("Unsupported recording format.");
                foreach (var turn in recording.Turns)
                {
                    var actual = BattleResolver.Resolve(turn.Before, recording.Rules, turn.PlayerOneOrders, turn.PlayerTwoOrders).State;
                    if (StateKey(actual) != StateKey(turn.After)) throw new InvalidOperationException($"Turn {turn.Before.Turn} does not reproduce.");
                }
                Debug.Log($"TurnZero recording verified: {recording.Turns.Count} turns reproduce exactly.");
            }
            catch (Exception exception) { Debug.LogException(exception); }
        }

        private static string StateKey(BattleState state) => $"{state.Turn}/{state.Phase}/{state.Outcome}/{string.Join(",", state.AP)}:" +
            string.Join(";", state.Entities.OrderBy(e => e.Id).Select(e => $"{e.Id}/{e.Owner}/{e.Kind}/{e.Position.X},{e.Position.Y}/{e.Health}/{e.MaximumHealth}"));
    }
}
