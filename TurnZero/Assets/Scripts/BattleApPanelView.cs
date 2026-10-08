using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static TurnZero.BattleUiStyle;
namespace TurnZero
{
    public sealed class BattleApPanelView : MonoBehaviour
    {
        [SerializeField] private Text apText, reservedText;
        [SerializeField] private Image[] apPips;
        public void Render(PlayerView view, BattleRules rules)
        {
            apText.text = $"{view.AP - view.ReservedAP} / {rules.MaximumAP}";
            reservedText.text = $"RESERVED {view.ReservedAP}";
            for (int i = 0; i < apPips.Length; i++)
            {
                float point = (i + 1) * (float)rules.MaximumAP / apPips.Length;
                apPips[i].color = point <= view.AP - view.ReservedAP ? Blue : point <= view.AP ?
                    new Color(0.23f, 0.32f, 0.37f) : new Color(0.11f, 0.16f, 0.2f);
            }
        }
    }
}
