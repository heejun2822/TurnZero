using UnityEngine;

namespace TurnZero
{
    [CreateAssetMenu(menuName = "TurnZero/Character")]
    public sealed class CharacterDefinition : ScriptableObject
    {
        public CharacterSpec Values;
        public Sprite Portrait;
        public GameObject Model;
        public Color Accent = Color.cyan;
    }
}
