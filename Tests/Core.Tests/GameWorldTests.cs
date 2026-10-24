// NUnit 4 moved the classic Assert; the tests were written against it (Unity Test Framework).
using Assert = NUnit.Framework.Legacy.ClassicAssert;
using CollectionAssert = NUnit.Framework.Legacy.CollectionAssert;
using StringAssert = NUnit.Framework.Legacy.StringAssert;
using System.Collections.Generic;
using System.Linq;
using Incognito.Npc;
using Incognito.Player;
using Incognito.Terminal;
using Incognito.Weapon;
using Incognito.World;
using NUnit.Framework;

namespace Incognito.Tests
{
    public class VicinityTests
    {
        [Test]
        public void Vicinity_keeps_only_objects_within_radius()
        {
            var a = new ConsoleObject(ConsoleObjectType.Sheep, "near");
            var b = new ConsoleObject(ConsoleObjectType.Sheep, "edge");
            var c = new ConsoleObject(ConsoleObjectType.Sheep, "far");
            var candidates = new[]
            {
                new KeyValuePair<ConsoleObject, Vec2>(a, new Vec2(1f, 0f)),
                new KeyValuePair<ConsoleObject, Vec2>(b, new Vec2(10f, 0f)),
                new KeyValuePair<ConsoleObject, Vec2>(c, new Vec2(10.5f, 0f)),
            };

            var within = Vicinity.Within(Vec2.Zero, 10f, candidates).ToList();
            CollectionAssert.AreEqual(new[] { a, b }, within);
        }
    }

    public class GameWorldTests
    {
        private static readonly PlayerId P1 = new PlayerId(1);
        private static readonly PlayerId P2 = new PlayerId(2);
        private const float Dt = 1f / 60f;

        private static GameWorld WorldWithPlayers(params PlayerId[] ids)
        {
            var world = new GameWorld();
            foreach (PlayerId id in ids) world.AddPlayer(id, PlayerSetup.Default);
            return world;
        }

        private static PlayerControls Walking(bool right) =>
            new PlayerControls(!right, right, false, false, 0f, false, 1920f);

        [Test]
        public void A_command_from_a_player_not_in_the_world_is_ignored()
        {
            var world = WorldWithPlayers(P1);
            Assert.IsFalse(world.Submit(new ToggleGun(P2)));
            Assert.AreEqual(PlayerWeaponState.AimMode.Off, world.FindPlayer(P1).Weapon.Mode);
            Assert.AreEqual(0, world.PendingEvents.Count);
        }

        [Test]
        public void A_seat_cannot_be_taken_twice()
        {
            var world = WorldWithPlayers(P1);
            Assert.Throws<System.ArgumentException>(() => world.AddPlayer(P1, PlayerSetup.Default));
        }

        [Test]
        public void Controls_walk_only_the_player_who_sent_them()
        {
            var world = WorldWithPlayers(P1, P2);
            world.Submit(new SetControls(P1, Walking(right: true)));
            for (int i = 0; i < 30; i++) world.Step(Dt);

            Assert.Greater(world.FindPlayer(P1).Locomotion.PositionX, 0f);
            Assert.AreEqual(0f, world.FindPlayer(P2).Locomotion.PositionX);
        }

        [Test]
        public void Controls_do_nothing_until_the_world_steps()
        {
            var world = WorldWithPlayers(P1);
            world.Submit(new SetControls(P1, Walking(right: true)));
            Assert.AreEqual(0f, world.FindPlayer(P1).Locomotion.PositionX);
        }

        [Test]
        public void Aiming_locks_walking()
        {
            var world = WorldWithPlayers(P1);
            world.Submit(new ToggleGun(P1));
            Assert.IsTrue(world.Submit(new PressAim(P1)));
            world.Submit(new SetControls(P1, Walking(right: true)));
            for (int i = 0; i < 30; i++) world.Step(Dt);

            Assert.AreEqual(0f, world.FindPlayer(P1).Locomotion.PositionX);
            Assert.IsTrue(world.FindPlayer(P1).MovementLocked);
        }

        [Test]
        public void Autowalk_walks_the_way_the_player_faces_ignoring_the_keys()
        {
            var world = WorldWithPlayers(P1);
            world.Submit(new SetAutoWalk(P1, true));
            for (int i = 0; i < 30; i++) world.Step(Dt);

            Assert.Less(world.FindPlayer(P1).Locomotion.PositionX, 0f); // starts facing left
        }

