using UnityEngine;

namespace TurnZero
{
    [CreateAssetMenu(menuName = "TurnZero/Field")]
    public sealed class FieldDefinition : ScriptableObject
    {
        public string DisplayName = "Stone courtyard";
        public int Width = 12;
        public int HomeHeight = 8;
        public int NeutralHeight = 4;
        public float TileSpacing = 2.2f;
        public Color Ground = new Color(0.17f, 0.22f, 0.26f);
        public Color Alternate = new Color(0.20f, 0.26f, 0.29f);
        [Tooltip("Optional 12x8 home tile variants, row-major. 0 = ground, 1 = alternate. Cosmetic in the local profile.")]
        public int[] HomeTiles = new int[96];

        public Color TileColor(int x, int y)
        {
            int index = y * Width + x;
            bool alternate = index < HomeTiles.Length && HomeTiles[index] == 1;

            return alternate || (x + y) % 2 == 0 ? Alternate : Ground;
        }
    }
}
