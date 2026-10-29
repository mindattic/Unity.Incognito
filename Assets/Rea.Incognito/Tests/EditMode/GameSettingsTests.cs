using System.Linq;
using Incognito.Content;
using Incognito.EditorTools;
using Incognito.Settings;
using Incognito.Terminal;
using NUnit.Framework;
using UnityEngine;

namespace Incognito.Tests
{
    /// <summary>
    /// U.INC-T22: the shipped Resources/Data/*.json files parse and validate with no exception
    /// (proof that the production data, not just the schema, is sound), and a few load-bearing
    /// values round-trip as expected.
    /// </summary>
    public class GameSettingsTests
    {
        [SetUp]
        public void Reset() => GameSettings.Reset();

        [TearDown]
        public void Cleanup() => GameSettings.Reset();

        [Test]
        public void Tuning_loads_and_validates()
        {
            var tuning = GameSettings.Tuning;
            Assert.IsNotNull(tuning);
            Assert.AreEqual(1920f, tuning.Camera.ViewWidth);
            Assert.AreEqual(4f, tuning.Locomotion.WalkSpeed);
            Assert.AreEqual(0.75f, tuning.Rain.Alpha);
        }

        [Test]
        public void Content_loads_and_validates()
        {
            var content = GameSettings.Content;
            Assert.IsNotNull(content);
            Assert.AreEqual(3, content.Rooms.Length);

            var street00 = content.FindRoom("Street00");
            Assert.IsNotNull(street00);
            Assert.AreEqual(11520f, street00.Width);
            Assert.IsTrue(street00.KeepNpc);
            StringAssert.Contains("strange city", street00.OpeningText);

            var npc = content.Npcs.Single();
            Assert.AreEqual("Street00", npc.RoomId);
            Assert.AreEqual("0097", npc.SheepId);

            Assert.AreEqual(CommandInterpreter.NotRecognized, content.Console.NotRecognizedText);
        }

        /// <summary>U.INC-T28: every catalogued sound (including the new sfx_flick and the unwired
        /// whispers) is a real AudioClip Unity imported, streamed exactly where the original streamed.</summary>
        [Test]
        public void Every_catalogued_sound_imports_as_an_AudioClip()
        {
            foreach (SoundDefinition sound in GameSettings.Content.Sounds)
            {
                var clip = RoomSceneBuilder.LoadSound(sound.Id);
                Assert.IsNotNull(clip, sound.Id);
                Assert.Greater(clip.length, 0f, $"{sound.Id} is empty");
                var importer = (UnityEditor.AudioImporter)UnityEditor.AssetImporter.GetAtPath(RoomSceneBuilder.SoundPath(sound));
                var expected = sound.Streamed ? UnityEngine.AudioClipLoadType.Streaming : UnityEngine.AudioClipLoadType.DecompressOnLoad;
                Assert.AreEqual(expected, importer.defaultSampleSettings.loadType, $"{sound.Id}'s load type");
            }
        }

        /// <summary>U.INC-T29: every catalogued image (including the sweep's new spr_icon_pistol(_selected),
        /// spr_exit_side, spr_barrier, spr_console, spr_command_prompt(_selected), spr_linewall,
        /// spr_brain_select, spr_cursor_arrow, spr_stealthwall, spr_black and bg_black) imports as one
        /// point-filtered sprite whose pivot is its GameMaker origin.</summary>
        [Test]
        public void Every_catalogued_image_imports_as_a_sprite_at_its_GameMaker_origin()
        {
            foreach (ArtDefinition art in GameSettings.Content.Art)
            {
                var sprite = OriginalArt.Import(art.Id);
                Assert.IsNotNull(sprite, art.Id);
                Assert.AreEqual(new Vector2(art.Width, art.Height), sprite.rect.size, $"{art.Id}'s size");
                // Sprite.pivot is in pixels from the bottom-left; GameMaker's origin is from the top-left.
                Assert.AreEqual(art.OriginX, sprite.pivot.x, 0.01f, $"{art.Id}'s x origin");
                Assert.AreEqual(art.Height - art.OriginY, sprite.pivot.y, 0.01f, $"{art.Id}'s y origin");
                Assert.AreEqual(32f, sprite.pixelsPerUnit, 0.001f, $"{art.Id}'s scale");
                Assert.AreEqual(FilterMode.Point, sprite.texture.filterMode, $"{art.Id}'s filter");
            }
        }

        [Test]
        public void Tuning_is_cached_across_calls()
        {
            Assert.AreSame(GameSettings.Tuning, GameSettings.Tuning);
        }
    }
}
