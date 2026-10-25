// NUnit 4 moved the classic Assert; the tests were written against it (Unity Test Framework).
using Assert = NUnit.Framework.Legacy.ClassicAssert;
using NUnit.Framework.Legacy;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Incognito.Content;
using NUnit.Framework;

namespace Incognito.Tests
{
    /// <summary>
    /// U.INC-T29: the asset parity sweep. Every sprite and background a live original object or room
    /// draws (or collides with) is in the shipped game-content.json Art catalog at its GameMaker
    /// origin and size, and its image is actually in Assets/Incognito/Art/Original; and every one of
    /// the original's 88 sprites and 21 backgrounds is accounted for as live, imported-but-unused, or
    /// dead with the reason. Liveness was read from the 09-05-13 source: the objects the four rooms
    /// in play place, the objects start_game.gml and live code create, each one's sprite_index (drawn
    /// unless its Draw event replaces draw_self) and every sprite its uncommented code draws or
    /// assigns, plus each room's visible backgrounds. Reads the real shipped files, not a fixture;
    /// when the original project is on this machine, also diffs the transcription against it.
    /// </summary>
    public class OriginalArtParityTests
    {
        private const string OriginalRoot = @"R:\Projects\_PRE_2022\_PRE_2021\_PRE_2020\_PRE_2017\gmlIncognito\09-05-13_2.19am";

        private struct Gm
        {
            public int W, H, Ox, Oy;
            public string Source;
            public Gm(int w, int h, int ox, int oy, string source) { W = w; H = h; Ox = ox; Oy = oy; Source = source; }
        }

