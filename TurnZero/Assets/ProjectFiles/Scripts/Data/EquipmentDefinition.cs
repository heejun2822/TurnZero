using UnityEngine;

namespace TurnZero.Game
{
    [CreateAssetMenu(menuName = "TurnZero/Equipment")]
    public sealed class EquipmentDefinition : ScriptableObject
    {
        public EquipmentSpec Values;
        public Sprite Icon;
        public Color Accent = Color.cyan;
    }
}
