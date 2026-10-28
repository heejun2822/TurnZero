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
    public static class LocalFieldMapAuthoring
    {
        public static void OpenPalette(Tilemap target)
        {
            EditorApplication.ExecuteMenuItem("Window/2D/Tile Palette");
            GridPaintingState.gridBrush = AssetDatabase.LoadAssetAtPath<GameObjectBrush>(GameDefinitions.FieldBrushPath);
            GridPaintingState.palette = AssetDatabase.LoadAssetAtPath<GameObject>(GameDefinitions.FieldPalettePath);

            Selection.activeGameObject = target.gameObject;
            GridPaintingState.scenePaintTarget = target.gameObject;
        }
    }
}
