// NUnit 4 moved the classic Assert; the tests were written against it (Unity Test Framework).
using Assert = NUnit.Framework.Legacy.ClassicAssert;
using Incognito.Content;
using Incognito.Npc;
using Incognito.Settings;
using NUnit.Framework;
using Unity.Shared.Core.Json;

namespace Incognito.Tests
{
    /// <summary>
    /// U.INC-T25: SettingsRegistry and GameContent now parse their own JSON with Unity.Shared.Core's
    /// engine-free reader instead of UnityEngine.JsonUtility, so this exercises that parsing from a
    /// plain C# test (something JsonUtility never allowed, since it needs UnityEngine). The shipped
    /// Resources/Data/*.json files are still proven sound by GameSettingsTests (Unity EditMode,
    /// since reading them needs Resources.Load); these tests are about the field mapping itself.
    /// </summary>
    public class SettingsRegistryFromJsonTests
    {
        private const string Full = @"{
            ""Suspicion"": { ""NearRate"": 0.4, ""MaxSuspicion"": 50.0 },
            ""Locomotion"": { ""WalkSpeed"": 7.0, ""Friction"": 0.5 },
            ""Camera"": { ""ViewWidth"": 1280.0 },
            ""Weapon"": { ""AimDivisor"": 15.0 },
            ""Rain"": { ""DropsPerStep"": 3, ""DriftMin"": -9, ""Alpha"": 0.5 }
        }";

        [Test]
        public void Present_fields_override_the_defaults()
        {
            var registry = SettingsRegistry.FromJson(Full);
            Assert.AreEqual(0.4f, registry.Suspicion.NearRate);
            Assert.AreEqual(50f, registry.Suspicion.MaxSuspicion);
            Assert.AreEqual(7f, registry.Locomotion.WalkSpeed);
            Assert.AreEqual(0.5f, registry.Locomotion.Friction);
            Assert.AreEqual(1280f, registry.Camera.ViewWidth);
            Assert.AreEqual(15f, registry.Weapon.AimDivisor);
            Assert.AreEqual(3, registry.Rain.DropsPerStep);
            Assert.AreEqual(-9, registry.Rain.DriftMin);
            Assert.AreEqual(0.5f, registry.Rain.Alpha);
        }

        [Test]
        public void Missing_fields_keep_their_class_defaults()
        {
            var registry = SettingsRegistry.FromJson(Full);
            // Suspicion.MidRate wasn't in the JSON above; the field initializer's value survives.
            Assert.AreEqual(new SuspicionTuning().MidRate, registry.Suspicion.MidRate);
            Assert.AreEqual(new CameraTuning().PlayerWidth, registry.Camera.PlayerWidth);
            Assert.AreEqual(new RainTuning().FallMax, registry.Rain.FallMax);
        }

        [Test]
        public void A_missing_category_still_reads_as_its_defaults_not_null()
        {
            var registry = SettingsRegistry.FromJson(@"{ ""Suspicion"": { ""NearRate"": 0.9 } }");
            Assert.AreEqual(0.9f, registry.Suspicion.NearRate);
            Assert.IsNotNull(registry.Locomotion);
            Assert.AreEqual(new LocomotionTuning().WalkSpeed, registry.Locomotion.WalkSpeed);
            Assert.DoesNotThrow(() => registry.Validate());
        }

        [Test]
        public void Bad_json_throws()
        {
            Assert.Throws<JsonFormatException>(() => SettingsRegistry.FromJson("not json"));
        }

