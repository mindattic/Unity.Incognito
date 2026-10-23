using System;
using System.Collections;
using Incognito.Player;
using Incognito.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Facing = Incognito.Player.PlayerLocomotionState.FacingDirection;
using Object = UnityEngine.Object;

namespace Incognito.Tests
{
    /// <summary>
    /// U.INC-T13 end-to-end check: plays the built room scenes in the order the original
    /// could be played (RECONSTRUCTION.md §7) — title -> Bleecher Street -> both side exits to
    /// 2nd Avenue and back -> the floating door into Simon's Pawn and Loan -> the shop's barrier
    /// -> its right-edge exit back out at x 9500 — and checks every arrival lands where obj_room
    /// placed the player. The player is walked by the side exits' own autowalk; the door is used
    /// through <see cref="DoorExit.TryUse"/> (what its W press calls).
    /// </summary>
    public class RoomTraversalPlayTests
    {
        private const float PlayerWidth = 512f;
        private const float Timeout = 60f;

        private float previousTimeScale;

        [SetUp]
        public void SetUp()
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 4f;
        }

        [TearDown]
        public void TearDown() => Time.timeScale = previousTimeScale;

        [UnityTest]
        public IEnumerator Every_room_is_reachable_from_the_title_and_every_exit_lands_where_the_original_put_the_player()
        {
            SceneManager.LoadScene("Titlescreen");
            yield return null;
            yield return null;

            var title = Object.FindAnyObjectByType<TitleScreen>();
            Assert.IsNotNull(title, "Titlescreen has its obj_titlescreen");
            title.StartGame();

            // start_game(): street00, entering from the right, facing left.
            Room room = null;
            yield return Arrive("Street00", r => room = r);
            AssertArrival(room, "Bleecher Street", 11520f - PlayerWidth, Facing.Left);

            // Right edge of street00 -> street01 entering "left".
            yield return WalkOff(room, Facing.Right);
            yield return Arrive("Street01", r => room = r);
            AssertArrival(room, "2nd Avenue", PlayerWidth, Facing.Right);

            // Left edge of street01 -> street00 entering "right".
            yield return WalkOff(room, Facing.Left);
            yield return Arrive("Street00", r => room = r);
            AssertArrival(room, "Bleecher Street", 11520f - PlayerWidth, Facing.Left);

            // The floating door to the shop at x 9504: out of reach, then within 300px.
            var door = Object.FindAnyObjectByType<DoorExit>();
            Assert.IsNotNull(door, "street00 has its obj_exit to the shop");
            room.Player.Teleport(9504f - 400f);
            yield return null;
            Assert.IsFalse(door.TryUse(), "W does nothing beyond 300px of the door");
            room.Player.Teleport(9504f - 250f);
            yield return null;
            Assert.IsTrue(door.TryUse(), "W within 300px enters the shop");
            yield return Arrive("Shop", r => room = r);
            AssertArrival(room, "Simon's Pawn and Loan", 5760f - PlayerWidth, Facing.Left);

            // The shop's obj_barrier at x 800 stops the player walking west.
            room.Player.Teleport(1400f);
            room.Player.AutoWalk = true;
            yield return WaitSeconds(3f);
            room.Player.AutoWalk = false;
            Assert.AreEqual(800f + RoomExitRules.StripWidth + PlayerWidth / 2f, room.Player.State.PositionX, 1f,
                "the barrier holds the player's box on its east side");
            Assert.AreEqual("Shop", SceneManager.GetActiveScene().name);

            // The shop's right-edge exit -> street00 at enter_x 9500.
            room.Player.Teleport(5200f);
            yield return WalkOff(room, Facing.Right);
            yield return Arrive("Street00", r => room = r);
            AssertArrival(room, "Bleecher Street", 9500f, Facing.Right);

            // Left edge of street00 -> street01 entering "right".
            room.Player.Teleport(600f);
            yield return WalkOff(room, Facing.Left);
            yield return Arrive("Street01", r => room = r);
            AssertArrival(room, "2nd Avenue", 15360f - PlayerWidth, Facing.Left);

            // Right edge of street01 -> street00 entering "left".
            yield return WalkOff(room, Facing.Right);
            yield return Arrive("Street00", r => room = r);
            AssertArrival(room, "Bleecher Street", PlayerWidth, Facing.Right);
        }

        private static void AssertArrival(Room room, string name, float x, Facing facing)
        {
            Assert.AreEqual(name, room.DisplayName);
            Assert.AreEqual(x, room.Player.State.PositionX, 1f, $"{name}: entry x");
            Assert.AreEqual(facing, room.Player.State.Facing, $"{name}: entry facing");
            Assert.IsFalse(room.Player.AutoWalk, $"{name}: autowalk is off on arrival");
        }

        /// <summary>Turns the player and lets obj_walker's autowalk carry them until an exit fires.</summary>
        private static IEnumerator WalkOff(Room room, Facing facing)
        {
            // TurnToFace only applies while idle: let any coast from a previous walk finish.
            while (room.Player.State.CurrentMode != PlayerLocomotionState.WalkMode.Idle) yield return null;
            Assert.IsTrue(room.Player.State.TurnToFace(facing));
            room.Player.AutoWalk = true;
            yield return null;
        }

        private static IEnumerator Arrive(string scene, Action<Room> found)
        {
            float waited = 0f;
            while (SceneManager.GetActiveScene().name != scene || Object.FindAnyObjectByType<Room>() == null)
            {
                waited += Time.unscaledDeltaTime;
                if (waited > Timeout) Assert.Fail($"never reached {scene} (still in {SceneManager.GetActiveScene().name})");
                yield return null;
            }

            yield return null; // Room.Start places the player
            yield return null;
            found(Object.FindAnyObjectByType<Room>());
        }

        private static IEnumerator WaitSeconds(float seconds)
        {
            float until = Time.time + seconds;
            while (Time.time < until) yield return null;
        }
    }
}
