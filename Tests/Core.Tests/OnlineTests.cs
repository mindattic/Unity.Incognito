// NUnit 4 moved the classic Assert; the tests were written against it (Unity Test Framework).
using Assert = NUnit.Framework.Legacy.ClassicAssert;
using System;
using Incognito.Content;
using Incognito.Settings;
using Incognito.World;
using NUnit.Framework;

namespace Incognito.Tests
{
    /// <summary>U.INC-T43: the engine-free half of online play: seats, the controls on the wire,
    /// unseating a player and the online tuning.</summary>
    public class OnlineTests
    {
        [Test]
        public void Seat_zero_plays_as_local_plays_P1_and_the_mapping_round_trips()
        {
            Assert.AreEqual(new PlayerId(1), OnlineSeats.PlayerFor(0));
            Assert.AreEqual(new PlayerId(4), OnlineSeats.PlayerFor(3));
            for (int seat = 0; seat < 16; seat++) Assert.AreEqual(seat, OnlineSeats.SeatOf(OnlineSeats.PlayerFor(seat)));
            Assert.AreEqual(-1, OnlineSeats.SeatOf(new PlayerId(0)));
            Assert.Throws<ArgumentOutOfRangeException>(() => OnlineSeats.PlayerFor(-1));
        }

        [Test]
        public void Controls_survive_the_wire_and_a_clients_bad_numbers_are_made_safe()
        {
            var sent = new PlayerControls(true, false, true, true, -120f, true, 2560f);
            PlayerControls got = OnlineControls.Unpack(OnlineControls.PackKeys(sent), sent.MouseBelowCentre, sent.ViewWidth);
            Assert.AreEqual(sent.MoveLeft, got.MoveLeft);
            Assert.AreEqual(sent.MoveRight, got.MoveRight);
            Assert.AreEqual(sent.Sprint, got.Sprint);
            Assert.AreEqual(sent.ShiftHeld, got.ShiftHeld);
            Assert.AreEqual(sent.LookingDown, got.LookingDown);
            Assert.AreEqual(-120f, got.MouseBelowCentre);
            Assert.AreEqual(2560f, got.ViewWidth);

            PlayerControls cheat = OnlineControls.Unpack(0, float.NaN, float.PositiveInfinity);
            Assert.AreEqual(0f, cheat.MouseBelowCentre);
            Assert.AreEqual(OnlineControls.MaxViewWidth, cheat.ViewWidth);
            Assert.AreEqual(540f, OnlineControls.Unpack(0, 99999f, -5f).MouseBelowCentre);
            Assert.AreEqual(1920f, OnlineControls.Unpack(0, 0f, -5f).ViewWidth);
        }

        [Test]
        public void A_removed_player_is_gone_and_ignores_commands_while_others_play_on()
        {
            var world = new GameWorld();
            world.AddPlayer(OnlineSeats.PlayerFor(0), PlayerSetup.Default);
            world.AddPlayer(OnlineSeats.PlayerFor(1), PlayerSetup.Default);

            Assert.IsTrue(world.RemovePlayer(OnlineSeats.PlayerFor(1)));
            Assert.IsFalse(world.RemovePlayer(OnlineSeats.PlayerFor(1)));
            Assert.IsNull(world.FindPlayer(OnlineSeats.PlayerFor(1)));
            Assert.IsFalse(world.Submit(new ToggleGun(OnlineSeats.PlayerFor(1))));

            world.Submit(new SetControls(OnlineSeats.PlayerFor(0), new PlayerControls(false, true, false, false, 0f, false, 1920f)));
            for (int i = 0; i < 30; i++) world.Step(1f / 60f);
            Assert.Greater(world.FindPlayer(OnlineSeats.PlayerFor(0)).Locomotion.PositionX, 0f);

            // The seat can be taken again.
            Assert.DoesNotThrow(() => world.AddPlayer(OnlineSeats.PlayerFor(1), PlayerSetup.Default));
        }

        [Test]
        public void A_removed_player_rejoining_can_take_a_side_exit_again()
        {
            var world = new GameWorld();
            var id = OnlineSeats.PlayerFor(1);
            world.AddSideExit(0f, 512f);
            world.AddPlayer(id, PlayerSetup.Default);
            world.Step(1f / 60f);
            Assert.AreEqual(1, Array.FindAll(world.TakeEvents(), e => e is SideExitReached).Length);

            world.RemovePlayer(id);
            world.AddPlayer(id, PlayerSetup.Default);
            world.Step(1f / 60f);
            Assert.AreEqual(1, Array.FindAll(world.TakeEvents(), e => e is SideExitReached).Length);
        }

        [Test]
        public void Online_tuning_starts_seat_zero_where_local_play_does_and_spaces_the_rest_inward()
        {
            var tuning = new OnlineTuning { SeatSpacing = 600f };
            var right = new RoomDefinition { Width = 11520f, PlayerStartX = 11008f };
            var left = new RoomDefinition { Width = 15360f, PlayerStartX = 512f };

            Assert.AreEqual(11008f, tuning.StartFor(right, 0), "seat 0 starts where local play does");
            Assert.AreEqual(11008f - 1200f, tuning.StartFor(right, 2), "a start near the right edge spaces leftward");
            Assert.AreEqual(512f + 600f, tuning.StartFor(left, 1), "a start near the left edge spaces rightward");
            Assert.AreEqual(0f, tuning.StartFor(new RoomDefinition { Width = 1000f, PlayerStartX = 900f }, 5), "kept inside the room");
            Assert.Throws<ArgumentNullException>(() => tuning.StartFor(null, 0));

            Assert.DoesNotThrow(() => tuning.Validate());
            Assert.Throws<SettingsValidationException>(() => new OnlineTuning { MaxPlayers = 0 }.Validate());
            Assert.Throws<SettingsValidationException>(() => new OnlineTuning { SeatSpacing = -1f }.Validate());
            Assert.Throws<SettingsValidationException>(() => new OnlineTuning { Room = "" }.Validate());
            Assert.Throws<SettingsValidationException>(() => new SettingsRegistry { Online = null }.Validate());
        }

        [Test]
        public void The_shipped_online_room_is_a_room_in_the_content()
        {
            var dir = new System.IO.DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (dir != null && !System.IO.File.Exists(System.IO.Path.Combine(dir.FullName, "Assets", "Incognito", "Resources", "Data", "game-tuning.json"))) dir = dir.Parent;
            Assert.IsNotNull(dir, "Could not find Assets/Incognito/Resources/Data above the test directory.");
            string data = System.IO.Path.Combine(dir.FullName, "Assets", "Incognito", "Resources", "Data");
            var tuning = SettingsRegistry.FromJson(System.IO.File.ReadAllText(System.IO.Path.Combine(data, "game-tuning.json")));
            var content = Incognito.Content.GameContent.FromJson(System.IO.File.ReadAllText(System.IO.Path.Combine(data, "game-content.json")));
            tuning.Validate();
            Assert.IsTrue(Array.Exists(content.Rooms, r => r.Id == tuning.Online.Room), tuning.Online.Room);
        }
    }
}