        [Test]
        public void A_json_array_at_the_top_level_is_rejected()
        {
            Assert.Throws<JsonFormatException>(() => SettingsRegistry.FromJson("[1, 2, 3]"));
        }
    }

    public class GameContentFromJsonTests
    {
        private const string Full = @"{
            ""Rooms"": [
                {
                    ""Id"": ""Street00"",
                    ""DisplayName"": ""Bleecher Street"",
                    ""Width"": 11520.0,
                    ""BackgroundSlices"": [""bg_00"", ""bg_01""],
                    ""PlayerStartX"": 11008.0,
                    ""Lampposts"": [ { ""X"": 1632.0, ""Description"": ""A lamp post."" } ],
                    ""SideExits"": [ { ""X"": -32.0, ""To"": ""Shop"", ""Enter"": 1, ""EnterX"": 0.0 } ],
                    ""Doors"": [ { ""X"": 9504.0, ""Y"": 480.0, ""Label"": ""Pawn Shop"", ""To"": ""Shop"", ""Enter"": 2, ""EnterX"": 9500.0 } ],
                    ""Barriers"": [800.0],
                    ""Rain"": true,
                    ""KeepNpc"": true,
                    ""Music"": ""music_oppressivegloom"",
                    ""OpeningText"": ""Hello"",
                    ""OpeningTextX"": 10870.0,
                    ""OpeningTextY"": 100.0
                },
                {
                    ""Id"": ""Shop"",
                    ""DisplayName"": ""Simon's Pawn and Loan"",
                    ""Width"": 5760.0,
                    ""BackgroundSlices"": [""bg_shop""],
                    ""PlayerStartX"": 5248.0
                }
            ],
            ""Npcs"": [
                {
                    ""Id"": ""0097"",
                    ""SheepId"": ""0097"",
                    ""RoomId"": ""Street00"",
                    ""Description"": ""Someone."",
                    ""SpawnX"": 2400.0,
                    ""Waypoints"": [9000.0, 2400.0],
                    ""WalkSpeed"": 5.0,
                    ""ArriveRange"": 20.0
                }
            ],
            ""Console"": { ""NotRecognizedText"": ""Nope"" }
        }";

        [Test]
        public void Rooms_npcs_and_console_text_all_parse()
        {
            var content = GameContent.FromJson(Full);
            Assert.AreEqual(2, content.Rooms.Length);

            var street00 = content.FindRoom("Street00");
            Assert.IsNotNull(street00);
            Assert.AreEqual(11520f, street00.Width);
            Assert.AreEqual(new[] { "bg_00", "bg_01" }, street00.BackgroundSlices);
            Assert.IsTrue(street00.Rain);
            Assert.IsTrue(street00.KeepNpc);
            Assert.AreEqual(1, street00.Lampposts.Length);
            Assert.AreEqual("A lamp post.", street00.Lampposts[0].Description);
            Assert.AreEqual(new[] { 800f }, street00.Barriers);

            Assert.AreEqual(1, street00.SideExits.Length);
            Assert.AreEqual(RoomEntranceKind.Right, street00.SideExits[0].Enter);

            Assert.AreEqual(1, street00.Doors.Length);
            Assert.AreEqual(RoomEntranceKind.At, street00.Doors[0].Enter);
            Assert.AreEqual(9500f, street00.Doors[0].EnterX);

            var npc = content.FindNpc("0097");
            Assert.IsNotNull(npc);
            Assert.AreEqual("Street00", npc.RoomId);
            Assert.AreEqual(new[] { 9000f, 2400f }, npc.Waypoints);

            Assert.AreEqual("Nope", content.Console.NotRecognizedText);
        }

        [Test]
        public void A_room_missing_from_the_json_falls_back_to_defaults()
        {
            var content = GameContent.FromJson(Full);
            var shop = content.FindRoom("Shop");
            Assert.IsNotNull(shop);
            // Shop's JSON has no Lampposts/SideExits/Doors/Barriers: the field initializers' empty arrays survive.
            Assert.AreEqual(0, shop.Lampposts.Length);
            Assert.AreEqual(0, shop.SideExits.Length);
            Assert.AreEqual(0, shop.Doors.Length);
            Assert.AreEqual(0, shop.Barriers.Length);
        }

        [Test]
        public void Bad_json_throws()
        {
            Assert.Throws<JsonFormatException>(() => GameContent.FromJson("{not json"));
        }
    }
}