        /// <summary>Every image a live original object or room uses: size and origin from its
        /// .sprite.gmx/.background.gmx, and who uses it.</summary>
        private static readonly Dictionary<string, Gm> LiveArt = new Dictionary<string, Gm>
        {
            { "spr_player", new Gm(512, 1024, 256, 168, "obj_walker") },
            { "spr_arm", new Gm(219, 379, 100, 60, "obj_arm, toggle_gun.gml") },
            { "spr_arm_left_aim1", new Gm(380, 258, 320, 142, "obj_walker aiming") },
            { "spr_arm_left_aim2", new Gm(550, 137, 500, 86, "obj_walker aiming") },
            { "spr_arm_right_drawing", new Gm(380, 258, 320, 142, "toggle_gun.gml") },
            { "spr_arm_right_aim", new Gm(550, 137, 500, 100, "obj_walker aiming") },
            { "spr_arm_right_fire", new Gm(550, 137, 500, 100, "obj_walker firing") },
            { "spr_bullet", new Gm(12, 12, 6, 6, "obj_bullet") },
            { "spr_eyewhite", new Gm(32, 20, 0, 0, "obj_eye, obj_walker") },
            { "spr_eye_ball", new Gm(11, 11, 5, 5, "obj_eye, obj_walker") },
            { "spr_eyelid_left", new Gm(32, 20, 0, 0, "obj_eye, obj_walker") },
            { "spr_eyelid_right", new Gm(32, 20, 0, 0, "obj_eye") },
            { "spr_cursor_eye", new Gm(32, 32, 16, 16, "obj_mouse") },
            { "spr_cursor_talk", new Gm(32, 32, 16, 16, "obj_mouse") },
            { "spr_cursor_pointer", new Gm(94, 64, 0, 0, "obj_mouse") },
            { "spr_cursor_reticle", new Gm(32, 32, 16, 16, "obj_mouse") },
            { "spr_cursor_arrow", new Gm(94, 64, 47, 32, "obj_mouse_gui GUI draw") },
            { "spr_npc_ia", new Gm(512, 1024, 256, 440, "obj_npc") },
            { "spr_brain_select", new Gm(200, 115, 100, 72, "obj_npc draw while inverted") },
            { "spr_desc_border", new Gm(1211, 853, 0, 0, "obj_typewriter draw") },
            { "spr_circuitwall1", new Gm(1920, 1080, 0, 0, "obj_gui_console") },
            { "spr_stealthwall", new Gm(1920, 1080, 0, 0, "obj_gui_menu GUI draw") },
            { "spr_program", new Gm(113, 86, 0, 0, "obj_program") },
            { "spr_program_active", new Gm(113, 86, 0, 0, "obj_npc pointer click") },
            { "spr_console", new Gm(580, 200, 0, 0, "obj_console") },
            { "spr_console_selected", new Gm(580, 200, 0, 0, "obj_npc pointer click") },
            { "spr_icon_pistol", new Gm(96, 96, 0, 0, "obj_icon_pistol, toggle_gun.gml") },
            { "spr_icon_pistol_selected", new Gm(96, 96, 0, 0, "toggle_gun.gml") },
            { "spr_black", new Gm(32, 32, 0, 0, "obj_fade") },
            { "spr_icon_exit", new Gm(64, 64, 32, 32, "obj_exit") },
            { "spr_exit_side", new Gm(10, 1080, 0, 0, "obj_exit_side collision mask") },
            { "spr_barrier", new Gm(10, 1080, 0, 0, "obj_barrier collision mask") },
            { "spr_lamppost", new Gm(128, 1080, 64, 0, "obj_prop_lamppost") },
            { "spr_lamplight01", new Gm(1600, 1600, 800, 800, "obj_prop_lamppost draw") },
            { "spr_mailbox", new Gm(363, 515, 0, 0, "obj_prop_mailbox") },
            { "spr_rain", new Gm(1, 4, 0, 0, "obj_raindrop") },
            { "bg_titlescreen", new Gm(1920, 1080, 0, 0, "titlescreen room") },
            { "bg_city_black", new Gm(1920, 1080, 0, 0, "titlescreen room") },
            { "bg_black", new Gm(32, 32, 0, 0, "street00 room") },
            { "bg_street00_00", new Gm(1920, 1080, 0, 0, "street00 room") },
            { "bg_street00_01", new Gm(1920, 1080, 0, 0, "street00 room") },
            { "bg_street00_02", new Gm(1920, 1080, 0, 0, "street00 room") },
            { "bg_street00_03", new Gm(1920, 1080, 0, 0, "street00 room") },
            { "bg_street00_04", new Gm(1920, 1080, 0, 0, "street00 room") },
            { "bg_street00_05", new Gm(1920, 1080, 0, 0, "street00 room") },
            { "bg_street01_00", new Gm(1920, 1080, 0, 0, "street01 room") },
            { "bg_street01_01", new Gm(1920, 1080, 0, 0, "street01 room") },
            { "bg_street01_02", new Gm(1920, 1080, 0, 0, "street01 room") },
            { "bg_street01_03", new Gm(1920, 1080, 0, 0, "street01 room") },
            { "bg_street01_04", new Gm(1920, 1080, 0, 0, "street01 room") },
            { "bg_street01_05", new Gm(1920, 1080, 0, 0, "street01 room") },
            { "bg_street01_06", new Gm(1920, 1080, 0, 0, "street01 room") },
            { "bg_street01_07", new Gm(1920, 1080, 0, 0, "street01 room") },
            { "bg_shop_00", new Gm(1920, 1080, 0, 0, "shop room") },
            { "bg_shop_01", new Gm(1920, 1080, 0, 0, "shop room") },
            { "bg_shop_02", new Gm(1920, 1080, 0, 0, "shop room") },
        };

        /// <summary>Imported though no live original object uses them: their objects are never
        /// created, but the rebuild ports (or may port) those objects' roles.</summary>
        private static readonly Dictionary<string, Gm> ImportedNotLive = new Dictionary<string, Gm>
        {
            { "spr_command_prompt", new Gm(580, 200, 0, 0, "obj_command_prompt, never created") },
            { "spr_command_prompt_selected", new Gm(580, 200, 0, 0, "obj_command_prompt, never created") },
            { "spr_linewall", new Gm(1920, 1080, 0, 0, "obj_stealth, obj_fade_stealth, never created") },
        };

