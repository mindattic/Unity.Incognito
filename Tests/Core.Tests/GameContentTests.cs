// NUnit 4 moved the classic Assert; the tests were written against it (Unity Test Framework).
using Assert = NUnit.Framework.Legacy.ClassicAssert;
using Incognito.Content;
using Incognito.Settings;
using NUnit.Framework;

namespace Incognito.Tests
{
    public class RoomDefinitionTests
    {
        private static RoomDefinition Valid() => new RoomDefinition
        {
            Id = "Street00",
            DisplayName = "Bleecher Street",
            Width = 11520f,
            BackgroundSlices = new[] { "bg_street00_00", "bg_street00_01" },
            PlayerStartX = 11008f,
        };

        [Test]
        public void A_well_formed_room_validates()
        {
            Assert.DoesNotThrow(() => Valid().Validate());
        }

        [Test]
        public void A_room_needs_an_id()
        {
            var room = Valid();
            room.Id = "";
            Assert.Throws<SettingsValidationException>(() => room.Validate());
        }

        [Test]
        public void Width_must_be_positive()
        {
            var room = Valid();
            room.Width = 0f;
            Assert.Throws<SettingsValidationException>(() => room.Validate());
        }

        [Test]
        public void At_least_one_background_slice_is_required()
        {
            var room = Valid();
            room.BackgroundSlices = new string[0];
            Assert.Throws<SettingsValidationException>(() => room.Validate());
        }

        [Test]
        public void Player_start_outside_the_room_is_rejected()
        {
            var room = Valid();
            room.PlayerStartX = 999999f;
            Assert.Throws<SettingsValidationException>(() => room.Validate());
        }

        [Test]
        public void A_side_exit_must_name_a_destination()
        {
            var room = Valid();
            room.SideExits = new[] { new SideExitSpec { X = -32f, To = "" } };
            Assert.Throws<SettingsValidationException>(() => room.Validate());
        }

        [Test]
        public void A_door_needs_a_label_and_destination()
        {
            var room = Valid();
            room.Doors = new[] { new DoorSpec { X = 100f, Y = 0f, Label = "", To = "Shop" } };
            Assert.Throws<SettingsValidationException>(() => room.Validate());
        }

        [Test]
        public void A_lamppost_needs_a_description_and_must_sit_near_the_room()
        {
            var room = Valid();
            room.Lampposts = new[] { new PropSpot { X = 100f, Description = "" } };
            Assert.Throws<SettingsValidationException>(() => room.Validate());

            room.Lampposts = new[] { new PropSpot { X = 999999f, Description = "A lamp post." } };
            Assert.Throws<SettingsValidationException>(() => room.Validate());
        }

        [Test]
        public void Side_exits_may_sit_just_outside_the_room_strip()
        {
            var room = Valid();
            room.SideExits = new[] { new SideExitSpec { X = -32f, To = "Street01" } };
            Assert.DoesNotThrow(() => room.Validate());
        }
    }

    public class NpcDefinitionTests
    {
        private static NpcDefinition Valid() => new NpcDefinition
        {
            Id = "0097",
            SheepId = "0097",
            RoomId = "Street00",
            Description = "A young woman with tell tale red eyes.",
            SpawnX = 2400f,
            Waypoints = new[] { 9000f, 2400f },
        };

        [Test]
        public void A_well_formed_npc_validates()
        {
            Assert.DoesNotThrow(() => Valid().Validate());
        }

        [Test]
        public void An_npc_needs_a_description()
        {
            var npc = Valid();
            npc.Description = "";
            Assert.Throws<SettingsValidationException>(() => npc.Validate());
        }

        [Test]
        public void An_npc_needs_at_least_one_waypoint()
        {
            var npc = Valid();
            npc.Waypoints = new float[0];
            Assert.Throws<SettingsValidationException>(() => npc.Validate());
        }

        [Test]
        public void Walk_speed_and_arrive_range_must_be_positive()
        {
            var npc = Valid();
            npc.WalkSpeed = 0f;
            Assert.Throws<SettingsValidationException>(() => npc.Validate());

            npc = Valid();
            npc.ArriveRange = -1f;
            Assert.Throws<SettingsValidationException>(() => npc.Validate());
        }
    }

    public class ConsoleTextTests
    {
        [Test]
        public void Default_text_validates()
        {
            Assert.DoesNotThrow(() => new ConsoleText().Validate());
        }

        [Test]
        public void A_blank_field_is_rejected()
        {
            var text = new ConsoleText { NotRecognizedText = "" };
            Assert.Throws<SettingsValidationException>(() => text.Validate());
        }
    }

