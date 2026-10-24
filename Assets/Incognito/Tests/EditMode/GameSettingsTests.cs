using System.Linq;
using Incognito.Settings;
using Incognito.Terminal;
using NUnit.Framework;

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

        [Test]
        public void Tuning_is_cached_across_calls()
        {
            Assert.AreSame(GameSettings.Tuning, GameSettings.Tuning);
        }
    }
}
