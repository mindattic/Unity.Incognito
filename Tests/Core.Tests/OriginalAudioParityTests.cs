// NUnit 4 moved the classic Assert; the tests were written against it (Unity Test Framework).
using Assert = NUnit.Framework.Legacy.ClassicAssert;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Incognito.Content;
using NUnit.Framework;

namespace Incognito.Tests
{
    /// <summary>
    /// U.INC-T28: every sound a live original object or script plays (RECONSTRUCTION.md §8, read
    /// from the original's <c>audio_play_sound</c> calls that are not commented out) is in the
    /// shipped game-content.json catalog, wired, and its file is actually in
    /// Assets/Incognito/Audio/Original; and the whispers ambient the title only names in a
    /// commented-out line is imported and catalogued, but left unwired as in the original.
    /// Reads the real shipped files from the repo, not a fixture.
    /// </summary>
    public class OriginalAudioParityTests
    {
        /// <summary>
        /// Transcribed from the original 09-05-13 source: each live (uncommented)
        /// <c>audio_play_sound</c>, and the per-room <c>current_track</c> music the rebuild plays.
        /// obj_command_prompt's own sfx_flick call sits inside a <c>/* */</c> block, so obj_mouse_gui
        /// is sfx_flick's live player.
        /// </summary>
        private static readonly Dictionary<string, string> LivePlays = new Dictionary<string, string>
        {
            { "sfx_flick", "objects/obj_mouse_gui.object.gmx: program icon click" },
            { "sfx_type", "objects/obj_command_prompt.object.gmx, obj_console.object.gmx: keystroke" },
            { "sfx_door_open", "objects/obj_exit.object.gmx" },
            { "sfx_whoosh", "objects/obj_walker.object.gmx: turn around, draw to aim" },
            { "sfx_reload", "scripts/toggle_gun.gml" },
            { "sfx_holster", "scripts/toggle_gun.gml" },
            { "sfx_fire_cannon", "scripts/shoot_bullet.gml" },
            { "sfx_step_concrete01", "scripts/play_footstep.gml" },
            { "sfx_step_concrete02", "scripts/play_footstep.gml" },
            { "sfx_step_concrete03", "scripts/play_footstep.gml" },
            { "sfx_step_concrete04", "scripts/play_footstep.gml" },
            { "sfx_step_concrete05", "scripts/play_footstep.gml" },
            { "sfx_step_concrete06", "scripts/play_footstep.gml" },
            { "sfx_step_concrete07", "scripts/play_footstep.gml" },
            { "sfx_step_concrete08", "scripts/play_footstep.gml" },
            { "sfx_step_concrete09", "scripts/play_footstep.gml" },
            { "sfx_step_concrete10", "scripts/play_footstep.gml" },
            { "sfx_step_concrete11", "scripts/play_footstep.gml" },
            { "sfx_step_concrete12", "scripts/play_footstep.gml" },
            { "sfx_step_concrete13", "scripts/play_footstep.gml" },
            { "sfx_step_concrete14", "scripts/play_footstep.gml" },
            { "sfx_step_concrete15", "scripts/play_footstep.gml" },
            { "sfx_step_concrete16", "scripts/play_footstep.gml" },
            { "music_oppressivegloom", "obj_music current_track: street00, street01, titlescreen (default)" },
            { "music_dubferal", "obj_music current_track: shop" },
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

        private static string AudioFile(SoundDefinition sound) =>
            Path.Combine(repoRoot, "Assets", "Incognito", "Audio", "Original", sound.File);

        [Test]
        public void The_shipped_content_validates()
        {
            Assert.DoesNotThrow(() => content.Validate());
        }

        [Test]
        public void Every_sound_a_live_original_object_plays_is_catalogued_wired_and_on_disk()
        {
            foreach (KeyValuePair<string, string> play in LivePlays)
            {
                SoundDefinition sound = content.FindSound(play.Key);
                Assert.IsNotNull(sound, play.Key + " (played by " + play.Value + ") is not in game-content.json's Sounds");
                Assert.IsTrue(sound.Wired, play.Key + " is played live by " + play.Value + " but catalogued as unwired");
                Assert.IsTrue(File.Exists(AudioFile(sound)), play.Key + "'s file " + sound.File + " is not in Assets/Incognito/Audio/Original");
            }
        }

        [Test]
        public void Whispers_is_imported_and_catalogued_but_unwired_as_in_the_original()
        {
            SoundDefinition whispers = content.FindSound("whispers");
            Assert.IsNotNull(whispers, "whispers is not in game-content.json's Sounds");
            Assert.IsFalse(whispers.Wired, "the original only names whispers in a commented-out line");
            Assert.IsTrue(whispers.Streamed, "whispers.sound.gmx has <streamed>1</streamed>");
            Assert.IsTrue(File.Exists(AudioFile(whispers)), "whispers' file " + whispers.File + " is not in Audio/Original");

            Assert.AreEqual("whispers", content.TitleScreen.UnwiredTrack);
            Assert.AreEqual("music_oppressivegloom", content.TitleScreen.Music, "the title keeps obj_music's global default");
            Assert.IsFalse(content.Rooms.Any(r => r.Music == "whispers"), "no room plays whispers");
        }

        [Test]
        public void Every_catalogued_sound_has_its_file_on_disk()
        {
            foreach (SoundDefinition sound in content.Sounds)
                Assert.IsTrue(File.Exists(AudioFile(sound)), sound.Id + "'s file " + sound.File + " is not in Assets/Incognito/Audio/Original");
        }

        [Test]
        public void Every_sound_file_on_disk_is_catalogued()
        {
            string dir = Path.Combine(repoRoot, "Assets", "Incognito", "Audio", "Original");
            var catalogued = new HashSet<string>(content.Sounds.Select(s => s.File));
            foreach (string file in Directory.GetFiles(dir).Where(f => !f.EndsWith(".meta")))
                Assert.IsTrue(catalogued.Contains(Path.GetFileName(file)), Path.GetFileName(file) + " is in Audio/Original but not in game-content.json's Sounds");
        }

        [Test]
        public void Only_music_and_the_ambient_stream_as_in_the_original()
        {
            foreach (SoundDefinition sound in content.Sounds)
            {
                bool expected = sound.Id.StartsWith("music_") || sound.Id == "whispers";
                Assert.AreEqual(expected, sound.Streamed, sound.Id + "'s Streamed flag differs from its .sound.gmx");
            }
        }
    }
}
