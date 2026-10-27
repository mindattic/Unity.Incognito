using System.Collections.Generic;
using System.IO;
using System.Linq;
using Incognito.Content;
using Incognito.Online;
using Incognito.Player;
using Unity.Multiplayer;        // NetPrefabList
using Unity.Multiplayer.Editor; // NetFrontEndSetup, NetSceneSetupOptions
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Incognito.EditorTools.Online
{
    /// <summary>
    /// Writes the online scene (U.INC-T43) in code, beside the untouched local rooms: the online room's
    /// background slices and an orthographic camera at the original 1920x1080 view, the kit's NetworkManager
    /// with Unity Transport, NetBootstrap and the drop-in NetFrontEnd (through the kit's own Add Front End),
    /// the <see cref="IncognitoNetPlayer"/> prefab (spr_player at its GameMaker origin) as the Player Prefab
    /// and in the scene's prefab list, and <see cref="IncognitoOnlineGame"/>. The scene goes last in Build
    /// Settings, so a launch still opens the title. Every run rewrites the prefab's sprite and the scene, so
    /// the scene can't drift from the content; mp-host.json is written only when missing.
    ///
    /// Batch: Unity -batchmode -quit -projectPath . -executeMethod Incognito.EditorTools.Online.OnlineSceneBuilder.BuildAll
    /// </summary>
    public static class OnlineSceneBuilder
    {
        public const string ScenePath = "Assets/Incognito/Scenes/Online.unity";
        public const string PrefabPath = "Assets/Incognito/Resources/" + IncognitoNetPlayer.PrefabResource + ".prefab";
        public const string PrefabListPath = "Assets/Incognito/Online/IncognitoNetPrefabs.asset";
        public const string MpHostPath = "mp-host.json";

        /// <summary>mp-host's defaults: the server and the smoke test's bots run the online scene.</summary>
        public const string MpHostJson = "{\n  \"name\": \"unity-incognito\",\n  \"scene\": \"" + ScenePath + "\",\n  \"bots\": \"A,B\"\n}\n";

        private const float Px = PlayerLocomotion.WorldUnitsPerOriginalPixel;
        private const float ViewWidth = 1920f;
        private const float ViewHeight = 1080f;
        private const string BackgroundLayer = "Background"; // RoomComposer's, ProjectSettings/TagManager.asset
        private const int BackgroundOrder = -100;

        [MenuItem("Incognito/Build Online Scene")]
        public static void BuildOnlineScene()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildAll();
        }

        /// <summary>Batch entry point: the prefab, the prefab list, the scene, the build settings and mp-host.json.</summary>
        public static void BuildAll()
        {
            GameObject prefab = EnsurePlayerPrefab();
            NetPrefabList list = EnsurePrefabList(prefab);
            WriteScene(prefab, list);
            EnsureBuildSettings();
            if (!File.Exists(MpHostPath)) File.WriteAllText(MpHostPath, MpHostJson);
            AssetDatabase.SaveAssets();
        }

        /// <summary>The online player: a NetworkObject, a SpriteRenderer with spr_player and <see cref="IncognitoNetPlayer"/>.</summary>
        public static GameObject EnsurePlayerPrefab()
        {
            Sprite body = OriginalArt.Import("spr_player");
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (existing != null)
            {
                var renderer = existing.GetComponent<SpriteRenderer>();
                if (renderer != null && renderer.sprite != body)
                {
                    renderer.sprite = body;
                    EditorUtility.SetDirty(existing);
                    PrefabUtility.SavePrefabAsset(existing);
                }

                return existing;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            var go = new GameObject("IncognitoNetPlayer");
            go.AddComponent<NetworkObject>();
            go.AddComponent<SpriteRenderer>().sprite = body;
            go.AddComponent<IncognitoNetPlayer>();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
            Object.DestroyImmediate(go);
            Debug.Log("[Incognito] Online player prefab written to " + PrefabPath);
            return prefab;
        }

        /// <summary>The scene's NetPrefabList, holding the player prefab.</summary>
        public static NetPrefabList EnsurePrefabList(GameObject prefab)
        {
            var list = AssetDatabase.LoadAssetAtPath<NetPrefabList>(PrefabListPath);
            if (list == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(PrefabListPath));
                list = ScriptableObject.CreateInstance<NetPrefabList>();
                AssetDatabase.CreateAsset(list, PrefabListPath);
            }

            if (!list.Prefabs.Contains(prefab))
            {
                list.Add(prefab);
                EditorUtility.SetDirty(list);
            }

            return list;
        }

        private static void WriteScene(GameObject prefab, NetPrefabList list)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RoomDefinition room = IncognitoAuthority.OnlineRoom();

            // The room's background slices, laid edge to edge as RoomComposer lays them.
            var backdrop = new GameObject("Background (" + room.Id + ")");
            for (int i = 0; i < room.BackgroundSlices.Length; i++)
            {
                var slice = new GameObject(room.BackgroundSlices[i], typeof(SpriteRenderer));
                slice.transform.SetParent(backdrop.transform, false);
                var sr = slice.GetComponent<SpriteRenderer>();
                sr.sprite = OriginalArt.Import(room.BackgroundSlices[i]);
                sr.sortingLayerName = BackgroundLayer;
                sr.sortingOrder = BackgroundOrder;
                slice.transform.position = new Vector3(i * ViewWidth * Px, ViewHeight * Px, 0f);
            }

            // The original's 1920x1080 view, the floor along its bottom; the owner's player moves it sideways.
            var camGo = new GameObject("Main Camera", typeof(Camera));
            var cam = camGo.GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = ViewHeight * 0.5f * Px;
            cam.backgroundColor = Color.black;
            cam.clearFlags = CameraClearFlags.SolidColor;
            camGo.transform.position = new Vector3(room.PlayerStartX * Px, ViewHeight * 0.5f * Px, -10f);
            camGo.tag = "MainCamera";

            var setup = NetFrontEndSetup.Run(scene, new NetSceneSetupOptions { PrefabList = list, RegisterProjectPrefabs = false });
            if (!setup.Succeeded) throw new System.InvalidOperationException("[Incognito] Add Front End failed: " + setup.Message);
            setup.Scene.Manager.NetworkConfig.PlayerPrefab = prefab;
            EditorUtility.SetDirty(setup.Scene.Manager);

            new GameObject("IncognitoOnlineGame").AddComponent<IncognitoOnlineGame>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[Incognito] Online scene written to " + ScenePath);
        }

        /// <summary>The local rooms first, as RoomSceneBuilder lists them (a launch opens the title); the online scene last.</summary>
        private static void EnsureBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes.Where(s => s.path != ScenePath));
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
