using System.IO;
using Incognito.CameraRig;
using Incognito.Eye;
using Incognito.Hacking;
using Incognito.Npc;
using Incognito.Player;
using Incognito.Terminal;
using Incognito.Weapon;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Incognito.EditorTools
{
    /// <summary>
    /// Builds Assets/Incognito/Scenes/PlayerSandbox.unity: a flat-ground street00-sized scene to exercise <see cref="PlayerLocomotion"/>,
    /// <see cref="CloseFramingCamera"/>, <see cref="PlayerEyeball"/>, <see cref="EyeCursor"/> and
    /// the typed <see cref="CommandPrompt"/> behind the <see cref="HackingOverlay"/>, and
    /// <see cref="PlayerWeapon"/>, plus a patrolling
    /// (and shootable) <see cref="NpcController"/>, in
    /// Play mode. The player, NPC, arm and bullet art is the original 2013 sprites. Safe to re-run
    /// (overwrites the scene and re-applies the original sprites' import settings).
    /// </summary>
    public static class SandboxSceneBuilder
    {
        private const string SceneDir = "Assets/Incognito/Scenes";
        private const string ScenePath = SceneDir + "/PlayerSandbox.unity";
        private const string ArtDir = "Assets/Incognito/Art/Placeholder";
        private const string OriginalArtDir = "Assets/Incognito/Art/Original";

        // Street backdrop sits behind everything, including the bullet (obj_bullet depth 5).
        private const int BackdropOrder = -10;

        [MenuItem("Incognito/Build Player Sandbox Scene")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var groundSprite = CreateSolidSpriteAsset(ArtDir + "/placeholder_ground.png", new Color(0.35f, 0.35f, 0.35f));
            // The original character art at its GameMaker origins (xorig/yorigin from each .sprite.gmx).
            var playerSprite = ImportOriginal("spr_player", 256, 168);
            var npcSprite = ImportOriginal("spr_npc_ia", 256, 440);

            // Room sized like the original street00 (11520x1080 px) at the placeholder
            // 1px = 1/32 unit scale: 360 units wide, ground top at y = 0.
            const float px = PlayerLocomotion.WorldUnitsPerOriginalPixel;
            const float roomWidth = 11520f * px;
            const float viewHeight = 1080f * px;

            var ground = new GameObject("Ground", typeof(SpriteRenderer));
            ground.GetComponent<SpriteRenderer>().sprite = groundSprite;
            ground.GetComponent<SpriteRenderer>().sortingOrder = BackdropOrder;
            ground.transform.localScale = new Vector3(roomWidth, 2f, 1f);
            ground.transform.position = new Vector3(roomWidth * 0.5f, -1f, 0f);

            // Street markers every 10 units so horizontal camera motion is visible.
            var markers = new GameObject("Markers");
            for (float x = 0f; x <= roomWidth; x += 10f)
            {
                var marker = new GameObject($"Marker {x:0}", typeof(SpriteRenderer));
                marker.GetComponent<SpriteRenderer>().sprite = groundSprite;
                marker.GetComponent<SpriteRenderer>().sortingOrder = BackdropOrder;
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

            // obj_arm x2 (U.INC-T9): arm1 (support) in front of the body (depth -1), arm2 (gun)
            // behind it (depth 1, also behind the eye stack), on the original sprites at their GM
            // origins so the pivots and muzzle line up.
            var armRest = ImportOriginal("spr_arm", 100, 60);
            var supportRaise = ImportOriginal("spr_arm_left_aim1", 320, 142);
            var supportHold = ImportOriginal("spr_arm_left_aim2", 500, 86);
            var gunDrawing = ImportOriginal("spr_arm_right_drawing", 320, 142);
            var gunAim = ImportOriginal("spr_arm_right_aim", 500, 100);
            var gunFire = ImportOriginal("spr_arm_right_fire", 500, 100);
            var bulletSprite = ImportOriginal("spr_bullet", 6, 6);

            var supportArm = CreateArm("Support Arm (arm1)", player.transform, armRest, 1);
            var gunArm = CreateArm("Gun Arm (arm2)", player.transform, armRest, -4);

            var weapon = player.AddComponent<PlayerWeapon>();
            var audio = player.AddComponent<AudioSource>();
            audio.playOnAwake = false;

            // One patrolling NPC at the original street00 placement (x = 2400). The original's
            // only waypoint was x = 12000, past the room's end; the sandbox patrols the street
            // instead so the NPC keeps crossing the player's path. Like obj_walker, obj_npc's
            // initialY = room_height - sprite_height/2 puts its origin 512px above the floor.
            var npc = new GameObject("NPC", typeof(SpriteRenderer));
            npc.GetComponent<SpriteRenderer>().sprite = npcSprite;
            npc.transform.position = new Vector3(2400f * px, 512f * px, 0.5f);
            npc.AddComponent<BoxCollider2D>(); // after the sprite, so it fits the sprite's bounds
            npc.AddComponent<BulletTarget>();  // U.INC-T9: a shot shoves the NPC 100px
            var npcController = npc.AddComponent<NpcController>();
            npcController.Player = locomotion;

            var label = new GameObject("NPC Suspicion Label", typeof(TextMesh));
            var text = label.GetComponent<TextMesh>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.GetComponent<MeshRenderer>().sharedMaterial = text.font.material;
            label.GetComponent<MeshRenderer>().sortingOrder = 10;
            text.text = "0%";
            text.fontSize = 48;
            text.characterSize = 0.3f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = Color.white;
            label.transform.position = new Vector3(npc.transform.position.x, npc.transform.position.y + 200f * px, -1f);

            var npcSo = new SerializedObject(npcController);
            var route = npcSo.FindProperty("waypoints");
            route.arraySize = 2;
            route.GetArrayElementAtIndex(0).floatValue = 9000f;
            route.GetArrayElementAtIndex(1).floatValue = 2400f;
            npcSo.FindProperty("suspicionLabel").objectReferenceValue = text;
            npcSo.ApplyModifiedPropertiesWithoutUndo();

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

            // Look/talk clicks on the NPC (U.INC-T6): the wheel cycles the cursor; a look click
            // types obj_npc's description, a talk click walks the NPC over to the player.
            var interaction = npc.AddComponent<NpcInteraction>();
            interaction.Cursor = cursor;
            interaction.Description = "A young woman with tell tale red eyes and a meandering gait.\nBless the young and stoned.";
            var interactionSo = new SerializedObject(interaction);
            interactionSo.FindProperty("viewCamera").objectReferenceValue = cam;
            interactionSo.FindProperty("descriptionBorder").objectReferenceValue = ImportOriginal("spr_desc_border", 0, 0).texture;
            interactionSo.ApplyModifiedPropertiesWithoutUndo();

            // Weapon (U.INC-T9): Z draws/holsters, hold right button to aim, left button fires.
            var weaponSo = new SerializedObject(weapon);
            weaponSo.FindProperty("supportArm").objectReferenceValue = supportArm;
            weaponSo.FindProperty("gunArm").objectReferenceValue = gunArm;
            weaponSo.FindProperty("restArm").objectReferenceValue = armRest;
            weaponSo.FindProperty("supportAimRaise").objectReferenceValue = supportRaise;
            weaponSo.FindProperty("supportAimHold").objectReferenceValue = supportHold;
            weaponSo.FindProperty("gunDrawing").objectReferenceValue = gunDrawing;
            weaponSo.FindProperty("gunAim").objectReferenceValue = gunAim;
            weaponSo.FindProperty("gunFire").objectReferenceValue = gunFire;
            weaponSo.FindProperty("bulletSprite").objectReferenceValue = bulletSprite;
            weaponSo.FindProperty("bulletSortingOrder").intValue = -5; // obj_bullet depth 5: behind the player
            weaponSo.FindProperty("audioSource").objectReferenceValue = audio;
            weaponSo.FindProperty("eyeCursor").objectReferenceValue = cursor;
            weaponSo.FindProperty("playerEyeball").objectReferenceValue = eye;
            // Bullets die when they leave the room: the street's width, and the resting view's height.
            weaponSo.FindProperty("room").rectValue = new Rect(0f, -2f, roomWidth, viewHeight);
            weaponSo.ApplyModifiedPropertiesWithoutUndo();

            // Typed console (U.INC-T10), drawn on the original spr_console_selected. Placeholder
            // console targets around the player's start: two civilians and a camera in vicinity
            // (one view width), and one civilian far down the street that `list sheep` must not see.
            var console = new GameObject("Command Prompt");
            var prompt = console.AddComponent<CommandPrompt>();
            prompt.Origin = player.transform;
            var promptSo = new SerializedObject(prompt);
            promptSo.FindProperty("panelArt").objectReferenceValue = ImportOriginal("spr_console_selected", 0, 0).texture;
            promptSo.ApplyModifiedPropertiesWithoutUndo();

            // Hacking overlay (U.INC-T7): Q toggles the console view (pointer cursor, circuit
            // backdrop, camera on the cursor); a pointer click on the NPC raises the program icon,
            // opens the console and centres the camera on the NPC.
            var hacking = new GameObject("Hacking Overlay").AddComponent<HackingOverlay>();
            hacking.Cursor = cursor;
            hacking.Prompt = prompt;
            hacking.ViewCamera = framing;
            var hackingSo = new SerializedObject(hacking);
            hackingSo.FindProperty("backdrop").objectReferenceValue = ImportOriginal("spr_circuitwall1", 0, 0).texture;
            hackingSo.FindProperty("programIcon").objectReferenceValue = ImportOriginal("spr_program", 0, 0).texture;
            hackingSo.FindProperty("programIconActive").objectReferenceValue = ImportOriginal("spr_program_active", 0, 0).texture;
            hackingSo.ApplyModifiedPropertiesWithoutUndo();

            var targets = new GameObject("Console Targets");
            AddConsoleTarget(targets.transform, groundSprite, ConsoleObjectType.Sheep, "0097", roomWidth * 0.5f + 15f);
            AddConsoleTarget(targets.transform, groundSprite, ConsoleObjectType.Sheep, "0102", roomWidth * 0.5f - 25f);
            AddConsoleTarget(targets.transform, groundSprite, ConsoleObjectType.Camera, "07", roomWidth * 0.5f + 40f);
            AddConsoleTarget(targets.transform, groundSprite, ConsoleObjectType.Sheep, "0451", roomWidth * 0.5f + 120f);

            if (!AssetDatabase.IsValidFolder(SceneDir))
            {
                AssetDatabase.CreateFolder("Assets/Incognito", "Scenes");
            }

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Sets the import of one of the original sprites copied verbatim into Art/Original (eye
        /// stack, cursors, characters, arms and bullet) so its pivot is GameMaker's origin (xorig/yorigin from its
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

        private static void AddConsoleTarget(Transform parent, Sprite sprite, ConsoleObjectType type, string id, float x)
        {
            var go = new GameObject($"{type} {id}", typeof(SpriteRenderer));
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = type == ConsoleObjectType.Sheep ? new Color(0.8f, 0.3f, 0.3f) : new Color(0.3f, 0.6f, 0.9f);
            go.transform.SetParent(parent);
            go.transform.localScale = type == ConsoleObjectType.Sheep ? new Vector3(4f, 8f, 1f) : new Vector3(1.5f, 1.5f, 1f);
            go.transform.position = new Vector3(x, type == ConsoleObjectType.Sheep ? 4f : 14f, 0.5f);
            go.AddComponent<ConsoleTarget>().Configure(type, id);
        }

        private static SpriteRenderer CreateArm(string name, Transform player, Sprite sprite, int sortingOrder)
        {
            var arm = new GameObject(name, typeof(SpriteRenderer));
            arm.transform.SetParent(player, false);
            var sr = arm.GetComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;
            return sr;
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
            // Single, not the importer's default Multiple-with-no-slices, or LoadAssetAtPath<Sprite>
            // returns null and every placeholder renders invisible.
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = size;
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
