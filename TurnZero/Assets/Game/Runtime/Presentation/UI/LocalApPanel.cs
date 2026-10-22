using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TurnZero.Game
{
    public sealed class LocalApPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text availableText, reservedText, maximumText, inventoryText, headingText;
        [SerializeField] private Image[] authoredPips;
        [SerializeField] private GridLayoutGroup grid;
        [SerializeField] private Image pipPrefab;
        private List<Image> pips;
        private BattleView lastView;
        private int lastMaximum;
        private Vector2 lastSize;
        public void Render(BattleView view, int maximum)
        {
            lastView = view; lastMaximum = maximum; lastSize = ((RectTransform)transform).rect.size;
            bool portrait = Screen.height > Screen.width;
            Set(headingText.rectTransform,portrait ? 0.02f : 0.07f,portrait ? 0.55f : 0.86f,portrait ? 0.23f : 0.94f,portrait ? 0.95f : 0.97f);
            Set(availableText.rectTransform,0.61f,portrait ? 0.45f : 0.41f,portrait ? 0.73f : 0.95f,portrait ? 0.98f : 0.73f);
            Set(maximumText.rectTransform,portrait ? 0.74f : 0.61f,portrait ? 0.55f : 0.26f,0.95f,portrait ? 0.9f : 0.43f);
            Set(reservedText.rectTransform,portrait ? 0.25f : 0.07f,portrait ? 0.55f : 0.72f,portrait ? 0.53f : 0.94f,portrait ? 0.95f : 0.85f);
            Set(inventoryText.rectTransform,portrait ? 0.55f : 0.05f,0.02f,0.95f,portrait ? 0.40f : 0.15f);
            Set((RectTransform)grid.transform,portrait ? 0.02f : 0.07f,portrait ? 0.08f : 0.20f,portrait ? 0.52f : 0.54f,portrait ? 0.45f : 0.67f);
            if (pips == null) pips = new List<Image>(authoredPips);
            if (pips.Count != maximum)
            {
                foreach (var pip in pips) Destroy(pip.gameObject); pips.Clear();
                for (int i = 0; i < maximum; i++) pips.Add(Instantiate(pipPrefab,grid.transform));
            }
            var rect = (RectTransform)grid.transform;
            int columns = portrait ? Mathf.Min(25,maximum) : 5;
            int rows = Mathf.CeilToInt((float)maximum / columns);
            grid.constraintCount = columns;
            grid.cellSize = new Vector2(Mathf.Max(2,(rect.rect.width - grid.spacing.x*(columns-1))/columns),Mathf.Max(2,(rect.rect.height-grid.spacing.y*(rows-1))/rows));
            int available = view.AP - view.ReservedAP;
            availableText.text = $"{available:00}"; reservedText.text = $"{view.ReservedAP} RESERVED"; maximumText.text = $"/ {maximum} AP";
            inventoryText.text = $"CRAFT  {view.Materials}   /   UPGRADE  --   /   CONSUMABLE  --";
            for (int i = 0; i < pips.Count; i++) pips[i].color = i < available ? new Color(0.15f,0.8f,0.73f) : i < view.AP ? new Color(0.95f,0.66f,0.24f) : new Color(0.13f,0.18f,0.23f);
        }
        private void LateUpdate() { if (lastView != null && lastSize != ((RectTransform)transform).rect.size) Render(lastView,lastMaximum); }
        private static void Set(RectTransform rect, float x0, float y0, float x1, float y1)
        { rect.anchorMin = new Vector2(x0,y0); rect.anchorMax = new Vector2(x1,y1); rect.offsetMin = rect.offsetMax = Vector2.zero; }
    }
}
