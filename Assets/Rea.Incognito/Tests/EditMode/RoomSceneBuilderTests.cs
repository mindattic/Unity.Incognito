using System.Collections.Generic;
using System.IO;
using System.Linq;
using Incognito.EditorTools;
using Incognito.World;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Incognito.Tests
{
    /// <summary>
    /// U.INC-T26: Incognito/Build Room Scenes must be deterministic — regenerating the four
    /// committed room scenes from the current game-content.json and original art must reproduce
    /// their content. Room content itself (background, props, exits) is no longer baked into these
    /// files at all (<see cref="RoomBootstrap"/> rebuilds it from data at Awake), so what's left to
    /// regenerate — the sandbox rig copy plus one bootstrap component's art/audio references — can
    /// never silently drift from what the builder produces today.
    ///
    /// Builds into a scratch directory under Assets (deleted afterward in <see cref="TearDown"/>)
    /// rather than overwriting the committed scenes in place, so a failing or crashing run can never
    /// leave the working tree dirty or the committed files mid-rewrite. Compared by loading both
    /// scenes and reading live objects (<see cref="RoomBootstrap"/>'s id and
    /// <see cref="RoomRuntimeAssets"/>, and every GameObject's hierarchy path) rather than diffing
    /// the raw YAML: Unity assigns every freshly-created object a new, session-random
    /// <c>fileID</c> and writes a scene's objects ordered by it, so neither the exact numbers nor
    /// their order in the file are real content, and a text-only comparison that tried to normalize
    /// them away would still have to guess which of two content-identical objects is "the same one"
    /// a reference points to. Reading the loaded objects sidesteps that entirely: a Sprite/AudioClip
    /// field compares equal exactly when it is the same asset (by GUID), regardless of how either
    /// scene happened to serialize the reference to it.
    /// </summary>
    public class RoomSceneBuilderTests
    {
        private const string OutputDir = "Assets/Rea.Incognito/Scenes/__RoomSceneBuilderTestOutput__";

        private static readonly (string Name, string CommittedPath)[] Scenes =
        {
            ("Titlescreen", "Assets/Rea.Incognito/Scenes/Rooms/Titlescreen.unity"),
            ("Street00", "Assets/Rea.Incognito/Scenes/Rooms/Street00.unity"),
            ("Street01", "Assets/Rea.Incognito/Scenes/Rooms/Street01.unity"),
            ("Shop", "Assets/Rea.Incognito/Scenes/Rooms/Shop.unity"),
        };

        [TearDown]
        public void TearDown()
        {
            if (AssetDatabase.IsValidFolder(OutputDir)) AssetDatabase.DeleteAsset(OutputDir);
        }

        [Test]
        public void Regenerating_the_room_scenes_reproduces_the_committed_hierarchy_and_bootstrap_data()
        {
            RoomSceneBuilder.BuildAll(OutputDir, updateBuildSettings: false);

            foreach (var scene in Scenes)
            {
                string generatedPath = $"{OutputDir}/{scene.Name}.unity";
                Assert.IsTrue(File.Exists(generatedPath), $"Incognito/Build Room Scenes did not produce {generatedPath}");

                (string[] names, RoomBootstrap bootstrap) committed = Load(scene.CommittedPath);
                string committedRoomId = committed.bootstrap != null ? committed.bootstrap.RoomId : null;
                RoomRuntimeAssets committedAssets = committed.bootstrap != null ? committed.bootstrap.Assets : null;

                (string[] names, RoomBootstrap bootstrap) generated = Load(generatedPath);

                CollectionAssert.AreEqual(committed.names, generated.names,
                    $"{scene.CommittedPath}'s GameObject hierarchy differs from a fresh build.");

                if (committed.bootstrap == null && generated.bootstrap == null) continue; // Titlescreen: no RoomBootstrap

                Assert.IsNotNull(generated.bootstrap, $"{generatedPath} lost its RoomBootstrap");
                Assert.AreEqual(committedRoomId, generated.bootstrap.RoomId, $"{scene.CommittedPath}'s RoomBootstrap.RoomId differs from a fresh build.");
                AssertAssetsEqual(committedAssets, generated.bootstrap.Assets, scene.CommittedPath);
            }
        }

        private static (string[] names, RoomBootstrap bootstrap) Load(string path)
        {
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var names = new List<string>();
            foreach (GameObject root in scene.GetRootGameObjects()) CollectNames(root.transform, "", names);
            RoomBootstrap bootstrap = Object.FindAnyObjectByType<RoomBootstrap>();
            return (names.ToArray(), bootstrap);
        }

        private static void CollectNames(Transform t, string prefix, List<string> into)
        {
            string path = prefix + "/" + t.name;
            into.Add(path);
            for (int i = 0; i < t.childCount; i++) CollectNames(t.GetChild(i), path, into);
        }

        private static void AssertAssetsEqual(RoomRuntimeAssets a, RoomRuntimeAssets b, string label)
        {
            Assert.IsNotNull(a, $"{label}: no baked RoomRuntimeAssets to compare");
            Assert.IsNotNull(b, $"{label}: a fresh build produced no RoomRuntimeAssets");

            CollectionAssert.AreEqual(a.BackgroundSlices, b.BackgroundSlices, $"{label}: BackgroundSlices");
            Assert.AreEqual(a.LamppostSprite, b.LamppostSprite, $"{label}: LamppostSprite");
            Assert.AreEqual(a.LamplightSprite, b.LamplightSprite, $"{label}: LamplightSprite");
            Assert.AreEqual(a.MailboxSprite, b.MailboxSprite, $"{label}: MailboxSprite");
            Assert.AreEqual(a.RainSprite, b.RainSprite, $"{label}: RainSprite");
            Assert.AreEqual(a.ExitIconSprite, b.ExitIconSprite, $"{label}: ExitIconSprite");
            Assert.AreEqual(a.DescriptionBorder, b.DescriptionBorder, $"{label}: DescriptionBorder");
            Assert.AreEqual(a.Music, b.Music, $"{label}: Music");
            CollectionAssert.AreEqual(a.Footsteps, b.Footsteps, $"{label}: Footsteps");
            Assert.AreEqual(a.Whoosh, b.Whoosh, $"{label}: Whoosh");
            Assert.AreEqual(a.ReloadClip, b.ReloadClip, $"{label}: ReloadClip");
            Assert.AreEqual(a.HolsterClip, b.HolsterClip, $"{label}: HolsterClip");
            Assert.AreEqual(a.FireClip, b.FireClip, $"{label}: FireClip");
            Assert.AreEqual(a.TypeClip, b.TypeClip, $"{label}: TypeClip");
            Assert.AreEqual(a.DoorOpenClip, b.DoorOpenClip, $"{label}: DoorOpenClip");
            Assert.AreEqual(a.FlickClip, b.FlickClip, $"{label}: FlickClip");
            Assert.IsNotNull(a.FlickClip, $"{label}: no baked sfx_flick (U.INC-T28)");
        }
    }
}
