#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace GrimoireAegis.EditorTools
{
    /// <summary>
    /// Enforces strict pixel-art import rules from `.agents/skills/pixel-art`:
    /// - FilterMode.Point (nearest-neighbor, zero bilinear blur)
    /// - Uncompressed RGBA32 (zero block compression artifacts)
    /// - Mipmaps disabled, NPOTScale.None
    /// </summary>
    public class PixelArtAssetPostprocessor : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.Contains("Resources/Gen/"))
                return;

            TextureImporter importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.alphaIsTransparency = true;
            importer.spritePixelsPerUnit = 16f;
        }
    }
}
#endif
