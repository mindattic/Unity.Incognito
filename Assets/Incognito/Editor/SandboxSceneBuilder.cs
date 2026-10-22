using System.IO;
using Incognito.CameraRig;
using Incognito.Eye;
using Incognito.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Incognito.EditorTools
{
    /// <summary>
    /// Builds Assets/Incognito/Scenes/PlayerSandbox.unity: a flat-ground, placeholder-sprite
    /// street00-sized scene to exercise <see cref="PlayerLocomotion"/>,
    /// <see cref="CloseFramingCamera"/>, <see cref="PlayerEyeball"/> and <see cref="EyeCursor"/>
    /// in Play mode before real art (U.INC-T11) exists. Safe to re-run (overwrites the scene and
    /// its placeholder sprites, and re-applies the original sprites' import settings).
    /// </summary>
    public static class SandboxSceneBuilder
    {
        private const string SceneDir = "Assets/Incognito/Scenes";
        private const string ScenePath = SceneDir + "/PlayerSandbox.unity";
        private const string ArtDir = "Assets/Incognito/Art/Placeholder";
        private const string OriginalArtDir = "Assets/Incognito/Art/Original";

        [MenuItem("Incognito/Build Player Sandbox Scene")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var groundSprite = CreateSolidSpriteAsset(ArtDir + "/placeholder_ground.png", new Color(0.35f, 0.35f, 0.35f));
            var playerSprite = CreatePlayerPlaceholder(ArtDir + "/placeholder_player.png");

            // Room sized like the original street00 (11520x1080 px) at the placeholder
            // 1px = 1/32 unit scale: 360 units wide, ground top at y = 0.
            const float px = PlayerLocomotion.WorldUnitsPerOriginalPixel;
            const float roomWidth = 11520f * px;
            const float viewHeight = 1080f * px;

            var ground = new GameObject("Ground", typeof(SpriteRenderer));
            ground.GetComponent<SpriteRenderer>().sprite = groundSprite;
            ground.transform.localScale = new Vector3(roomWidth, 2f, 1f);
            ground.transform.position = new Vector3(roomWidth * 0.5f, -1f, 0f);

            // Street markers every 10 units so horizontal camera motion is visible.
            var markers = new GameObject("Markers");
            for (float x = 0f; x <= roomWidth; x += 10f)
            {
                var marker = new GameObject($"Marker {x:0}", typeof(SpriteRenderer));
                marker.GetComponent<SpriteRenderer>().sprite = groundSprite;
                marker.transform.SetParent(markers.transform);
                marker.transform.localScale = new Vector3(0.5f, 12f, 1f);
                marker.transform.position = new Vector3(x, 6f, 1f);
            }

            // Placeholder at spr_player's true 512x1024 size and origin (256,168), so the camera's
            // 512px player width and the eye socket offsets line up with it. obj_walker's
            // initialY = room_height - sprite_height/2 puts that origin 512px above the floor.
            var player = new GameObject("Player", typeof(SpriteRenderer));
            player.GetComponent<SpriteRenderer>().sprite = playerSprite;
            player.transform.position = new Vector3(roomWidth * 0.5f, 512f * px, 0f);
            var locomotion = player.AddComponent<PlayerLocomotion>();

            // Orthographic view matching the original 1920x1080 window, resting with the
            // ground along the bottom (the original kept the player's feet at the room floor).
            var camGo = new GameObject("Main Camera", typeof(Camera));
            var cam = camGo.GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = viewHeight * 0.5f;
            cam.backgroundColor = new Color(0.08f, 0.08f, 0.12f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            camGo.transform.position = new Vector3(roomWidth * 0.5f, viewHeight * 0.5f - 2f, -10f);
            camGo.tag = "MainCamera";

            var framing = camGo.AddComponent<CloseFramingCamera>();
            framing.Player = locomotion;
            var so = new SerializedObject(framing);
            so.FindProperty("roomMinX").floatValue = 0f;
            so.FindProperty("roomMaxX").floatValue = roomWidth;
            so.ApplyModifiedPropertiesWithoutUndo();

            // The in-world eyeball (obj_eye) and the eye-shaped cursor (obj_mouse), with the
            // original sprites imported at their GameMaker origins.
            var eye = new GameObject("PlayerEyeball").AddComponent<PlayerEyeball>();
            eye.Player = locomotion;
            eye.ViewCamera = cam;
            var eyeSo = new SerializedObject(eye);
            eyeSo.FindProperty("eyeWhite").objectReferenceValue = ImportOriginal("spr_eyewhite", 0, 0);
            eyeSo.FindProperty("pupil").objectReferenceValue = ImportOriginal("spr_eye_ball", 5, 5);
            eyeSo.FindProperty("eyelidLeft").objectReferenceValue = ImportOriginal("spr_eyelid_left", 0, 0);
            eyeSo.FindProperty("eyelidRight").objectReferenceValue = ImportOriginal("spr_eyelid_right", 0, 0);
            eyeSo.ApplyModifiedPropertiesWithoutUndo();

            var cursor = new GameObject("EyeCursor", typeof(SpriteRenderer)).AddComponent<EyeCursor>();
            cursor.ViewCamera = cam;
            var cursorSo = new SerializedObject(cursor);
            cursorSo.FindProperty("eyeSprite").objectReferenceValue = ImportOriginal("spr_cursor_eye", 16, 16);
            cursorSo.FindProperty("talkSprite").objectReferenceValue = ImportOriginal("spr_cursor_talk", 16, 16);
            cursorSo.FindProperty("pointerSprite").objectReferenceValue = ImportOriginal("spr_cursor_pointer", 0, 0);
            cursorSo.FindProperty("reticleSprite").objectReferenceValue = ImportOriginal("spr_cursor_reticle", 16, 16);
            cursorSo.ApplyModifiedPropertiesWithoutUndo();

            if (!AssetDatabase.IsValidFolder(SceneDir))
            {
                AssetDatabase.CreateFolder("Assets/Incognito", "Scenes");
            }

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Sets the import of one of the original sprites copied verbatim into Art/Original (eye
        /// stack and cursors) so its pivot is GameMaker's origin (xorig/yorigin from its
        /// .sprite.gmx, measured from the top-left), at 1 original px = 1/32 unit, point-filtered.
        /// </summary>
        private static Sprite ImportOriginal(string name, int originX, int originY)
        {
            return ImportPixelSprite($"{OriginalArtDir}/{name}.png", originX, originY);
        }

        private static Sprite ImportPixelSprite(string path, int originX, int originY)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.GetSourceTextureWidthAndHeight(out int width, out int height);

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.textureType = TextureImporterType.Sprite;
            settings.spriteMode = (int)SpriteImportMode.Single;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = new Vector2((float)originX / width, 1f - (float)originY / height);
            settings.spritePixelsPerUnit = 1f / PlayerLocomotion.WorldUnitsPerOriginalPixel;
            settings.filterMode = FilterMode.Point;
            settings.mipmapEnabled = false;
            importer.SetTextureSettings(settings);
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>
        /// A flat grey 512x1024 stand-in for spr_player, pivoted at its origin (256,168), with a
        /// transparent socket cut where obj_eye draws spr_eyewhite for the (unflipped,
        /// left-facing) art: the eye stack renders behind the player, as in the original, and
        /// shows through this hole. Mirroring for right-facing lands within 3px of obj_eye's
        /// right-facing socket (offsetX 93 vs. a mirrored 96) — the original's own numbers.
        /// </summary>
        private static Sprite CreatePlayerPlaceholder(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            const int width = 512, height = 1024, originX = 256, originY = 168;
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            var skin = new Color(0.8f, 0.8f, 0.8f);
            for (int i = 0; i < pixels.Length; i++) pixels[i] = skin;

            // Socket rect in GM pixels from the top-left: eye x = origin - 108, white drawn at
            // (x - 10, y - 10), 32x20.
            int socketLeft = originX - 108 - 10, socketTop = originY - 10;
            for (int gy = socketTop; gy < socketTop + 20; gy++)
            {
                for (int gx = socketLeft; gx < socketLeft + 32; gx++)
                {
                    pixels[(height - 1 - gy) * width + gx] = Color.clear; // texture rows run bottom-up
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();

            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            return ImportPixelSprite(path, originX, originY);
        }

        private static Sprite CreateSolidSpriteAsset(string path, Color color)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply();

            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = size;
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
