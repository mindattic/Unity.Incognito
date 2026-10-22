using System.IO;
using Incognito.CameraRig;
using Incognito.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Incognito.EditorTools
{
    /// <summary>
    /// Builds Assets/Incognito/Scenes/PlayerSandbox.unity: a flat-ground, placeholder-sprite
    /// street00-sized scene to exercise <see cref="PlayerLocomotion"/> and
    /// <see cref="CloseFramingCamera"/> in Play mode before real art (U.INC-T11)
    /// exists. Safe to re-run (overwrites the scene and its two placeholder sprites).
    /// </summary>
    public static class SandboxSceneBuilder
    {
        private const string SceneDir = "Assets/Incognito/Scenes";
        private const string ScenePath = SceneDir + "/PlayerSandbox.unity";
        private const string ArtDir = "Assets/Incognito/Art/Placeholder";

        [MenuItem("Incognito/Build Player Sandbox Scene")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var groundSprite = CreateSolidSpriteAsset(ArtDir + "/placeholder_ground.png", new Color(0.35f, 0.35f, 0.35f));
            var playerSprite = CreateSolidSpriteAsset(ArtDir + "/placeholder_player.png", Color.white);

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

            var player = new GameObject("Player", typeof(SpriteRenderer));
            player.GetComponent<SpriteRenderer>().sprite = playerSprite;
            player.transform.localScale = new Vector3(4f, 8f, 1f);
            player.transform.position = new Vector3(roomWidth * 0.5f, 4f, 0f);
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

            if (!AssetDatabase.IsValidFolder(SceneDir))
            {
                AssetDatabase.CreateFolder("Assets/Incognito", "Scenes");
            }

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
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
