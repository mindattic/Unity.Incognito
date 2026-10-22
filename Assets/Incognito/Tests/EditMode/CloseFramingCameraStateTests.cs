using Incognito.CameraRig;
using Incognito.Player;
using NUnit.Framework;

namespace Incognito.Tests
{
    public class CloseFramingCameraStateTests
    {
        private const float Dt = 1f / 60f; // one original-style step
        private const PlayerLocomotionState.FacingDirection Left = PlayerLocomotionState.FacingDirection.Left;
        private const PlayerLocomotionState.FacingDirection Right = PlayerLocomotionState.FacingDirection.Right;

        // Original street00: 1920 view, 512px spr_player, 11520px room, hborder 910, hspeed 20.
        private static CloseFramingCameraState NewState() => new CloseFramingCameraState
        {
            ViewWidth = 1920f,
            PlayerWidth = 512f,
            RoomMinX = 0f,
            RoomMaxX = 11520f,
            HorizontalDeadzone = 50f,
            HorizontalPanSpeed = 20f,
            MaxVerticalPan = 135f,
        };

        [Test]
        public void Target_leads_the_player_in_the_facing_direction()
        {
            var state = NewState();
            // Original: x = player.x +/- (view_wview/2 - player_width) = +/- 448.
            Assert.AreEqual(5000f + 448f, state.TargetX(5000f, Right), 0.001f);
            Assert.AreEqual(5000f - 448f, state.TargetX(5000f, Left), 0.001f);
        }

        [Test]
        public void Target_falls_back_to_player_x_near_room_ends()
        {
            var state = NewState();
            // Lock band is [960 + 512, 11520 - 960 - 512).
            Assert.AreEqual(1400f, state.TargetX(1400f, Right), 0.001f);
            Assert.AreEqual(10100f, state.TargetX(10100f, Left), 0.001f);
            Assert.AreEqual(1472f + 448f, state.TargetX(1472f, Right), 0.001f);
        }

        [Test]
        public void Snap_pins_the_player_near_the_trailing_edge()
        {
            var state = NewState();
            state.SnapTo(5000f, Right);

            float playerScreenX = 5000f - (state.CenterX - 960f);
            Assert.AreEqual(512f, playerScreenX, 0.001f, "facing right, player sits one sprite-width from the left edge");

            state.SnapTo(5000f, Left);
            playerScreenX = 5000f - (state.CenterX - 960f);
            Assert.AreEqual(1920f - 512f, playerScreenX, 0.001f, "facing left, player sits one sprite-width from the right edge");
        }

        [Test]
        public void Small_moves_inside_the_deadzone_do_not_pan()
        {
            var state = NewState();
            state.SnapTo(5000f, Right);
            float before = state.CenterX;

            state.Step(Dt, 5040f, Right, 0.5f);

            Assert.AreEqual(before, state.CenterX, 0.001f);
        }

        [Test]
        public void Facing_flip_swings_the_view_at_most_hspeed_per_step()
        {
            var state = NewState();
            state.SnapTo(5000f, Right);
            float start = state.CenterX;

            state.Step(Dt, 5000f, Left, 0.5f);
            Assert.AreEqual(start - 20f, state.CenterX, 0.001f, "one step pans exactly hspeed toward the new side");

            for (int i = 0; i < 120; i++) state.Step(Dt, 5000f, Left, 0.5f);

            // Settles at the deadzone edge nearest the new target (target + 50).
            Assert.AreEqual(5000f - 448f + 50f, state.CenterX, 0.001f);
        }

        [Test]
        public void Pan_speed_scales_with_delta_time()
        {
            var state = NewState();
            state.SnapTo(5000f, Right);
            float start = state.CenterX;

            state.Step(Dt * 2f, 5000f, Left, 0.5f);

            Assert.AreEqual(start - 40f, state.CenterX, 0.001f);
        }

        [Test]
        public void View_is_clamped_inside_the_room()
        {
            var state = NewState();
            state.SnapTo(100f, Left);
            Assert.AreEqual(960f, state.CenterX, 0.001f);

            state.SnapTo(11500f, Right);
            Assert.AreEqual(11520f - 960f, state.CenterX, 0.001f);
        }

        [Test]
        public void Room_narrower_than_the_view_centres_it()
        {
            var state = NewState();
            state.RoomMaxX = 1000f;
            state.SnapTo(300f, Right);
            Assert.AreEqual(500f, state.CenterX, 0.001f);
        }

        [Test]
        public void Vertical_pan_follows_mouse_height_and_is_bounded()
        {
            var state = NewState();
            state.SnapTo(5000f, Right);

            state.Step(Dt, 5000f, Right, 0.5f);
            Assert.AreEqual(0f, state.VerticalOffset, 0.001f, "mouse at mid-screen rests");

            state.Step(Dt, 5000f, Right, 1f);
            Assert.AreEqual(135f, state.VerticalOffset, 0.001f, "mouse at top pans fully up");

            state.Step(Dt, 5000f, Right, 0.25f);
            Assert.AreEqual(-67.5f, state.VerticalOffset, 0.001f, "linear in between");

            state.Step(Dt, 5000f, Right, -3f);
            Assert.AreEqual(-135f, state.VerticalOffset, 0.001f, "off-screen mouse is clamped");
        }
    }
}
