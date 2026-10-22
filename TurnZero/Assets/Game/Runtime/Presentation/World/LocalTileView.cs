using UnityEngine;
using UnityEngine.Tilemaps;

namespace TurnZero.Game
{
    public sealed class LocalTileView : MonoBehaviour
    {
        [SerializeField] private Renderer surface, outline;
        private MaterialPropertyBlock block;
        public GridCell Cell
        {
            get
            {
                var tilemap = GetComponentInParent<Tilemap>();
                var cell = tilemap.WorldToCell(transform.position);
                return new GridCell(cell.x,cell.y);
            }
        }
        public void Render(Color ground, Sight sight, Color? highlight)
        {
            if (block == null) block = new MaterialPropertyBlock();
            var color = sight == Sight.Unknown ? new Color(0.024f,0.033f,0.047f) : ground * (sight == Sight.Explored ? 0.32f : 1);
            color.a = 1; block.SetColor("_BaseColor",color); surface.SetPropertyBlock(block);
            outline.gameObject.SetActive(highlight.HasValue);
            if (highlight.HasValue) { block.SetColor("_BaseColor",highlight.Value); outline.SetPropertyBlock(block); }
        }
    }
}
