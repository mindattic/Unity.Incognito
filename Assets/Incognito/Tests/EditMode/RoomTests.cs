using Incognito.Player;
using Incognito.World;
using NUnit.Framework;

namespace Incognito.Tests
{
    public class RoomTransitionTests
    {
        [Test]
        public void Entry_follows_obj_room_enter_from()
        {
            Assert.AreEqual(11520f - 512f, RoomTransition.EntryX(RoomEntrance.Right, 11520f, 512f, 0f));
            Assert.AreEqual(512f, RoomTransition.EntryX(RoomEntrance.Left, 15360f, 512f, 0f));
            Assert.AreEqual(9500f, RoomTransition.EntryX(RoomEntrance.At, 11520f, 512f, 9500f)); // out of the shop
        }

        [Test]
        public void A_pending_transition_applies_once()
        {
            RoomTransition.Request("Shop", RoomEntrance.Right, 0f, PlayerLocomotionState.FacingDirection.Right);
            Assert.AreEqual("Shop", RoomTransition.NextRoom);

            Assert.IsTrue(RoomTransition.TryConsume(out var entrance, out _, out var facing));
            Assert.AreEqual(RoomEntrance.Right, entrance);
            Assert.AreEqual(PlayerLocomotionState.FacingDirection.Right, facing);
            Assert.IsFalse(RoomTransition.TryConsume(out _, out _, out _));
        }
    }

    public class WorldTimingTests
    {
        [Test]
        public void Title_skyline_scrolls_left_2px_per_step_and_wraps_by_one_tile()
        {
            float offset = ScrollingBackground.Wrap(0f, -2f, 1, 1920f);
            Assert.AreEqual(-2f, offset);
            offset = ScrollingBackground.Wrap(offset, -2f, 959, 1920f);
            Assert.AreEqual(0f, offset, 1e-4f); // a full tile later it is back where it started
        }

        [Test]
        public void Lamp_light_blanks_only_on_a_zero_roll()
        {
            Assert.AreEqual(0f, LampFlicker.LightAlpha(0));
            Assert.AreEqual(1f, LampFlicker.LightAlpha(1));
            Assert.AreEqual(1f, LampFlicker.LightAlpha(LampFlicker.Odds));
            Assert.AreEqual(1f, LampFlicker.Roll(3, (min, max) => max, 0f));
        }
    }
}
