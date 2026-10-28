using System.Collections.Generic;
using System.Linq;
using System;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEditor.Tilemaps;
using UnityEditor;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using UnityEngine;

namespace TurnZero.Game.Editor
{
    public static class LocalPlaytestAuthoring
    {
        public const string ScenePath = "Assets/ProjectFiles/Scenes/LocalBattle.unity";
        [MenuItem("TurnZero/Local Playtest/Open game")]
        public static void Open()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath);
        }
        [MenuItem("TurnZero/Local Playtest/Build scene and missing defaults")]
        public static void BuildMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (System.IO.File.Exists(ScenePath) && !EditorUtility.DisplayDialog("Rebuild LocalBattle scene?","This rebuilds the generated LocalBattle scene and session prefab. Edited content ScriptableObjects are preserved. Save custom scene edits first.","Rebuild","Cancel")) return;
            BuildBatch();
        }
        public static void BuildBatch() { new LocalPlaytestBuilder().Build(); Debug.Log("TurnZero local playtest authored successfully."); }
    }
}
