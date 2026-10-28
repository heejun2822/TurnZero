using UnityEngine;

namespace TurnZero
{
    [ExecuteAlways]
    public sealed class LocalHudLayout : MonoBehaviour
    {
        [SerializeField] private RectTransform safeArea, header, intel, commands, roster, ap, toolbar, hint, selectionHeader, actions, details, craft, ready;
        [SerializeField] private RectTransform[] ownCards, enemyCards;
        [SerializeField] private GameObject[] desktopIntelLabels = new GameObject[0];
        private Vector2 size;
        private Rect lastSafe;
        private void LateUpdate()
        {
            if (size.x == Screen.width && size.y == Screen.height && lastSafe == Screen.safeArea) return;
            size = new Vector2(Screen.width,Screen.height); lastSafe = Screen.safeArea;
            safeArea.anchorMin = new Vector2(lastSafe.xMin/Screen.width,lastSafe.yMin/Screen.height);
            safeArea.anchorMax = new Vector2(lastSafe.xMax/Screen.width,lastSafe.yMax/Screen.height); safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
            bool portrait = Screen.height > Screen.width;
            foreach (var label in desktopIntelLabels) label.SetActive(!portrait);
            Set(header,0.018f,portrait ? 0.943f : 0.918f,0.982f,0.985f);
            Set(intel,0.018f,portrait ? 0.843f : 0.29f,portrait ? 0.982f : 0.19f,portrait ? 0.925f : 0.895f);
            Set(commands,portrait ? 0.018f : 0.785f,portrait ? 0.046f : 0.29f,0.982f,portrait ? 0.27f : 0.895f);
            Set(roster,portrait ? 0.018f : 0.205f,portrait ? 0.285f : 0.038f,0.982f,portrait ? 0.35f : 0.212f);
            Set(ap,0.018f,portrait ? 0.36f : 0.038f,portrait ? 0.982f : 0.19f,portrait ? 0.42f : 0.265f);
            Set(toolbar,portrait ? 0.018f : 0.205f,portrait ? 0.005f : 0.235f,portrait ? 0.982f : 0.765f,portrait ? 0.035f : 0.278f);
            Set(hint,portrait ? 0.02f : 0.21f,portrait ? 0.425f : 0.285f,portrait ? 0.98f : 0.76f,portrait ? 0.445f : 0.31f);
            for (int i = 0; i < ownCards.Length; i++) Set(ownCards[i],i/5f+0.004f,0.02f,(i+1)/5f-0.004f,0.98f);
            for (int i = 0; i < enemyCards.Length; i++)
                if (portrait) Set(enemyCards[i],i/5f+0.005f,0.02f,(i+1)/5f-0.005f,0.98f);
                else Set(enemyCards[i],0.04f,0.12f+(4-i)*0.155f,0.96f,0.26f+(4-i)*0.155f);
            Set(selectionHeader,0.045f,portrait ? 0.65f : 0.77f,portrait ? 0.29f : 0.955f,0.97f);
            Set(actions,portrait ? 0.32f : 0.045f,portrait ? 0.53f : 0.51f,0.955f,portrait ? 0.97f : 0.75f);
            Set(details,0.045f,portrait ? 0.10f : 0.30f,portrait ? 0.29f : 0.955f,portrait ? 0.61f : 0.49f);
            Set(craft,portrait ? 0.32f : 0.045f,0.10f,portrait ? 0.76f : 0.955f,portrait ? 0.48f : 0.285f);
            Set(ready,portrait ? 0.79f : 0.045f,portrait ? 0.15f : 0.02f,0.955f,portrait ? 0.45f : 0.085f);
        }
        private static void Set(RectTransform rect, float x0, float y0, float x1, float y1)
        { rect.anchorMin = new Vector2(x0,y0); rect.anchorMax = new Vector2(x1,y1); rect.offsetMin = rect.offsetMax = Vector2.zero; }
    }
}