        [Test]
        public void Firing_while_aiming_produces_a_bullet_and_a_sound_and_firing_holstered_does_not()
        {
            var world = WorldWithPlayers(P1);
            Assert.IsFalse(world.Submit(new PressFire(P1)));
            Assert.AreEqual(0, world.PendingEvents.Count);

            world.Submit(new ToggleGun(P1));
            world.Submit(new PressAim(P1));
            world.TakeEvents();
            Assert.IsTrue(world.Submit(new PressFire(P1)));

            var events = world.TakeEvents();
            Assert.IsTrue(events.OfType<BulletFired>().Any(e => e.Player == P1));
            Assert.IsTrue(events.OfType<WeaponSounded>().Any(e => e.Sound == PlayerWeaponState.WeaponSound.Fire));
        }

        [Test]
        public void Toggling_the_gun_plays_events_in_order()
        {
            var world = WorldWithPlayers(P1);
            world.Submit(new ToggleGun(P1));
            world.Submit(new ToggleGun(P1));

            var sounds = world.TakeEvents().OfType<WeaponSounded>().Select(e => e.Sound).ToArray();
            CollectionAssert.AreEqual(new[] { PlayerWeaponState.WeaponSound.Reload, PlayerWeaponState.WeaponSound.Holster }, sounds);
            Assert.AreEqual(0, world.PendingEvents.Count, "events are taken once");
        }

        [Test]
        public void Walking_produces_footstep_events_for_that_player()
        {
            var world = WorldWithPlayers(P1);
            world.Submit(new SetControls(P1, Walking(right: true)));
            for (int i = 0; i < 120; i++) world.Step(Dt);

            var steps = world.TakeEvents().OfType<PlayerFootstep>().ToList();
            Assert.Greater(steps.Count, 0);
            Assert.IsTrue(steps.All(s => s.Player == P1));
        }

        [Test]
        public void Placing_a_player_sets_their_x_and_facing_and_ends_autowalk()
        {
            var world = WorldWithPlayers(P1);
            world.Submit(new SetAutoWalk(P1, true));
            world.Submit(new PlacePlayer(P1, 1234f, PlayerLocomotionState.FacingDirection.Right));

            var player = world.FindPlayer(P1);
            Assert.AreEqual(1234f, player.Locomotion.PositionX);
            Assert.AreEqual(PlayerLocomotionState.FacingDirection.Right, player.Locomotion.Facing);
            Assert.IsFalse(player.AutoWalk);
            Assert.IsTrue(world.TakeEvents().OfType<PlayerPlaced>().Any());
        }

        [Test]
        public void A_door_is_used_only_within_reach_and_idles_the_player()
        {
            var world = WorldWithPlayers(P1);
            world.Submit(new TeleportPlayer(P1, 1000f));
            Assert.IsFalse(world.Submit(new UseDoor(P1, 1400f)));
            Assert.AreEqual(0, world.PendingEvents.Count);

            Assert.IsTrue(world.Submit(new UseDoor(P1, 1250f)));
            Assert.AreEqual(PlayerLocomotionState.WalkMode.Idle, world.FindPlayer(P1).Locomotion.CurrentMode);
            Assert.AreEqual(1250f, world.TakeEvents().OfType<DoorUsed>().Single().DoorX);
        }

        [Test]
        public void A_barrier_holds_the_player_on_its_near_side()
        {
            var world = WorldWithPlayers(P1);
            world.AddBarrier(800f, 512f);
            world.Submit(new TeleportPlayer(P1, 1400f));
            world.Submit(new SetAutoWalk(P1, true));
            for (int i = 0; i < 600; i++) world.Step(Dt);

            Assert.AreEqual(800f + RoomExitRules.StripWidth + 512f / 2f, world.FindPlayer(P1).Locomotion.PositionX, 5f);
        }

        [Test]
        public void A_side_exit_catches_the_player_once_and_puts_them_on_autowalk()
        {
            var world = WorldWithPlayers(P1);
            int exit = world.AddSideExit(-10f, 512f);
            world.Submit(new TeleportPlayer(P1, 0f));
            world.Step(Dt);

            Assert.IsTrue(world.FindPlayer(P1).AutoWalk);
            var reached = world.TakeEvents().OfType<SideExitReached>().ToList();
            Assert.AreEqual(1, reached.Count);
            Assert.AreEqual(exit, reached[0].Exit);

            world.Step(Dt);
            Assert.AreEqual(0, world.TakeEvents().OfType<SideExitReached>().Count(), "it fires once");
        }

