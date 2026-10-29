using System.Linq;
using Incognito.EditorTools.Online;
using Incognito.Online;
using Incognito.Player;
using NUnit.Framework;
using Unity.Multiplayer;
using Unity.Multiplayer.Editor;
using Unity.Multiplayer.UI;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Incognito.Tests.Online.EditMode
{
    /// <summary>
    /// The online scene (U.INC-T43) is wired the way the Unity.Multiplayer kit needs it, beside local rooms
    /// that have none of it: a broken reference or an unregistered network prefab fails here instead of in a
    /// build.
    /// </summary>
    public class OnlineSceneTests
    {
        private const string TitleScenePath = "Assets/Rea.Incognito/Scenes/Rooms/Titlescreen.unity";
        private const string Street00Path = "Assets/Rea.Incognito/Scenes/Rooms/Street00.unity";

        [Test]
        public void The_online_scene_has_the_kits_network_manager_front_end_and_the_player_prefab()
        {
            Scene scene = EditorSceneManager.OpenScene(OnlineSceneBuilder.ScenePath, OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();
            NetworkManager manager = roots.Select(r => r.GetComponentInChildren<NetworkManager>()).First(m => m != null);

            Assert.That(manager.GetComponent<NetBootstrap>(), Is.Not.Null, "NetBootstrap");
            Assert.That(manager.GetComponent<NetFrontEnd>(), Is.Not.Null, "NetFrontEnd (Host, Join by IP, Join by code, the lobby)");
            GameObject prefab = manager.NetworkConfig.PlayerPrefab;
            Assert.That(prefab, Is.Not.Null, "the Player Prefab");
            Assert.That(prefab.GetComponent<IncognitoNetPlayer>(), Is.Not.Null, "the player prefab is an IncognitoNetPlayer");
            Assert.That(prefab, Is.SameAs(AssetDatabase.LoadAssetAtPath<GameObject>(OnlineSceneBuilder.PrefabPath)));
            Assert.That(prefab.GetComponent<SpriteRenderer>().sprite, Is.Not.Null, "the player draws spr_player");
            Assert.That(prefab.GetComponent<SpriteRenderer>().sprite.name, Is.EqualTo("spr_player"));

            var lists = new SerializedObject(manager.GetComponent<NetBootstrap>()).FindProperty("prefabLists");
            Assert.That(Enumerable.Range(0, lists.arraySize)
                .Select(i => lists.GetArrayElementAtIndex(i).objectReferenceValue as NetPrefabList)
                .Any(l => l != null && l.Prefabs.Contains(prefab)), "the player prefab is in the scene's prefab list");

            Assert.That(roots.Any(r => r.GetComponentInChildren<IncognitoOnlineGame>() != null), "IncognitoOnlineGame");
            Camera cam = roots.Select(r => r.GetComponentInChildren<Camera>()).First(c => c != null);
            Assert.That(cam.orthographic, Is.True);
            Assert.That(cam.orthographicSize, Is.EqualTo(540f * PlayerLocomotion.WorldUnitsPerOriginalPixel).Within(1e-4f),
                "the original's 1080px-tall view");

            int slices = roots.SelectMany(r => r.GetComponentsInChildren<SpriteRenderer>()).Count(s => s.sortingLayerName == "Background");
            Assert.That(slices, Is.EqualTo(IncognitoAuthority.OnlineRoom().BackgroundSlices.Length), "every background slice of the online room");
        }

        [Test]
        public void The_title_stays_first_in_build_settings_and_the_local_rooms_have_none_of_the_kit()
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            Assert.That(scenes.First(), Is.EqualTo(TitleScenePath), "a launch opens the title");
            Assert.That(scenes.Last(), Is.EqualTo(OnlineSceneBuilder.ScenePath));
            Assert.That(IncognitoOnlineGame.SoloScene, Is.EqualTo(System.IO.Path.GetFileNameWithoutExtension(TitleScenePath)),
                "Play solo opens the title");

            foreach (string path in new[] { TitleScenePath, Street00Path })
            {
                Scene local = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                Assert.That(local.GetRootGameObjects().All(r => r.GetComponentInChildren<NetworkManager>(true) == null),
                    path + " has no NetworkManager");
            }
        }

        [Test]
        public void Only_the_online_launch_switches_open_the_online_scene()
        {
            Assert.That(OnlineLaunch.Requested(new[] { "Incognito.exe", "-online" }), Is.True);
            Assert.That(OnlineLaunch.Requested(new[] { "Incognito.exe", "-mode", "server" }), Is.True, "a dedicated server");
            Assert.That(OnlineLaunch.Requested(new[] { "Incognito.exe", "-bot", "A" }), Is.True, "a smoke-test bot");
            Assert.That(OnlineLaunch.Requested(new[] { "Incognito.exe", "-muted", "-logFile", "play.log" }), Is.False, "play.ps1's own launch opens the title");
            Assert.That(OnlineLaunch.Requested(null), Is.False);
            Assert.That(OnlineLaunch.OnlineScene, Is.EqualTo(System.IO.Path.GetFileNameWithoutExtension(OnlineSceneBuilder.ScenePath)));
        }

        [Test]
        public void Every_network_prefab_passes_the_kits_validator()
        {
            NetPrefabValidationReport report = NetPrefabValidator.ValidateProject(new[] { "Assets/Rea.Incognito" });
            report.Log();
            Assert.That(report.HasErrors, Is.False, "see the validator's messages in the log");
        }
    }
}