        /// <summary>The rest of the original's sprites and backgrounds, and why none of them is ever
        /// seen: each is the resource of an object no room places and nothing creates, a marker
        /// drawn outside its room, a sprite its object's Draw event never draws, or an unused
        /// <c>_new</c> / scenes.gml variant.</summary>
        private static readonly Dictionary<string, string> DeadArt = new Dictionary<string, string>
        {
            { "spr_arm_left_support", "named nowhere in code" },
            { "spr_arm_new", "unused _new variant" },
            { "spr_eye_ball_new", "unused _new variant" },
            { "spr_eyelid_left_new", "unused _new variant" },
            { "spr_eyelid_right_new", "unused _new variant" },
            { "spr_eyewhite_new", "unused _new variant" },
            { "spr_player_new", "unused _new variant" },
            { "spr_eye_white", "named nowhere in code (obj_eye uses spr_eyewhite)" },
            { "spr_eyelid", "named nowhere in code" },
            { "spr_jukebox", "obj_jukebox, created only by obj_room_street00 (placed in no room) and placed only in the unused gamescreen room" },
            { "spr_linewall2", "named nowhere in code" },
            { "spr_musicnote", "obj_music, placed at x -96: an editor marker drawn outside the room" },
            { "spr_rainemitter", "obj_rain_emitter, placed at x -96: an editor marker drawn outside the room" },
            { "spr_raindrop", "named nowhere in code (obj_raindrop uses spr_rain)" },
            { "spr_room_startup", "obj_room*'s sprite; obj_room's Draw event only draws the room name" },
            { "spr_typewriter", "obj_typewriter's sprite; its Draw event only draws spr_desc_border and text" },
            { "spr_program_cmd", "named nowhere in code" },
            { "spr_street00", "named nowhere in code" },
            { "spr_street00_full", "obj_background, which no room places" },
            { "spr_street00_full_new", "unused _new variant" },
            { "spr_street00_00", "scenes.gml, whose only call (globals.gml) is commented out; also obj_street00_00, never placed" },
            { "spr_street00_00_new", "unused _new variant" },
            { "spr_street00_01", "scenes.gml (never called); obj_street00_01 and obj_wall, never placed" },
            { "spr_street00_01_new", "unused _new variant" },
            { "spr_street00_02", "scenes.gml (never called); obj_street00_02, never placed" },
            { "spr_street00_02_new", "unused _new variant" },
            { "spr_street00_03", "scenes.gml (never called); obj_street00_03, never placed" },
            { "spr_street00_03_new", "unused _new variant" },
            { "spr_street00_04", "scenes.gml (never called); obj_street00_04, never placed" },
            { "spr_street00_04_new", "unused _new variant" },
            { "spr_street00_05", "scenes.gml (never called); obj_street00_05, never placed" },
            { "spr_street00_05_new", "unused _new variant" },
            { "spr_street00_06", "named nowhere in code" },
            { "spr_street00_07", "named nowhere in code" },
            { "spr_street00_08", "named nowhere in code" },
            { "spr_street01", "named nowhere in code" },
            { "spr_street01_00", "scenes.gml, never called" },
            { "spr_street01_01", "scenes.gml, never called" },
            { "spr_street01_02", "scenes.gml, never called" },
            { "spr_street01_03", "scenes.gml, never called" },
            { "spr_street01_04", "scenes.gml, never called" },
            { "spr_street01_05", "scenes.gml, never called" },
            { "spr_street01_06", "scenes.gml, never called" },
            { "spr_street01_07", "scenes.gml, never called" },
            { "spr_street02", "named nowhere in code" },
            { "spr_street03", "named nowhere in code" },
            { "spr_street04", "named nowhere in code" },
            { "spr_street05", "named nowhere in code" },
            { "spr_street06", "named nowhere in code" },
            { "bg_street00", "no room uses it" },
        };

        private static string repoRoot;
        private static GameContent content;

