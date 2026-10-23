using System.Collections.Generic;
using System.IO;
using System.Linq;
using Incognito.Audio;
using Incognito.CameraRig;
using Incognito.Eye;
using Incognito.Player;
using Incognito.Terminal;
using Incognito.Weapon;
using Incognito.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Incognito.EditorTools
{
    /// <summary>
    /// Builds the original's playable rooms (RECONSTRUCTION.md §7–8) as scenes under
    /// Assets/Incognito/Scenes/Rooms/: Titlescreen, Street00 (Bleecher Street), Street01 (2nd
    /// Avenue) and Shop (Simon's Pawn and Loan), and puts them in the build with the title first.
    ///
    /// Every room is laid out from the data below, transcribed from the original
    /// rooms/*.room.gmx (tile slices, instance positions and creation code), on top of a copy
    /// of the player rig in PlayerSandbox.unity — player, arms, eyeball, eye cursor, close camera,
    /// typed console (and the street00 NPC) — minus the sandbox's placeholder ground, markers and
    /// console targets. Rebuild the sandbox first if the rig changes, then rerun this. Safe to
    /// re-run (overwrites the scenes and re-applies the original assets' import settings).
    /// </summary>
    public static class RoomSceneBuilder
    {
        private const string SandboxPath = "Assets/Incognito/Scenes/PlayerSandbox.unity";
        private const string RoomsDir = "Assets/Incognito/Scenes/Rooms";
        private const string ArtDir = "Assets/Incognito/Art/Original";
        private const string BackgroundDir = ArtDir + "/Backgrounds";
        private const string AudioDir = "Assets/Incognito/Audio/Original";

        private const float Px = PlayerLocomotion.WorldUnitsPerOriginalPixel;
        private const float ViewWidth = 1920f;
        private const float ViewHeight = 1080f;
        private const float PlayerOriginAboveFloor = 512f; // obj_walker initialY = room_height - sprite_height/2

        // Sorting orders, from the original depths (lower depth draws in front).
        private const int BackgroundOrder = -100; // tiles, depth 1000000
        private const int ExitOrder = -19;        // obj_exit, depth 19
        private const int LamppostOrder = -10;    // obj_prop_lamppost, depth 10 (behind obj_bullet's 5)
        private const int LamplightOrder = -9;    // drawn in the lamppost's own draw event, after the post
        private const int RainOrder = 2;          // obj_raindrop, depth -1: in front of the player and arms
        private const int FrontPropOrder = 10;    // obj_prop_mailbox, depth -10

        public const string TitleScene = "Titlescreen";
        public const string Street00 = "Street00";
        public const string Street01 = "Street01";
        public const string Shop = "Shop";

        private const string IntroPoem =
            "In a strange city lying alone\n" +
            "Far down within the dim West,\n" +
            "Where the good and the bad and the worst and the best\n" +
            "Have gone to their eternal rest.\n" +
            "There shrines and palaces and towers\n" +
            "(Time-eaten towers that tremble not!)\n" +
            "Resemble nothing that is ours.\n" +
            "Around, by lifting winds forgot,\n" +
            "Resignedly beneath the sky\n" +
            "The melancholy waters lie.";

        private sealed class SideExitSpec
        {
            public float X;
            public string To;
            public RoomEntrance Enter;
            public float EnterX;
        }

        private sealed class DoorSpec
        {
            public Vector2 Position;
            public string Label;
            public string To;
            public RoomEntrance Enter;
        }

        private sealed class RoomSpec
        {
            public string Scene;
            public string Name;      // get_room_name.gml
            public float Width;
            public string[] Slices;  // tiles, 1920 wide each, left to right from x = 0
            public float PlayerStartX; // where a directly-opened scene puts the player
            public float[] Lampposts = new float[0];
            public float[] Mailboxes = new float[0];
            public SideExitSpec[] SideExits = new SideExitSpec[0];
            public DoorSpec[] Doors = new DoorSpec[0];
            public float[] Barriers = new float[0];
            public bool Rain;
            public bool KeepNpc;
            public string Music;
            public string OpeningText;
            public Vector2 OpeningTextPosition;
        }

        private static readonly RoomSpec[] Rooms =
        {
            new RoomSpec
            {
                Scene = Street00, Name = "Bleecher Street", Width = 11520f,
                Slices = Enumerable.Range(0, 6).Select(i => $"bg_street00_{i:00}").ToArray(),
                PlayerStartX = 11520f - 512f, // start_game's enter_from = "right"
                Lampposts = new[] { 1632f, 4032f, 6496f, 8928f },
                Mailboxes = new[] { 672f, 7104f },
                SideExits = new[]
                {
                    new SideExitSpec { X = -32f, To = Street01, Enter = RoomEntrance.Right },
                    new SideExitSpec { X = 11552f, To = Street01, Enter = RoomEntrance.Left },
                },
                Doors = new[]
                {
                    new DoorSpec { Position = new Vector2(9504f, 480f), Label = "Simon's Pawn and Loan", To = Shop, Enter = RoomEntrance.Right },
                },
                Rain = true, KeepNpc = true, Music = "music_oppressivegloom",
                OpeningText = IntroPoem, OpeningTextPosition = new Vector2(11520f - 650f, 100f),
            },
            new RoomSpec
            {
                Scene = Street01, Name = "2nd Avenue", Width = 15360f,
                Slices = Enumerable.Range(0, 8).Select(i => $"bg_street01_{i:00}").ToArray(),
                PlayerStartX = 512f,
                SideExits = new[]
                {
                    new SideExitSpec { X = -32f, To = Street00, Enter = RoomEntrance.Right },
                    new SideExitSpec { X = 15392f, To = Street00, Enter = RoomEntrance.Left },
                },
                Rain = true, Music = "music_oppressivegloom",
            },
            new RoomSpec
            {
                Scene = Shop, Name = "Simon's Pawn and Loan", Width = 5760f,
                Slices = Enumerable.Range(0, 3).Select(i => $"bg_shop_{i:00}").ToArray(),
                PlayerStartX = 5760f - 512f,
                Barriers = new[] { 800f },
                SideExits = new[]
                {
                    new SideExitSpec { X = 5792f, To = Street00, Enter = RoomEntrance.At, EnterX = 9500f },
                },
                Music = "music_dubferal",
            },
        };

        [MenuItem("Incognito/Build Room Scenes")]
        public static void BuildAll()
        {
            if (!File.Exists(SandboxPath)) SandboxSceneBuilder.Build();
            Directory.CreateDirectory(RoomsDir);
            ImportAudio();

            var scenes = new List<string> { BuildTitle() };
            foreach (var spec in Rooms) scenes.Add(BuildRoom(spec));

            EditorBuildSettings.scenes = scenes.Select(p => new EditorBuildSettingsScene(p, true)).ToArray();
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

        private static string BuildTitle()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera(new Vector3(ViewWidth * 0.5f * Px, ViewHeight * 0.5f * Px, -10f));

            // titlescreen.room.gmx: bg_city_black tiled, hspeed -2, under bg_titlescreen.
            var city = new GameObject("bg_city_black (scrolling)", typeof(SpriteRenderer));
            var citySr = city.GetComponent<SpriteRenderer>();
            citySr.sprite = ImportSprite($"{BackgroundDir}/bg_city_black.png", 0, 0, tiled: true);
            citySr.drawMode = SpriteDrawMode.Tiled;
            citySr.size = new Vector2(ViewWidth * 3f * Px, ViewHeight * Px);
            citySr.sortingOrder = BackgroundOrder;
            city.transform.position = new Vector3(0f, ViewHeight * Px, 0f);
            city.AddComponent<ScrollingBackground>().Configure(-2f);

            var title = new GameObject("bg_titlescreen", typeof(SpriteRenderer));
            var titleSr = title.GetComponent<SpriteRenderer>();
            titleSr.sprite = ImportSprite($"{BackgroundDir}/bg_titlescreen.png", 0, 0);
            titleSr.sortingOrder = BackgroundOrder + 1;
            title.transform.position = new Vector3(0f, ViewHeight * Px, 0f);

            new GameObject("Title Screen").AddComponent<TitleScreen>().Configure(Street00, LoadClip("music_oppressivegloom.ogg"));

            string path = $"{RoomsDir}/{TitleScene}.unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        // ---------------------------------------------------------------- rooms

        private static string BuildRoom(RoomSpec spec)
        {
            var scene = EditorSceneManager.OpenScene(SandboxPath, OpenSceneMode.Single);

            // Strip the sandbox-only stand-ins.
            var remove = new HashSet<string> { "Ground", "Markers", "Console Targets" };
            if (!spec.KeepNpc)
            {
                remove.Add("NPC");
                remove.Add("NPC Suspicion Label");
            }
            foreach (var root in scene.GetRootGameObjects())
            {
                if (remove.Contains(root.name)) Object.DestroyImmediate(root);
            }

            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            var framing = Object.FindAnyObjectByType<CloseFramingCamera>();
            var cam = framing.GetComponent<Camera>();
            var cursor = Object.FindAnyObjectByType<EyeCursor>();
            var weapon = Object.FindAnyObjectByType<PlayerWeapon>();
            var prompt = Object.FindAnyObjectByType<CommandPrompt>();
            var border = AssetDatabase.LoadAssetAtPath<Texture2D>($"{ArtDir}/spr_desc_border.png");

            // Player at its room start; the camera rests on the room's full 1080px height.
            player.transform.position = new Vector3(spec.PlayerStartX * Px, PlayerOriginAboveFloor * Px, player.transform.position.z);
            cam.transform.position = new Vector3(spec.PlayerStartX * Px, ViewHeight * 0.5f * Px, cam.transform.position.z);
            cam.backgroundColor = Color.black; // street00's bg_black
            if (cam.GetComponent<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();
            var framingSo = new SerializedObject(framing);
            framingSo.FindProperty("roomMinX").floatValue = 0f;
            framingSo.FindProperty("roomMaxX").floatValue = spec.Width * Px;
            framingSo.ApplyModifiedPropertiesWithoutUndo();

            // Audio: footsteps + whoosh (obj_walker), weapon sounds (toggle_gun / shoot_bullet).
            var playerAudioSource = GetOrAdd<AudioSource>(player.gameObject);
            playerAudioSource.playOnAwake = false;
            var playerAudio = GetOrAdd<PlayerAudio>(player.gameObject);
            var audioSo = new SerializedObject(playerAudio);
            audioSo.FindProperty("audioSource").objectReferenceValue = playerAudioSource;
            var steps = audioSo.FindProperty("footsteps");
            steps.arraySize = 16;
            for (int i = 0; i < 16; i++) steps.GetArrayElementAtIndex(i).objectReferenceValue = LoadClip($"sfx_step_concrete{i + 1:00}.wav");
            audioSo.FindProperty("whoosh").objectReferenceValue = LoadClip("sfx_whoosh.wav");
            audioSo.ApplyModifiedPropertiesWithoutUndo();

            var weaponSo = new SerializedObject(weapon);
            weaponSo.FindProperty("reloadClip").objectReferenceValue = LoadClip("sfx_reload.wav");
            weaponSo.FindProperty("holsterClip").objectReferenceValue = LoadClip("sfx_holster.wav");
            weaponSo.FindProperty("whooshClip").objectReferenceValue = LoadClip("sfx_whoosh.wav");
            weaponSo.FindProperty("fireClip").objectReferenceValue = LoadClip("sfx_fire_cannon.wav");
            weaponSo.FindProperty("room").rectValue = new Rect(0f, 0f, spec.Width * Px, ViewHeight * Px);
            weaponSo.ApplyModifiedPropertiesWithoutUndo();

            if (prompt != null)
            {
                var typeSource = GetOrAdd<AudioSource>(prompt.gameObject);
                typeSource.playOnAwake = false;
                var promptSo = new SerializedObject(prompt);
                promptSo.FindProperty("typeSource").objectReferenceValue = typeSource;
                promptSo.FindProperty("typeClip").objectReferenceValue = LoadClip("sfx_type.wav");
                promptSo.ApplyModifiedPropertiesWithoutUndo();
            }

            // The room: background slices (tiles at depth 1000000), left to right.
            var backdrop = new GameObject("Background");
            for (int i = 0; i < spec.Slices.Length; i++)
            {
                var slice = new GameObject(spec.Slices[i], typeof(SpriteRenderer));
                slice.transform.SetParent(backdrop.transform, false);
                var sr = slice.GetComponent<SpriteRenderer>();
                sr.sprite = ImportSprite($"{BackgroundDir}/{spec.Slices[i]}.png", 0, 0);
                sr.sortingOrder = BackgroundOrder;
                slice.transform.position = GmToWorld(i * ViewWidth, 0f);
            }

            // obj_room + obj_music.
            var fade = new GameObject("Fade").AddComponent<RoomFade>();
            var room = new GameObject("Room").AddComponent<Room>();
            var roomSo = new SerializedObject(room);
            roomSo.FindProperty("displayName").stringValue = spec.Name;
            roomSo.FindProperty("roomWidth").floatValue = spec.Width;
            roomSo.FindProperty("playerWidth").floatValue = 512f;
            roomSo.FindProperty("player").objectReferenceValue = player;
            roomSo.FindProperty("music").objectReferenceValue = LoadClip($"{spec.Music}.ogg");
            roomSo.FindProperty("openingText").stringValue = spec.OpeningText ?? string.Empty;
            roomSo.FindProperty("openingTextPosition").vector2Value = spec.OpeningTextPosition;
            roomSo.FindProperty("descriptionBorder").objectReferenceValue = border;
            roomSo.FindProperty("viewCamera").objectReferenceValue = cam;
            roomSo.ApplyModifiedPropertiesWithoutUndo();

            // Props: lampposts (behind, with their flickering pooled light) and mailboxes (in front).
            var props = new GameObject("Props");
            var lamppost = ImportSprite($"{ArtDir}/spr_lamppost.png", 64, 0);
            var lamplight = ImportSprite($"{ArtDir}/spr_lamplight01.png", 800, 800);
            var mailbox = ImportSprite($"{ArtDir}/spr_mailbox.png", 0, 0);
            foreach (float x in spec.Lampposts)
            {
                var post = CreateProp(props.transform, "Lamppost", lamppost, LamppostOrder, x, 0f,
                    "It's a lamp post.\nNothing sinister about that...", cursor, cam, border);
                var glow = new GameObject("Lamplight", typeof(SpriteRenderer));
                glow.transform.SetParent(post.transform, false);
                glow.transform.position = GmToWorld(x, -100f);
                var glowSr = glow.GetComponent<SpriteRenderer>();
                glowSr.sprite = lamplight;
                glowSr.sortingOrder = LamplightOrder;
                post.AddComponent<LampFlicker>().Configure(glowSr);
            }
            foreach (float x in spec.Mailboxes)
            {
                CreateProp(props.transform, "Mailbox", mailbox, FrontPropOrder, x, 800f,
                    "It's just a mailbox.\n...or is it?", cursor, cam, border);
            }

            if (spec.Rain)
            {
                new GameObject("Rain").AddComponent<RainEmitter>()
                    .Configure(ImportSprite($"{ArtDir}/spr_rain.png", 0, 0), cam, RainOrder);
            }

            // Exits.
            var exits = new GameObject("Exits");
            foreach (var e in spec.SideExits)
            {
                var go = new GameObject($"Exit Side x={e.X:0} -> {e.To}");
                go.transform.SetParent(exits.transform, false);
                go.AddComponent<SideExit>().Configure(e.X, e.To, e.Enter, e.EnterX, room, fade);
            }
            var icon = ImportSprite($"{ArtDir}/spr_icon_exit.png", 32, 32);
            var doorClip = LoadClip("sfx_door_open.wav");
            foreach (var d in spec.Doors)
            {
                var go = new GameObject($"Exit {d.Label} -> {d.To}", typeof(SpriteRenderer));
                go.transform.SetParent(exits.transform, false);
                var sr = go.GetComponent<SpriteRenderer>();
                sr.sprite = icon;
                sr.sortingOrder = ExitOrder;
                sr.color = new Color(1f, 1f, 1f, 0f);
                go.transform.position = GmToWorld(d.Position.x, d.Position.y);
                go.AddComponent<DoorExit>().Configure(d.Position, d.Label, d.To, d.Enter, 0f, room, fade, playerAudioSource, doorClip, cam);
            }
            foreach (float x in spec.Barriers)
            {
                var go = new GameObject($"Barrier x={x:0}");
                go.transform.SetParent(exits.transform, false);
                go.AddComponent<RoomBarrier>().Configure(x, room);
            }

            string path = $"{RoomsDir}/{spec.Scene}.unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        private static GameObject CreateProp(Transform parent, string name, Sprite sprite, int order, float x, float y,
            string description, EyeCursor cursor, Camera cam, Texture2D border)
        {
            var go = new GameObject($"{name} x={x:0}", typeof(SpriteRenderer));
            go.transform.SetParent(parent, false);
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            go.transform.position = GmToWorld(x, y);
            go.AddComponent<BoxCollider2D>(); // after the sprite, so it fits the sprite's bounds
            go.AddComponent<PropDescription>().Configure(description, cursor, cam, border);
            return go;
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            // Not ?? : in the editor a missing component comes back as a fake-null object.
            var existing = go.GetComponent<T>();
            return existing != null ? existing : go.AddComponent<T>();
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

        /// <summary>A GameMaker room point (px from the top-left, y down) in world units: the
        /// room's floor (y = 1080) sits at world y = 0, as in the sandbox.</summary>
        private static Vector3 GmToWorld(float x, float y) => new Vector3(x * Px, (ViewHeight - y) * Px, 0f);

        // ---------------------------------------------------------------- assets

        /// <summary>
        /// Imports one of the original images copied verbatim into Art/Original with its pivot at
        /// GameMaker's origin (from the top-left), 1 original px = 1/32 unit, point-filtered.
        /// Compressed: the painted 1920x1080 street slices would be 8MB each uncompressed.
        /// </summary>
        private static Sprite ImportSprite(string path, int originX, int originY, bool tiled = false)
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
            settings.spritePixelsPerUnit = 1f / Px;
            settings.filterMode = FilterMode.Point;
            settings.mipmapEnabled = false;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.wrapMode = tiled ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            importer.SetTextureSettings(settings);
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>Music streams (Vorbis); short effects decompress on load.</summary>
        private static void ImportAudio()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { AudioDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (AudioImporter)AssetImporter.GetAtPath(path);
                bool music = Path.GetFileName(path).StartsWith("music_");
                var settings = importer.defaultSampleSettings;
                settings.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }
        }

        private static AudioClip LoadClip(string file)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioDir}/{file}");
            if (clip == null) throw new FileNotFoundException($"Missing original audio {AudioDir}/{file}");
            return clip;
        }
    }
}
