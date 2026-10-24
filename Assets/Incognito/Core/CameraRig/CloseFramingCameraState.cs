using Incognito.Player;
using Incognito.Settings;

namespace Incognito.CameraRig
{
    /// <summary>
    /// Pure, engine-loop-free simulation of the original obj_camera close framing
    /// (RECONSTRUCTION.md §2), in original GameMaker pixel units.
    ///
    /// Horizontal: the camera target is pinned half a view minus one player-width ahead of the
    /// player in the direction they face, so the player sits near the trailing edge with room
    /// to walk into. Near the room ends the target falls back to the player's own x. The view
    /// then follows that target the way GM8's view_object follow did in street00 (hborder=910
    /// in a 1920 view, i.e. a 50px deadzone either side of centre, at most hspeed=20px per
    /// step) and is clamped to the room, which is what makes a facing flip read as a quick
    /// swing rather than a cut.
    ///
    /// Vertical: the original set y = mouse_y, but its room was exactly as tall as the view, so
    /// that pan never actually moved anything. The rebuild's chosen interpretation (BIBLE.md
    /// open question, decided in U.INC-T4) is a direct, instant, bounded pan: the mouse's
    /// height on screen maps linearly onto ±MaxVerticalPan around the camera's resting height.
    /// </summary>
    public sealed class CloseFramingCameraState
    {
        // GM8 view speeds were per 60fps step; scaled by deltaTime like PlayerLocomotionState.
        public const float ReferenceFps = PlayerLocomotionState.ReferenceFps;

        /// <summary>View width (original: wview = 1920).</summary>
        public float ViewWidth = 1920f;

        /// <summary>Original: player_width = sprite_get_width(spr_player) = 512.</summary>
        public float PlayerWidth = 512f;

        /// <summary>Room extent (original street00: 0..11520).</summary>
        public float RoomMinX = 0f;
        public float RoomMaxX = 11520f;

        /// <summary>Half-width of the follow deadzone around view centre (original: wview/2 - hborder = 960 - 910).</summary>
        public float HorizontalDeadzone = 50f;

        /// <summary>Max view travel per step (original: hspeed = 20). Zero or negative means instant.</summary>
        public float HorizontalPanSpeed = 20f;

        /// <summary>How far the view may pan up/down from rest when the mouse is at the screen top/bottom.</summary>
        public float MaxVerticalPan = 135f;

        public float CenterX { get; private set; }
        public float VerticalOffset { get; private set; }

        /// <summary>
        /// Builds from a validated <see cref="CameraTuning"/> (U.INC-T22); null takes the
        /// original's own values, same as every field's own default above (so the parameterless
        /// object-initializer form tests use still works unchanged). Room bounds are per-room
        /// scene data, not tuning, so they keep their own field defaults here.
        /// </summary>
        public CloseFramingCameraState(CameraTuning tuning = null)
        {
            CameraTuning t = tuning ?? new CameraTuning();
            ViewWidth = t.ViewWidth;
            PlayerWidth = t.PlayerWidth;
            HorizontalDeadzone = t.HorizontalDeadzone;
            HorizontalPanSpeed = t.HorizontalPanSpeed;
            MaxVerticalPan = t.MaxVerticalPan;
        }

        /// <summary>Where obj_camera itself would sit this step (before the view-follow lag).</summary>
        public float TargetX(float playerX, PlayerLocomotionState.FacingDirection facing)
        {
            float halfView = ViewWidth * 0.5f;
            bool awayFromRoomEnds =
                playerX >= RoomMinX + halfView + PlayerWidth &&
                playerX < RoomMaxX - halfView - PlayerWidth;

            if (!awayFromRoomEnds)
            {
                return playerX;
            }

            return facing == PlayerLocomotionState.FacingDirection.Left
                ? playerX - halfView + PlayerWidth
                : playerX + halfView - PlayerWidth;
        }

        /// <summary>Jumps straight to the framed position (scene start), skipping the pan lag.</summary>
        public void SnapTo(float playerX, PlayerLocomotionState.FacingDirection facing, float mouseViewportY = 0.5f)
        {
            CenterX = ClampToRoom(TargetX(playerX, facing));
            VerticalOffset = VerticalPanFor(mouseViewportY);
        }

        /// <param name="mouseViewportY">Mouse height on screen, 0 = bottom, 1 = top.</param>
        public void Step(float deltaTime, float playerX, PlayerLocomotionState.FacingDirection facing, float mouseViewportY)
        {
            StepToward(deltaTime, TargetX(playerX, facing), HorizontalDeadzone, HorizontalPanSpeed, mouseViewportY);
        }

        /// <summary>
        /// GM8's view_object follow for an arbitrary followed x (U.INC-T7: obj_walker's console
        /// toggle points view_object[0] at the cursor, and an NPC pointer click points it at the
        /// NPC): the view moves only once <paramref name="targetX"/> is more than
        /// <paramref name="deadzone"/> (view_wview/2 - view_hborder) from its centre, by at most
        /// <paramref name="panSpeed"/> (view_hspeed) per step, clamped to the room.
        /// </summary>
        public void StepToward(float deltaTime, float targetX, float deadzone, float panSpeed, float mouseViewportY)
        {
            float steps = CoreMath.Max(deltaTime, 0f) * ReferenceFps;
            deadzone = CoreMath.Max(deadzone, 0f);

            float desired = CenterX;
            if (targetX > CenterX + deadzone)
            {
                desired = targetX - deadzone;
            }
            else if (targetX < CenterX - deadzone)
            {
                desired = targetX + deadzone;
            }

            CenterX = panSpeed > 0f
                ? CoreMath.MoveTowards(CenterX, desired, panSpeed * steps)
                : desired;
            CenterX = ClampToRoom(CenterX);

            VerticalOffset = VerticalPanFor(mouseViewportY);
        }

        /// <summary>The cursor's room x (GM's mouse_x) for a mouse at
        /// <paramref name="mouseViewportX"/> across the view (0 = left edge, 1 = right edge).</summary>
        public float CursorX(float mouseViewportX) => CenterX + (mouseViewportX - 0.5f) * ViewWidth;

        private float ClampToRoom(float centerX)
        {
            float halfView = ViewWidth * 0.5f;
            float min = RoomMinX + halfView;
            float max = RoomMaxX - halfView;
            if (min > max)
            {
                return (RoomMinX + RoomMaxX) * 0.5f;
            }
            return CoreMath.Clamp(centerX, min, max);
        }

        private float VerticalPanFor(float mouseViewportY)
        {
            float normalized = CoreMath.Clamp(mouseViewportY * 2f - 1f, -1f, 1f);
            return normalized * MaxVerticalPan;
        }
    }
}
