using System;

namespace Incognito.World
{
    /// <summary>
    /// <see cref="PlayerControls"/> as they travel from an online player to the authority (U.INC-T43):
    /// the four held keys packed into one int beside the cursor's height and the view's width, and
    /// unpacked on the server with every number made safe, since a client's bytes are never trusted
    /// (a NaN, an infinity or a cursor far outside the view can't reach the rules).
    /// </summary>
    public static class OnlineControls
    {
        public const int MoveLeftBit = 1;
        public const int MoveRightBit = 2;
        public const int SprintBit = 4;
        public const int ShiftHeldBit = 8;
        public const int LookingDownBit = 16;

        /// <summary>The widest view a client may claim, original px (four reference views).</summary>
        public const float MaxViewWidth = 1920f * 4f;

        /// <summary>The held keys and looking-down flag of <paramref name="controls"/> as bits.</summary>
        public static int PackKeys(PlayerControls controls) =>
            (controls.MoveLeft ? MoveLeftBit : 0)
            | (controls.MoveRight ? MoveRightBit : 0)
            | (controls.Sprint ? SprintBit : 0)
            | (controls.ShiftHeld ? ShiftHeldBit : 0)
            | (controls.LookingDown ? LookingDownBit : 0);

        /// <summary>
        /// Rebuilds the controls a client sent. <paramref name="mouseBelowCentre"/> is clamped to half of
        /// <paramref name="viewHeight"/> either way (the cursor can't leave the view); a view width that is
        /// not a positive number reads as the reference 1920 and is capped at <see cref="MaxViewWidth"/>.
        /// </summary>
        public static PlayerControls Unpack(int keys, float mouseBelowCentre, float viewWidth, float viewHeight = 1080f)
        {
            float half = viewHeight > 0f && !float.IsInfinity(viewHeight) ? viewHeight / 2f : 540f;
            float below = float.IsNaN(mouseBelowCentre) ? 0f : Math.Max(-half, Math.Min(half, mouseBelowCentre));
            float width = float.IsNaN(viewWidth) || viewWidth <= 0f ? 1920f : Math.Min(viewWidth, MaxViewWidth);

            return new PlayerControls(
                moveLeft: (keys & MoveLeftBit) != 0,
                moveRight: (keys & MoveRightBit) != 0,
                sprint: (keys & SprintBit) != 0,
                shiftHeld: (keys & ShiftHeldBit) != 0,
                mouseBelowCentre: below,
                lookingDown: (keys & LookingDownBit) != 0,
                viewWidth: width);
        }
    }
}
