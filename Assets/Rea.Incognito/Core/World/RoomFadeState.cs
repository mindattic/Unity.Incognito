namespace Incognito.World
{
    /// <summary>
    /// obj_fade (RECONSTRUCTION.md §7): a full-screen black overlay. Every room starts opaque and
    /// fades out ("fadeout", 0.03 alpha per step, then "off"); leaving a room fades it back in
    /// ("fadein") and, once opaque, the next room loads. The original's unused "fadehalf" mode is
    /// not ported.
    /// </summary>
    public sealed class RoomFadeState
    {
        public enum FadeMode { Off, FadeIn, FadeOut }

        public float FadeSpeed = 0.03f;

        public FadeMode Mode { get; private set; } = FadeMode.FadeOut;
        public float Alpha { get; private set; } = 1f;

        /// <summary>True once a fade-in has reached opaque: time to load the next room.</summary>
        public bool ReadyToLoad { get; private set; }

        /// <summary>Starts the fade to black before a room change (obj_exit / obj_exit_side).</summary>
        public void FadeIn() => Mode = FadeMode.FadeIn;

        /// <summary>Advances whole original steps.</summary>
        public void Step(int steps)
        {
            for (int i = 0; i < steps; i++) StepOnce();
        }

        private void StepOnce()
        {
            switch (Mode)
            {
                case FadeMode.Off:
                    Alpha = 0f;
                    break;

                case FadeMode.FadeIn:
                    // if (image_alpha < 1) image_alpha += fadespeed; else room_goto(next_room);
                    if (Alpha < 1f) Alpha = System.Math.Min(1f, Alpha + FadeSpeed);
                    else ReadyToLoad = true;
                    break;

                case FadeMode.FadeOut:
                    // if (image_alpha > 0) image_alpha -= fadespeed; else mode = "off";
                    if (Alpha > 0f) Alpha = System.Math.Max(0f, Alpha - FadeSpeed);
                    else Mode = FadeMode.Off;
                    break;
            }
        }
    }
}
