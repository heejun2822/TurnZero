using UnityEngine;
using UnityEngine.Tilemaps;

namespace TurnZero
{
    [ExecuteAlways]
    public sealed class BattleTileView : MonoBehaviour
    {
        [SerializeField] private Renderer surface;
        [SerializeField] private GameObject border;
        [SerializeField] private Color terrainColor = new Color(0.62f, 0.58f, 0.49f);
        [SerializeField] private BattleWorldTarget target;
        private MaterialPropertyBlock tint;
        private Renderer[] geometry;
        private Color[] originalColors;
        public Cell Cell
        {
            get
            {
                var tilemap = GetComponentInParent<Tilemap>();
                if (tilemap == null) return target.Cell;
                var cell = tilemap.WorldToCell(transform.position);
                return new Cell(cell.x, cell.y);
            }
        }
        public Color TerrainColor => terrainColor;
        public void Bind(System.Action<Cell> click)
        {
            target.Cell = Cell;
            target.Click = click;
        }
        private void OnEnable()
        {
            if (!Application.IsPlaying(gameObject) && surface != null) Render(TileVisibility.Visible, false, false, false);
        }

        public void Render(TileVisibility visibility, bool placement, bool highlighted, bool selected)
        {
            if (geometry == null)
            {
                geometry = GetComponentsInChildren<Renderer>(true);
                originalColors = new Color[geometry.Length];
                for (int i = 0; i < geometry.Length; i++)
                    originalColors[i] = geometry[i].sharedMaterial.GetColor("_BaseColor");
            }
            if (tint == null) tint = new MaterialPropertyBlock();
            bool known = visibility != TileVisibility.Unexplored;
            for (int i = 0; i < geometry.Length; i++)
            {
                var renderer = geometry[i];
                renderer.enabled = known || renderer == surface;
                if (!renderer.enabled) continue;
                var color = !known ? new Color(0.045f, 0.055f, 0.07f) :
                    renderer == surface ? terrainColor : originalColors[i];
                if (visibility == TileVisibility.Explored) { color.r *= 0.3f; color.g *= 0.3f; color.b *= 0.3f; }
                if (renderer == surface && known)
                {
                    if (placement) color = new Color(0.19f, 0.5f, 0.4f);
                    if (highlighted) color = Color.Lerp(color, BattleUiStyle.Blue, selected ? 0.44f : 0.23f);
                }
                tint.SetColor("_BaseColor", color);
                renderer.SetPropertyBlock(tint);
            }
            border.SetActive(known && highlighted);
        }
    }
}
