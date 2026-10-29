namespace Incognito.World
{
    /// <summary>
    /// The pure rules behind the original's exits (RECONSTRUCTION.md §7), in original pixels:
    /// obj_exit_side (an invisible 10px-wide strip just past a room edge that the player walks
    /// into), obj_exit (a floating door icon entered with W within 300px) and obj_barrier.
    /// </summary>
    public static class RoomExitRules
    {
        /// <summary>spr_exit_side / spr_barrier: 10x1080.</summary>
        public const float StripWidth = 10f;

        /// <summary>obj_exit: W only works within this horizontal distance.</summary>
        public const float DoorReach = 300f;

        /// <summary>obj_exit: beyond this the icon fades away entirely.</summary>
        public const float DoorGlimpse = 600f;

        public const float DoorFadeStep = 0.01f;
        public const float DoorGlimpseAlpha = 0.25f;

        /// <summary>
        /// Whether the player's box (centered on <paramref name="playerX"/>, as spr_player's
        /// origin is) overlaps a strip starting at <paramref name="stripX"/>.
        /// </summary>
        public static bool Overlaps(float playerX, float playerWidth, float stripX, float stripWidth = StripWidth)
        {
            float half = playerWidth * 0.5f;
            return playerX + half > stripX && playerX - half < stripX + stripWidth;
        }

        /// <summary>
        /// obj_barrier, as a wall: pushes the player's box back out to the side its center is on.
        /// (The original nudged the player 1px per step along the barrier-to-player direction —
        /// sign-flipped, so it pulled rather than pushed — and never actually stopped anyone.)
        /// </summary>
        public static float Block(float playerX, float playerWidth, float stripX, float stripWidth = StripWidth)
        {
            if (!Overlaps(playerX, playerWidth, stripX, stripWidth)) return playerX;
            float half = playerWidth * 0.5f;
            float stripCenter = stripX + stripWidth * 0.5f;
            return playerX >= stripCenter ? stripX + stripWidth + half : stripX - half;
        }

        /// <summary>obj_exit's W press: only within <see cref="DoorReach"/>, horizontally.</summary>
        public static bool CanUseDoor(float playerX, float doorX) => System.Math.Abs(playerX - doorX) <= DoorReach;

        /// <summary>
        /// obj_exit step: within 300px the icon fades up to opaque; within 600px it hovers around
        /// a quarter; beyond that it fades out. One original step.
        /// </summary>
        public static float DoorAlphaStep(float alpha, float playerX, float doorX)
        {
            float distance = System.Math.Abs(playerX - doorX);
            if (distance <= DoorReach)
            {
                if (alpha < 1f) alpha += DoorFadeStep;
            }
            else if (distance <= DoorGlimpse)
            {
                alpha += alpha < DoorGlimpseAlpha ? DoorFadeStep : -DoorFadeStep;
            }
            else if (alpha > 0f)
            {
                alpha -= DoorFadeStep;
            }

            return System.Math.Max(0f, System.Math.Min(1f, alpha));
        }
    }

    /// <summary>
    /// obj_exit's "gently float up and down" alarm: every 3 steps hoverY ramps by 1 between -5
    /// and +5 (pausing a tick at each end to turn) and is ADDED to y — so the icon drifts in a
    /// smooth arc rather than ticking between two heights. Original pixels, y down.
    /// </summary>
    public sealed class DoorHoverState
    {
        public const int Rate = 3;
        public const int HoverAmount = 5;

        public float OffsetY { get; private set; }

        private bool hoverUp = true;
        private int hoverY;
        private int countdown = Rate;

        public void Step(int steps)
        {
            for (int i = 0; i < steps; i++)
            {
                if (--countdown > 0) continue;
                countdown = Rate;

                if (hoverUp)
                {
                    if (hoverY < HoverAmount) hoverY += 1;
                    else hoverUp = false;
                }
                else
                {
                    if (hoverY > -HoverAmount) hoverY -= 1;
                    else hoverUp = true;
                }

                OffsetY += hoverY;
            }
        }
    }
}
