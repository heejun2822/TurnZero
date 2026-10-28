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

namespace TurnZero.Editor
{
    [CustomEditor(typeof(LocalFieldMap))]
    public sealed class LocalFieldMapEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            LocalFieldMap map = (LocalFieldMap)target;

            EditorGUILayout.HelpBox("Paint 3D floor prefabs with GameObject Brush at Z = 0. Erase a floor before replacing it. Edit field colors in the FieldDefinition assets.", MessageType.Info);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                foreach (Tilemap layer in map.Fields)
                {
                    if (layer != null && GUILayout.Button("Paint " + layer.name))
                    {
                        LocalFieldMapAuthoring.OpenPalette(layer);
                    }
                }
            }

            string error = map.LayoutError();

            if (error != null)
            {
                EditorGUILayout.HelpBox(error, MessageType.Warning);
            }
        }
    }
}
