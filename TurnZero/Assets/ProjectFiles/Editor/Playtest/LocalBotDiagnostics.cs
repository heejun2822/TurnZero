using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TurnZero.Editor
{
    public static class LocalBotDiagnostics
    {
        private static string RecordPath => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Tools/Checks/Results/last-bot-plan.json"));

        [MenuItem("TurnZero/Local Playtest/Bot/Export last decision")]
        public static void Export()
        {
            BattleSession session = UnityEngine.Object.FindFirstObjectByType<BattleSession>();

            if (session == null || session.BotPlan == null)
            {
                Debug.LogWarning("Start a bot match and enter planning before exporting a decision.");
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(RecordPath));
            File.WriteAllText(RecordPath, JsonUtility.ToJson(session.BotPlan, true));
            Debug.Log($"Bot decision exported: {RecordPath}");
        }

        [MenuItem("TurnZero/Local Playtest/Bot/Replay exported decision")]
        public static void Replay()
        {
            if (!File.Exists(RecordPath))
            {
                Debug.LogWarning("Export a bot decision first.");
                return;
            }

            BattleBotPlan saved = JsonUtility.FromJson<BattleBotPlan>(File.ReadAllText(RecordPath));

            if (saved.Version != GameDefinitions.BotVersion)
            {
                throw new InvalidOperationException("The exported decision uses a different bot version.");
            }

            BattleBotPlan replay = new BattleBotPlanner().Plan(saved.Observation, saved.Seed);

            if (saved.RulesSignature != replay.RulesSignature || saved.Decisions.Count != replay.Decisions.Count)
            {
                throw new InvalidOperationException("The replay has a different rules signature or decision count.");
            }

            for (int i = 0; i < saved.Decisions.Count; i++)
            {
                BattleBotDecision expected = saved.Decisions[i];
                BattleBotDecision actual = replay.Decisions[i];

                if (JsonUtility.ToJson(expected.Order) != JsonUtility.ToJson(actual.Order) ||
                    JsonUtility.ToJson(expected.Score) != JsonUtility.ToJson(actual.Score))
                {
                    throw new InvalidOperationException($"Bot replay differs at decision {i + 1}.");
                }
            }

            Debug.Log($"Bot replay matched: turn {saved.Observation.Turn}, P{saved.Observation.Player + 1}, seed {saved.Seed}, rules {saved.RulesSignature}.");
        }
    }
}
