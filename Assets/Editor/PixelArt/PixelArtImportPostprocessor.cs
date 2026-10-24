using UnityEditor;
using UnityEditor.U2D.Aseprite;
using UnityEngine;

namespace Game01.EditorTools
{
    /// <summary>
    /// Aplica la configuración de pixel art a todo el arte que entra en Assets/Art
    /// la primera vez que se importa. Si después cambiás algo a mano en el Inspector,
    /// se respeta.
    /// </summary>
    public class PixelArtImportPostprocessor : AssetPostprocessor
    {
        public const string ArtRoot = "Assets/Art/";

        // PNG y otras imágenes.
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtRoot) || !assetImporter.importSettingsMissing)
                return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = PixelArtSettings.PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
        }

        // Archivos .aseprite / .ase. El importador ya usa Point, sin compresión
        // y pivote abajo al centro; solo falta el PPU (por defecto es 100).
        void OnPreprocessAsset()
        {
            if (assetImporter is not AsepriteImporter importer)
                return;
            if (!assetPath.StartsWith(ArtRoot) || !importer.importSettingsMissing)
                return;

            importer.spritePixelsPerUnit = PixelArtSettings.PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
        }
    }
}