        private static NpcEntity AddNpc(GameWorld world, PlayerId watching, float x = 500f)
        {
            return world.AddNpc(new NpcSetup
            {
                Watching = watching,
                StartX = x,
                Waypoints = new float[0],
                WalkSpeed = 5f,
                ArriveRange = 20f,
                Suspicion = new SuspicionTuning(),
            });
        }

        [Test]
        public void A_shot_shoves_the_npc_and_says_so()
        {
            var world = WorldWithPlayers(P1);
            var npc = AddNpc(world, P1, 500f);
            Assert.IsTrue(world.Submit(new ShoveNpc(P1, npc.Id, -100f)));

            Assert.AreEqual(400f, npc.Patrol.PositionX);
            var shoved = world.TakeEvents().OfType<NpcShoved>().Single();
            Assert.AreEqual(P1, shoved.Shooter);
            Assert.AreEqual(npc.Id, shoved.Npc);
        }

        [Test]
        public void Commands_about_an_unknown_npc_are_ignored()
        {
            var world = WorldWithPlayers(P1);
            Assert.IsFalse(world.Submit(new ShoveNpc(P1, new NpcId(99), 100f)));
            Assert.IsFalse(world.Submit(new TalkToNpc(P1, new NpcId(99), 200f)));
        }

        [Test]
        public void A_talking_npc_walks_to_the_player_and_turns_them_to_face_it()
        {
            var world = WorldWithPlayers(P1);
            world.Submit(new TeleportPlayer(P1, 2000f));
            var npc = AddNpc(world, P1, 500f);
            world.Submit(new TalkToNpc(P1, npc.Id, 256f));
            for (int i = 0; i < 1200 && !npc.Patrol.InConversation; i++) world.Step(Dt);

            Assert.IsTrue(npc.Patrol.InConversation);
            Assert.IsTrue(world.TakeEvents().OfType<ConversationStarted>().Any(e => e.Npc == npc.Id));
        }

        [Test]
        public void The_npc_grows_suspicious_of_the_player_it_watches_only()
        {
            var world = WorldWithPlayers(P1, P2);
            world.Submit(new TeleportPlayer(P1, 600f));
            world.Submit(new TeleportPlayer(P2, 600f));
            var watchingP1 = AddNpc(world, P1, 500f);
            var watchingP2 = AddNpc(world, P2, 500f);
            // P2 is far from where their NPC stands.
            world.Submit(new TeleportPlayer(P2, 50000f));
            for (int i = 0; i < 120; i++) world.Step(Dt);

            Assert.Greater(watchingP1.Suspicion.Suspicion, watchingP2.Suspicion.Suspicion);
        }

        [Test]
        public void The_console_lists_what_is_in_vicinity_and_reports_a_reboot()
        {
            var world = WorldWithPlayers(P1);
            var near = new ConsoleObject(ConsoleObjectType.Sheep, "0097");
            var far = new ConsoleObject(ConsoleObjectType.Sheep, "0451");
            world.RegisterConsoleObject(near, new Vec2(100f, 0f));
            world.RegisterConsoleObject(far, new Vec2(9000f, 0f));

            world.Submit(new RunConsoleCommand(P1, "list sheep", Vec2.Zero, 1920f));
            var output = world.TakeEvents().OfType<ConsoleOutput>().Single();
            StringAssert.Contains("SHP#0097", output.Text);
            StringAssert.DoesNotContain("SHP#0451", output.Text);

            world.Submit(new RunConsoleCommand(P1, "reboot 0097", Vec2.Zero, 1920f));
            var events = world.TakeEvents();
            Assert.AreEqual(near, events.OfType<ConsoleObjectRebooted>().Single().Target);
            Assert.AreEqual("Rebooting SHP#0097...", events.OfType<ConsoleOutput>().Single().Text);
        }

        [Test]
        public void An_unregistered_console_object_is_gone()
        {
            var world = WorldWithPlayers(P1);
            var obj = new ConsoleObject(ConsoleObjectType.Camera, "07");
            world.RegisterConsoleObject(obj, Vec2.Zero);
            Assert.IsTrue(world.UnregisterConsoleObject(obj));
            Assert.IsFalse(world.UnregisterConsoleObject(obj));

            world.Submit(new RunConsoleCommand(P1, "list camera", Vec2.Zero, 1920f));
            StringAssert.Contains("none", world.TakeEvents().OfType<ConsoleOutput>().Single().Text);
        }
    }
}