        [OneTimeSetUp]
        public void LoadShippedContent()
        {
            var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "Assets", "Incognito", "Resources", "Data", "game-content.json")))
                dir = dir.Parent;
            Assert.IsNotNull(dir, "Could not find the repo root (Assets/Incognito/Resources/Data/game-content.json) above the test directory.");
            repoRoot = dir.FullName;
            content = GameContent.FromJson(File.ReadAllText(Path.Combine(repoRoot, "Assets", "Incognito", "Resources", "Data", "game-content.json")));
        }

        private static string ArtDir => Path.Combine(repoRoot, "Assets", "Incognito", "Art", "Original");

        private static string ArtFile(ArtDefinition art) => Path.Combine(ArtDir, art.File.Replace('/', Path.DirectorySeparatorChar));

        /// <summary>A PNG's width and height, from its IHDR chunk (bytes 16-23, big-endian).</summary>
        private static (int W, int H) PngSize(string path)
        {
            byte[] header = new byte[24];
            using (FileStream stream = File.OpenRead(path))
                Assert.AreEqual(24, stream.Read(header, 0, 24), path + " is too short to be a PNG");
            Assert.AreEqual(0x89, header[0], path + " is not a PNG (a Git LFS pointer that was never pulled?)");
            Assert.AreEqual((byte)'P', header[1], path + " is not a PNG");
            int Be(int at) => (header[at] << 24) | (header[at + 1] << 16) | (header[at + 2] << 8) | header[at + 3];
            return (Be(16), Be(20));
        }

        private static void AssertCatalogued(string id, Gm gm, bool live)
        {
            ArtDefinition art = content.FindArt(id);
            Assert.IsNotNull(art, id + " (" + gm.Source + ") is not in game-content.json's Art");
            Assert.AreEqual(live, art.Live, id + "'s Live flag");
            Assert.AreEqual(gm.W, art.Width, id + "'s width");
            Assert.AreEqual(gm.H, art.Height, id + "'s height");
            Assert.AreEqual(gm.Ox, art.OriginX, id + "'s GameMaker x origin");
            Assert.AreEqual(gm.Oy, art.OriginY, id + "'s GameMaker y origin");
            Assert.IsTrue(File.Exists(ArtFile(art)), id + "'s file " + art.File + " is not in Art/Original");
            Assert.AreEqual((gm.W, gm.H), PngSize(ArtFile(art)), id + "'s image size on disk");
        }

        [Test]
        public void The_shipped_content_validates()
        {
            Assert.DoesNotThrow(() => content.Validate());
        }

        [Test]
        public void Every_image_a_live_original_object_or_room_uses_is_catalogued_at_its_origin_and_on_disk()
        {
            foreach (KeyValuePair<string, Gm> entry in LiveArt)
                AssertCatalogued(entry.Key, entry.Value, live: true);
        }

        [Test]
        public void The_never_created_objects_art_is_imported_and_catalogued_as_not_live()
        {
            foreach (KeyValuePair<string, Gm> entry in ImportedNotLive)
                AssertCatalogued(entry.Key, entry.Value, live: false);
        }

        [Test]
        public void The_catalog_is_exactly_the_live_and_imported_art()
        {
            var expected = new HashSet<string>(LiveArt.Keys.Concat(ImportedNotLive.Keys), StringComparer.OrdinalIgnoreCase);
            foreach (ArtDefinition art in content.Art)
            {
                Assert.IsTrue(expected.Contains(art.Id), art.Id + " is catalogued but is neither live nor a ported object's art");
                Assert.IsFalse(DeadArt.ContainsKey(art.Id), art.Id + " is catalogued but the original never shows it");
            }
            Assert.AreEqual(expected.Count, content.Art.Length);
        }

        [Test]
        public void Every_image_in_Art_Original_is_catalogued()
        {
            var catalogued = new HashSet<string>(content.Art.Select(a => a.File), StringComparer.OrdinalIgnoreCase);
            foreach (string file in Directory.GetFiles(ArtDir, "*.png", SearchOption.AllDirectories))
            {
                string relative = file.Substring(ArtDir.Length + 1).Replace(Path.DirectorySeparatorChar, '/');
                Assert.IsTrue(catalogued.Contains(relative), relative + " is in Art/Original but not in game-content.json's Art");
            }
        }

        [Test]
        public void Every_room_background_slice_is_catalogued_live_art()
        {
            foreach (RoomDefinition room in content.Rooms)
                foreach (string slice in room.BackgroundSlices)
                {
                    ArtDefinition art = content.FindArt(slice);
                    Assert.IsNotNull(art, room.Id + "'s slice " + slice);
                    Assert.IsTrue(art.Live, slice);
                    StringAssert.StartsWith("Backgrounds/", art.File, slice);
                }
        }

        [Test]
        public void Only_the_title_skyline_tiles()
        {
            foreach (ArtDefinition art in content.Art)
                Assert.AreEqual(art.Id == "bg_city_black", art.Tiled, art.Id + "'s Tiled flag");
        }

        // ------------------------------------------------------------ diff against the original

        private static string OriginalOrIgnore()
        {
            if (!Directory.Exists(Path.Combine(OriginalRoot, "sprites")))
                Assert.Ignore("The original 09-05-13 project is not on this machine (" + OriginalRoot + "); the transcription above stands in for it.");
            return OriginalRoot;
        }

        private static IEnumerable<(string Id, string Gmx, string Image, bool Background)> OriginalResources(string root)
        {
            foreach (string gmx in Directory.GetFiles(Path.Combine(root, "sprites"), "*.sprite.gmx"))
            {
                string id = Path.GetFileName(gmx).Replace(".sprite.gmx", "");
                yield return (id, gmx, Path.Combine(root, "sprites", "images", id + "_0.png"), false);
            }
            foreach (string gmx in Directory.GetFiles(Path.Combine(root, "background"), "*.background.gmx"))
            {
                string id = Path.GetFileName(gmx).Replace(".background.gmx", "");
                yield return (id, gmx, Path.Combine(root, "background", "images", id + ".png"), true);
            }
        }

        private static int Tag(string xml, string tag)
        {
            Match m = Regex.Match(xml, "<" + tag + ">(-?\\d+)</" + tag + ">");
            Assert.IsTrue(m.Success, "no <" + tag + ">");
            return int.Parse(m.Groups[1].Value);
        }

        [Test]
        public void Every_original_sprite_and_background_is_live_imported_or_dead_and_none_is_both()
        {
            string root = OriginalOrIgnore();
            var original = OriginalResources(root).Select(r => r.Id).ToList();
            Assert.AreEqual(88 + 21, original.Count, "the 09-05-13 project's 88 sprites and 21 backgrounds");

            foreach (string id in original)
            {
                int buckets = (LiveArt.ContainsKey(id) ? 1 : 0) + (ImportedNotLive.ContainsKey(id) ? 1 : 0) + (DeadArt.ContainsKey(id) ? 1 : 0);
                Assert.AreEqual(1, buckets, id + " must be exactly one of live, imported-not-live, dead");
            }
            var originalSet = new HashSet<string>(original);
            foreach (string id in LiveArt.Keys.Concat(ImportedNotLive.Keys).Concat(DeadArt.Keys))
                Assert.IsTrue(originalSet.Contains(id), id + " is not one of the original's resources");
        }

        [Test]
        public void The_transcribed_sizes_and_origins_match_the_originals_gmx_and_the_images_are_verbatim()
        {
            string root = OriginalOrIgnore();
            foreach (var resource in OriginalResources(root))
            {
                Gm gm;
                if (!LiveArt.TryGetValue(resource.Id, out gm) && !ImportedNotLive.TryGetValue(resource.Id, out gm)) continue;

                string xml = File.ReadAllText(resource.Gmx);
                Assert.AreEqual(Tag(xml, "width"), gm.W, resource.Id + " width");
                Assert.AreEqual(Tag(xml, "height"), gm.H, resource.Id + " height");
                if (!resource.Background)
                {
                    Assert.AreEqual(Tag(xml, "xorig"), gm.Ox, resource.Id + " xorig");
                    Assert.AreEqual(Tag(xml, "yorigin"), gm.Oy, resource.Id + " yorigin");
                    Assert.AreEqual(1, Regex.Matches(xml, "<frame ").Count, resource.Id + " is a single-frame sprite");
                }

                ArtDefinition art = content.FindArt(resource.Id);
                CollectionAssert.AreEqual(File.ReadAllBytes(resource.Image), File.ReadAllBytes(ArtFile(art)),
                    resource.Id + "'s " + art.File + " is not a verbatim copy of the original's image");
            }
        }
    }
}
