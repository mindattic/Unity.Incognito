using System.Collections.Generic;
using System.IO;
using System.Linq;
using Incognito.Content;
using Incognito.Player;
using Incognito.Settings;
using Incognito.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Incognito.EditorTools
{
    /// <summary>
    /// Builds the original's playable rooms (RECONSTRUCTION.md §7–8) as scenes under
    /// Assets/Rea.Incognito/Scenes/Rooms/: Titlescreen, Street00 (Bleecher Street), Street01 (2nd
    /// Avenue) and Shop (Simon's Pawn and Loan), and puts them in the build with the title first.
    ///
    /// Every room is laid out from <see cref="GameSettings.Content"/>'s validated
    /// <see cref="RoomDefinition"/>s (U.INC-T22) — transcribed from the original rooms/*.room.gmx
    /// (tile slices, instance positions and creation code) into
    /// Assets/Rea.Incognito/Resources/Data/game-content.json, instead of a hard-coded table here — on
    /// top of a copy of the player rig in PlayerSandbox.unity — player, arms, eyeball, eye cursor,
    /// close camera, typed console (and the street00 NPC) — minus the sandbox's placeholder
    /// ground, markers and console targets.
    ///
    /// U.INC-T26: this builder no longer bakes a room's background/props/exits into the scene file
    /// itself. It only imports that room's art and audio (<see cref="RoomRuntimeAssets"/>) and drops
    /// them on a <see cref="RoomBootstrap"/> component, which rebuilds the room's actual content
    /// (<see cref="RoomComposer"/>) from game-content.json at Awake — in Play mode and in a build
    /// (not merely opening the scene in the Editor; see <see cref="RoomBootstrap"/>'s own doc
    /// comment) — so the committed scene can never drift from the data it was generated from.
    /// Rebuild the sandbox first if the rig changes, then rerun this. Safe to re-run (overwrites the
    /// scenes and re-applies the original assets' import settings) — and deterministic: re-running
    /// it with unchanged data and art reproduces the same content (Tests/EditMode/RoomSceneBuilderTests.cs
    /// proves this with the scene's actual content, not Unity's own per-session, non-reproducible
    /// <c>fileID</c>s or their order in the file).
    /// </summary>
    public static class RoomSceneBuilder
    {
        private const string SandboxPath = "Assets/Rea.Incognito/Scenes/PlayerSandbox.unity";
        private const string RoomsDir = "Assets/Rea.Incognito/Scenes/Rooms";
        private const string AudioDir = "Assets/Rea.Incognito/Audio/Original";

        private const float Px = PlayerLocomotion.WorldUnitsPerOriginalPixel;
        private const float ViewWidth = 1920f;
        private const float ViewHeight = 1080f;

        // The title screen's own backdrop is still baked here (it isn't a RoomDefinition room); its
        // sorting layer and order match RoomComposer's BackgroundLayer/BackgroundOrder
        // (ProjectSettings/TagManager.asset, RECONSTRUCTION.md §7, U.INC-T24).
        private const int BackgroundOrder = -100;
        private const string BackgroundLayer = "Background";

        public const string TitleScene = "Titlescreen";
        public const string Street00 = "Street00";
        public const string Street01 = "Street01";
        public const string Shop = "Shop";

        [MenuItem("Incognito/Build Room Scenes")]
        public static void BuildRoomScenes() => BuildAll();

        /// <summary>
        /// Builds the title and every <see cref="RoomDefinition"/> room into <paramref name="roomsDir"/>
        /// (the real Assets/Rea.Incognito/Scenes/Rooms by default). <paramref name="updateBuildSettings"/>
        /// lets a test build into a scratch directory (e.g. Tests/EditMode/RoomSceneBuilderTests.cs)
        /// without touching the project's actual EditorBuildSettings scene list.
        /// </summary>
        public static void BuildAll(string roomsDir = RoomsDir, bool updateBuildSettings = true)
        {
            if (!File.Exists(SandboxPath)) SandboxSceneBuilder.Build();
            Directory.CreateDirectory(roomsDir);
            OriginalArt.ImportAll();
            ImportAudio();

            var scenes = new List<string> { BuildTitle(roomsDir) };
            foreach (RoomDefinition spec in GameSettings.Content.Rooms) scenes.Add(BuildRoom(spec, roomsDir));

            if (updateBuildSettings)
            {
                // The rooms go first, in order (a launch opens the title); any other scene already
                // listed (the online scene, U.INC-T43) keeps its place after them.
                var others = EditorBuildSettings.scenes.Where(s => !scenes.Contains(s.path));
                EditorBuildSettings.scenes = scenes.Select(p => new EditorBuildSettingsScene(p, true)).Concat(others).ToArray();
            }
            AssetDatabase.SaveAssets();
        }

        /// <summary>Builds a Windows player of the room scenes to Builds/Windows (for play checks).</summary>
        [MenuItem("Incognito/Build Windows Player")]
        public static void BuildWindowsPlayer()
        {
            BuildAll();
            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = "Builds/Windows/Incognito.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                throw new System.Exception($"Windows build failed: {report.summary.result}");
            }
        }

        // ---------------------------------------------------------------- title

        private static string BuildTitle(string roomsDir)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera(new Vector3(ViewWidth * 0.5f * Px, ViewHeight * 0.5f * Px, -10f));

            // titlescreen.room.gmx: bg_city_black tiled, hspeed -2, under bg_titlescreen.
            var city = new GameObject("bg_city_black (scrolling)", typeof(SpriteRenderer));
            var citySr = city.GetComponent<SpriteRenderer>();
            citySr.sprite = OriginalArt.Import("bg_city_black");
            citySr.drawMode = SpriteDrawMode.Tiled;
            citySr.size = new Vector2(ViewWidth * 3f * Px, ViewHeight * Px);
            citySr.sortingLayerName = BackgroundLayer;
            citySr.sortingOrder = BackgroundOrder;
            city.transform.position = new Vector3(0f, ViewHeight * Px, 0f);
            city.AddComponent<ScrollingBackground>().Configure(-2f);

            var title = new GameObject("bg_titlescreen", typeof(SpriteRenderer));
            var titleSr = title.GetComponent<SpriteRenderer>();
            titleSr.sprite = OriginalArt.Import("bg_titlescreen");
            titleSr.sortingLayerName = BackgroundLayer;
            titleSr.sortingOrder = BackgroundOrder + 1;
            title.transform.position = new Vector3(0f, ViewHeight * Px, 0f);

            // titlescreen's obj_music: the global default track; its own "//current_track = whispers;"
            // stays commented out, so TitleScreen.UnwiredTrack is catalogued but never loaded (U.INC-T28).
            new GameObject("Title Screen").AddComponent<TitleScreen>().Configure(Street00, LoadSound(GameSettings.Content.TitleScreen.Music));

            string path = $"{roomsDir}/{TitleScene}.unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        // ---------------------------------------------------------------- rooms

        /// <summary>Opens the sandbox, strips its placeholder stand-ins, imports this room's art and
        /// audio, and drops them on a <see cref="RoomBootstrap"/> that rebuilds the room's actual
        /// content from <paramref name="spec"/> every time the scene loads (U.INC-T26).</summary>
        private static string BuildRoom(RoomDefinition spec, string roomsDir)
        {
            var scene = EditorSceneManager.OpenScene(SandboxPath, OpenSceneMode.Single);

            // Strip the sandbox-only stand-ins.
            var remove = new HashSet<string> { "Ground", "Markers", "Console Targets" };
            if (!spec.KeepNpc) remove.Add("NPC");
            foreach (var root in scene.GetRootGameObjects())
            {
                if (remove.Contains(root.name)) Object.DestroyImmediate(root);
            }

            var assets = new RoomRuntimeAssets
            {
                BackgroundSlices = spec.BackgroundSlices.Select(OriginalArt.Import).ToArray(),
                LamppostSprite = OriginalArt.Import("spr_lamppost"),
                LamplightSprite = OriginalArt.Import("spr_lamplight01"),
                MailboxSprite = OriginalArt.Import("spr_mailbox"),
                RainSprite = OriginalArt.Import("spr_rain"),
                ExitIconSprite = OriginalArt.Import("spr_icon_exit"),
                DescriptionBorder = OriginalArt.ImportTexture("spr_desc_border"),
                Music = string.IsNullOrEmpty(spec.Music) ? null : LoadSound(spec.Music),
                Footsteps = Enumerable.Range(1, 16).Select(i => LoadSound($"sfx_step_concrete{i:00}")).ToArray(),
                Whoosh = LoadSound("sfx_whoosh"),
                ReloadClip = LoadSound("sfx_reload"),
                HolsterClip = LoadSound("sfx_holster"),
                FireClip = LoadSound("sfx_fire_cannon"),
                TypeClip = LoadSound("sfx_type"),
                DoorOpenClip = LoadSound("sfx_door_open"),
                FlickClip = LoadSound("sfx_flick"),
            };

            var bootstrap = new GameObject("Room Bootstrap").AddComponent<RoomBootstrap>();
            bootstrap.Configure(spec.Id, assets);

            string path = $"{roomsDir}/{spec.Id}.unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        private static Camera CreateCamera(Vector3 position)
        {
            var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            var cam = go.GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = ViewHeight * 0.5f * Px;
            cam.backgroundColor = Color.black;
            cam.clearFlags = CameraClearFlags.SolidColor;
            go.transform.position = position;
            go.tag = "MainCamera";
            return cam;
        }

        // ---------------------------------------------------------------- assets

        /// <summary>Applies every catalogued sound's import (game-content.json's Sounds, U.INC-T28):
        /// what the original streamed (music, the whispers ambient) streams (Vorbis); short effects
        /// decompress on load.</summary>
        private static void ImportAudio()
        {
            foreach (SoundDefinition sound in GameSettings.Content.Sounds)
            {
                string path = $"{AudioDir}/{sound.File}";
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                var importer = AssetImporter.GetAtPath(path) as AudioImporter
                    ?? throw new FileNotFoundException($"Missing original audio {path} (sound '{sound.Id}')");
                var settings = importer.defaultSampleSettings;
                settings.loadType = sound.Streamed ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }
        }

        /// <summary>Loads a sound by its game-content.json catalog id (U.INC-T28), never by a file
        /// literal, so every clip a room uses is one the catalog names.</summary>
        public static AudioClip LoadSound(string id)
        {
            SoundDefinition sound = GameSettings.Content.FindSound(id)
                ?? throw new KeyNotFoundException($"game-content.json's Sounds has no '{id}'.");
            string path = $"{AudioDir}/{sound.File}";
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null) throw new FileNotFoundException($"Missing original audio {path} (sound '{id}')");
            return clip;
        }

        /// <summary>The project path of a catalogued sound's file (Audio/Original).</summary>
        public static string SoundPath(SoundDefinition sound) => $"{AudioDir}/{sound.File}";
    }
}
