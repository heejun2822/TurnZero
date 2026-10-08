using UnityEngine;

namespace TurnZero
{
    public sealed class BattleTileView : MonoBehaviour
    {
        [SerializeField] private Renderer surface;
        [SerializeField] private GameObject border;
        [SerializeField] private Color terrainColor;
        [SerializeField] private BattleWorldTarget target;
        private MaterialPropertyBlock tint;
        public Cell Cell => target.Cell;

        public void Bind(System.Action<Cell> click) => target.Click = click;

        public void Render(bool visible, bool placement, bool highlighted, bool selected)
        {
            var color = placement ? new Color(0.19f, 0.5f, 0.4f) : visible ? terrainColor : new Color(0.065f, 0.075f, 0.083f);
            if (highlighted) color = Color.Lerp(color, BattleUiStyle.Blue, selected ? 0.44f : 0.23f);
            if (tint == null) tint = new MaterialPropertyBlock();
            tint.SetColor("_BaseColor", color);
            surface.SetPropertyBlock(tint);
            border.SetActive(highlighted);
        }
    }
}
