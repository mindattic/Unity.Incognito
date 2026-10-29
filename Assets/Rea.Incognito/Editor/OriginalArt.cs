using System.Collections.Generic;
using System.IO;
using Incognito.Content;
using Incognito.Player;
using Incognito.Settings;
using UnityEditor;
using UnityEngine;

namespace Incognito.EditorTools
{
    /// <summary>
    /// Imports the original's sprites and backgrounds (copied verbatim into
    /// Assets/Incognito/Art/Original) through <see cref="GameContent.Art"/>, the validated catalog in
    /// game-content.json (U.INC-T29). Each image is a single point-filtered sprite whose pivot is
    /// GameMaker's origin (<see cref="ArtDefinition.OriginX"/>/<see cref="ArtDefinition.OriginY"/>,
    /// from the top-left) at 1 original px = 1/32 unit; painted world art imports compressed as a
    /// full-rect quad, pixel sprites and HUD art uncompressed. The room and sandbox builders load
    /// every image by catalog id through <see cref="Import"/>, so no builder carries an origin or a
    /// file literal of its own, and every image imports the same way whichever builder touches it.
    /// </summary>
    public static class OriginalArt
    {
        public const string ArtDir = "Assets/Incognito/Art/Original";

        /// <summary>The project path of a catalogued image.</summary>
        public static string PathOf(ArtDefinition art) => $"{ArtDir}/{art.File}";

        /// <summary>The catalogued art with id <paramref name="id"/>; throws if the catalog lacks it.</summary>
        public static ArtDefinition Find(string id) =>
            GameSettings.Content.FindArt(id)
            ?? throw new KeyNotFoundException($"game-content.json's Art has no '{id}'.");

        /// <summary>Applies every catalogued image's import settings (run before the builders, and
        /// on its own from the menu after adding art to the catalog).</summary>
        [MenuItem("Incognito/Import Original Art")]
        public static void ImportAll()
        {
            foreach (ArtDefinition art in GameSettings.Content.Art) Import(art);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Imports the catalogued image <paramref name="id"/> at its GameMaker origin and
        /// returns its sprite.</summary>
        public static Sprite Import(string id) => Import(Find(id));

        /// <summary>The catalogued image's texture (for UI Toolkit backgrounds), imported first.</summary>
        public static Texture2D ImportTexture(string id) => Import(id).texture;

        public static Sprite Import(ArtDefinition art)
        {
            string path = PathOf(art);
            if (!File.Exists(path)) throw new FileNotFoundException($"Missing original art {path} ('{art.Id}')");

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            if (width != art.Width || height != art.Height)
                throw new InvalidDataException($"{path} is {width}x{height}, but game-content.json catalogues '{art.Id}' as {art.Width}x{art.Height}.");

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.textureType = TextureImporterType.Sprite;
            settings.spriteMode = (int)SpriteImportMode.Single;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = new Vector2((float)art.OriginX / width, 1f - (float)art.OriginY / height);
            settings.spritePixelsPerUnit = 1f / PlayerLocomotion.WorldUnitsPerOriginalPixel;
            settings.filterMode = FilterMode.Point;
            settings.mipmapEnabled = false;
            settings.spriteMeshType = art.Compressed ? SpriteMeshType.FullRect : SpriteMeshType.Tight;
            settings.wrapMode = art.Tiled ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            importer.SetTextureSettings(settings);
            importer.maxTextureSize = 2048;
            importer.textureCompression = art.Compressed ? TextureImporterCompression.CompressedHQ : TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
