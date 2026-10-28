using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static TurnZero.BattleUiStyle;

namespace TurnZero
{
    [ExecuteAlways]
    public sealed class BattleHudLayout : MonoBehaviour
    {
        [SerializeField]
        private Canvas canvasRoot;
        [SerializeField]
        private RectTransform safeArea;
        [SerializeField]
        private RectTransform headerPanel;
        [SerializeField]
        private RectTransform boardPanel;
        [SerializeField]
        private RectTransform controlsPanel;
        [SerializeField]
        private RectTransform teamPanel;
        [SerializeField]
        private RectTransform apPanel;
        [SerializeField]
        private RectTransform toolbar;
        [SerializeField]
        private RectTransform helpPanel;
        [SerializeField]
        private RectTransform[] cards;
        private Vector2 lastSize;
        private Rect lastSafeArea;
        private readonly Vector3[] fieldCorners = new Vector3[4];

        public Rect FieldViewport { get; private set; }

        private void LateUpdate()
        {
            Refresh();
        }

        public void Refresh()
        {
            Canvas canvas = canvasRoot;

            if (canvasRoot == null || safeArea == null || cards == null || cards.Length != 4)
            {
                return;
            }

            Rect pixels = canvas.pixelRect;

            if (pixels.width <= 0 || pixels.height <= 0)
            {
                return;
            }

            Vector2 size = pixels.size;

            if (size != lastSize || Screen.safeArea != lastSafeArea)
            {
                lastSize = size;
                lastSafeArea = Screen.safeArea;
                safeArea.anchorMin = new Vector2(lastSafeArea.xMin / Screen.width, lastSafeArea.yMin / Screen.height);
                safeArea.anchorMax = new Vector2(lastSafeArea.xMax / Screen.width, lastSafeArea.yMax / Screen.height);
                bool portrait = size.y > size.x;

                SetAnchors(headerPanel, new Vector2(0, portrait ? 0.94f : 0.947f), Vector2.one);
                SetAnchors(
                    boardPanel,
                    new Vector2(0.008f, portrait ? 0.655f : 0.21f),
                    new Vector2(portrait ? 0.992f : 0.762f, 0.888f));
                SetAnchors(toolbar, new Vector2(0.012f, 0.895f), new Vector2(portrait ? 0.988f : 0.758f, 0.933f));
                SetAnchors(
                    controlsPanel,
                    new Vector2(portrait ? 0.55f : 0.774f, portrait ? 0.145f : 0.187f),
                    new Vector2(0.991f, portrait ? 0.645f : 0.928f));
                SetAnchors(teamPanel, Vector2.zero, portrait ? new Vector2(0.537f, 0.645f) : new Vector2(0.763f, 0.174f));
                SetAnchors(apPanel, new Vector2(portrait ? 0.55f : 0.769f, 0), new Vector2(1, portrait ? 0.135f : 0.174f));
                SetAnchors(
                    helpPanel,
                    new Vector2(0.014f, portrait ? 0.649f : 0.178f),
                    new Vector2(portrait ? 0.99f : 0.755f, portrait ? 0.685f : 0.214f));

                for (int i = 0; i < 4; i++)
                {
                    SetAnchors(
                        cards[i],
                        portrait ? new Vector2(0.02f, 0.02f + (3 - i) * 0.245f) : new Vector2(0.012f + i * 0.247f, 0.1f),
                        portrait ? new Vector2(0.98f, 0.25f + (3 - i) * 0.245f) : new Vector2(0.247f + i * 0.247f, 0.9f));
                }

                Canvas.ForceUpdateCanvases();
            }

            boardPanel.GetWorldCorners(fieldCorners);
            Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector2 lower = RectTransformUtility.WorldToScreenPoint(uiCamera, fieldCorners[0]);
            Vector2 upper = RectTransformUtility.WorldToScreenPoint(uiCamera, fieldCorners[2]);

            FieldViewport = new Rect(
                (lower.x - pixels.x) / pixels.width,
                (lower.y - pixels.y) / pixels.height,
                (upper.x - lower.x) / pixels.width,
                (upper.y - lower.y) / pixels.height);
        }
    }
}
