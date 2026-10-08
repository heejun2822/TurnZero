using UnityEngine;
using UnityEngine.UI;
namespace TurnZero
{
    internal static class BattleUiStyle
    {
        public static readonly Color Ink = new Color(0.035f, 0.065f, 0.09f, 0.97f);
        public static readonly Color Panel = new Color(0.075f, 0.105f, 0.16f);
        public static readonly Color Edge = new Color(0.18f, 0.29f, 0.36f);
        public static readonly Color Muted = new Color(0.64f, 0.75f, 0.82f);
        public static readonly Color Blue = new Color(0.03f, 0.91f, 0.93f);
        public static readonly Color Enemy = new Color(1f, 0.32f, 0.37f);
        public static readonly Color Health = new Color(0.24f, 0.95f, 0.64f);
        public static readonly Color Gold = new Color(1f, 0.77f, 0.34f);
        public static string Outcome(BattleOutcome result) => result == BattleOutcome.Draw ? "DRAW" : result == BattleOutcome.PlayerOne ? "PLAYER 1 WINS" : "PLAYER 2 WINS";
        public static void Fill(Image image, float amount) => image.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(amount), 1);
        public static void Frame(GameObject target, Color color) => target.GetComponent<Outline>().effectColor = color;
        public static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
