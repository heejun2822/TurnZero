using UnityEditor;

namespace TurnZero.Editor
{
    public sealed class PrototypeArtImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Resources/PrototypeUI/")) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.textureType = assetPath.EndsWith("portrait-atlas.png") ? TextureImporterType.Default : TextureImporterType.Sprite;
            importer.spriteImportMode = assetPath.EndsWith("portrait-atlas.png") ? SpriteImportMode.None : SpriteImportMode.Single;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
        }
    }
}
