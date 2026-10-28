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
    internal static class LocalFieldMapBuilder
    {
        public static LocalFieldMap Build(GameObject board, GameObject tilePrefab, FieldDefinition field, GameRules rules)
        {
            var grid = new GameObject("Field Grid",typeof(Grid)).GetComponent<Grid>();
            grid.transform.SetParent(board.transform,false);
            float spacing = field.TileSpacing;
            grid.transform.localPosition = new Vector3(-rules.Width*spacing/2,0,-rules.Height*spacing/2);
            grid.cellSize = new Vector3(spacing,spacing,1); grid.cellSwizzle = GridLayout.CellSwizzle.XZY;
            var allied = FieldLayer(grid,"Allied Field");
            var neutral = FieldLayer(grid,"Neutral Field");
            var enemy = FieldLayer(grid,"Enemy Field");
            var map = board.GetComponent<LocalFieldMap>();
            LocalPlaytestBuilder.Wire(map,("definition",field),("alliedField",allied),("neutralField",neutral),("enemyField",enemy));
            var brush = AssetDatabase.LoadAssetAtPath<GameObjectBrush>(LocalFieldMapAuthoring.BrushPath);
            if (brush == null)
            {
                brush = ScriptableObject.CreateInstance<GameObjectBrush>();
                AssetDatabase.CreateAsset(brush,LocalFieldMapAuthoring.BrushPath);
            }
            // Keep the shared brush empty; a temporary brush paints the authored scene and palette.
            var painter = ScriptableObject.CreateInstance<GameObjectBrush>();
            try
            {
                painter.SetGameObject(Vector3Int.zero,tilePrefab);
                for (int y = 0; y < rules.Height; y++) for (int x = 0; x < rules.Width; x++)
                {
                    var layer = map.FieldFor(new GridCell(x,y));
                    painter.Paint(layer,layer.gameObject,new Vector3Int(x,y,0));
                }
                if (AssetDatabase.LoadAssetAtPath<GameObject>(LocalFieldMapAuthoring.PalettePath) == null)
                {
                    var palette = new GameObject("Local field tiles",typeof(Grid));
                    try
                    {
                        var paletteGrid = palette.GetComponent<Grid>(); paletteGrid.cellSize = grid.cellSize; paletteGrid.cellSwizzle = grid.cellSwizzle;
                        var layer = FieldLayer(paletteGrid,"Floor prefabs");
                        painter.Paint(layer,layer.gameObject,Vector3Int.zero);
                        PrefabUtility.SaveAsPrefabAsset(palette,LocalFieldMapAuthoring.PalettePath);
                    }
                    finally { UnityEngine.Object.DestroyImmediate(palette); }
                }
                if (AssetDatabase.LoadAssetAtPath<GridPalette>(LocalFieldMapAuthoring.PalettePath) == null)
                {
                    var settings = ScriptableObject.CreateInstance<GridPalette>();
                    settings.name = "Palette Settings"; settings.cellSizing = GridPalette.CellSizing.Manual;
                    AssetDatabase.AddObjectToAsset(settings,LocalFieldMapAuthoring.PalettePath);
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(painter); }
            string error = map.LayoutError();
            if (error != null) throw new System.InvalidOperationException(error);
            return map;
        }
        private static Tilemap FieldLayer(Grid grid, string name)
        {
            var layer = new GameObject(name,typeof(Tilemap),typeof(TilemapRenderer)).GetComponent<Tilemap>();
            layer.transform.SetParent(grid.transform,false); layer.orientation = Tilemap.Orientation.XZ;
            layer.tileAnchor = new Vector3(0.5f,0.5f,0); layer.GetComponent<TilemapRenderer>().enabled = false;
            return layer;
        }

    }
}