    public class GameContentTests
    {
        private static RoomDefinition Room(string id, float width, params SideExitSpec[] exits) => new RoomDefinition
        {
            Id = id,
            DisplayName = id,
            Width = width,
            BackgroundSlices = new[] { "bg_" + id },
            PlayerStartX = 0f,
            SideExits = exits,
        };

        private static NpcDefinition Npc(string roomId, float spawnX) => new NpcDefinition
        {
            Id = "npc",
            SheepId = "0097",
            RoomId = roomId,
            Description = "Someone.",
            SpawnX = spawnX,
            Waypoints = new[] { 0f },
        };

        [Test]
        public void A_well_formed_catalog_validates()
        {
            var content = new GameContent
            {
                Rooms = new[] { Room("Street00", 11520f) },
                Npcs = new[] { Npc("Street00", 2400f) },
            };
            Assert.DoesNotThrow(() => content.Validate());
        }

        [Test]
        public void At_least_one_room_is_required()
        {
            var content = new GameContent { Rooms = new RoomDefinition[0] };
            Assert.Throws<SettingsValidationException>(() => content.Validate());
        }

        [Test]
        public void Two_rooms_sharing_an_id_are_rejected()
        {
            var content = new GameContent
            {
                Rooms = new[] { Room("Street00", 1000f), Room("Street00", 2000f) },
            };
            Assert.Throws<SettingsValidationException>(() => content.Validate());
        }

        [Test]
        public void A_side_exit_to_an_unknown_room_is_rejected()
        {
            var content = new GameContent
            {
                Rooms = new[] { Room("Street00", 1000f, new SideExitSpec { X = -32f, To = "Nowhere" }) },
            };
            Assert.Throws<SettingsValidationException>(() => content.Validate());
        }

        [Test]
        public void An_npc_naming_an_unknown_room_is_rejected()
        {
            var content = new GameContent
            {
                Rooms = new[] { Room("Street00", 1000f) },
                Npcs = new[] { Npc("Nowhere", 0f) },
            };
            Assert.Throws<SettingsValidationException>(() => content.Validate());
        }

        [Test]
        public void An_npc_spawning_outside_its_room_is_rejected()
        {
            var content = new GameContent
            {
                Rooms = new[] { Room("Street00", 1000f) },
                Npcs = new[] { Npc("Street00", 5000f) },
            };
            Assert.Throws<SettingsValidationException>(() => content.Validate());
        }

        [Test]
        public void Find_room_and_npc_are_case_insensitive()
        {
            var content = new GameContent
            {
                Rooms = new[] { Room("Street00", 1000f) },
                Npcs = new[] { Npc("Street00", 0f) },
            };
            Assert.IsNotNull(content.FindRoom("street00"));
            Assert.IsNotNull(content.FindNpc("NPC"));
        }

        // U.INC-T28: the sound catalog.
        private static SoundDefinition Sound(string id, bool wired = true) => new SoundDefinition
        {
            Id = id,
            File = id + ".wav",
            PlayedBy = "somewhere",
            Wired = wired,
        };

        [Test]
        public void Room_music_and_title_tracks_must_name_catalogued_sounds()
        {
            var room = Room("Street00", 1000f);
            room.Music = "music_x";
            var content = new GameContent
            {
                Rooms = new[] { room },
                Sounds = new[] { Sound("music_x"), Sound("whispers", wired: false) },
                TitleScreen = new TitleScreenSpec { Music = "music_x", UnwiredTrack = "whispers" },
            };
            Assert.DoesNotThrow(() => content.Validate());
            Assert.IsNotNull(content.FindSound("WHISPERS"));

            room.Music = "music_missing";
            Assert.Throws<SettingsValidationException>(() => content.Validate());

            room.Music = "music_x";
            content.TitleScreen.UnwiredTrack = "missing";
            Assert.Throws<SettingsValidationException>(() => content.Validate());
        }

        [Test]
        public void Two_sounds_sharing_an_id_are_rejected()
        {
            var content = new GameContent
            {
                Rooms = new[] { Room("Street00", 1000f) },
                Sounds = new[] { Sound("sfx_flick"), Sound("SFX_FLICK") },
            };
            Assert.Throws<SettingsValidationException>(() => content.Validate());
        }

        [Test]
        public void A_sound_needs_a_bare_file_name_and_its_source()
        {
            var sound = Sound("sfx_flick");
            sound.File = "";
            Assert.Throws<SettingsValidationException>(() => sound.Validate());

            sound = Sound("sfx_flick");
            sound.File = "sub/sfx_flick.wav";
            Assert.Throws<SettingsValidationException>(() => sound.Validate());

            sound = Sound("sfx_flick");
            sound.PlayedBy = "";
            Assert.Throws<SettingsValidationException>(() => sound.Validate());
        }
    }
}
