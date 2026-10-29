namespace Incognito.Eye
{
    /// <summary>obj_mouse's <c>mode</c> (RECONSTRUCTION.md §3). Look is the default.</summary>
    public enum EyeCursorMode { Look, Talk, Pointer }

    /// <summary>The four cursor sprites obj_mouse swaps between.</summary>
    public enum EyeCursorSprite { Eye, Talk, Pointer, Reticle }

    public static class EyeCursorSprites
    {
        /// <summary>
        /// obj_mouse Step: aiming overrides everything with spr_cursor_reticle; otherwise
        /// "pointer" -> spr_cursor_pointer, "look" -> spr_cursor_eye, "talk" -> spr_cursor_talk.
        /// </summary>
        public static EyeCursorSprite For(EyeCursorMode mode, bool aiming)
        {
            if (aiming) return EyeCursorSprite.Reticle;
            switch (mode)
            {
                case EyeCursorMode.Pointer: return EyeCursorSprite.Pointer;
                case EyeCursorMode.Talk: return EyeCursorSprite.Talk;
                default: return EyeCursorSprite.Eye;
            }
        }
    }

    public static class EyeCursorModes
    {
        /// <summary>
        /// obj_mouse's mode toggle (U.INC-T6): "look" -> "talk" -> "look". "pointer" is left
        /// alone: it is set by the NPC/console flow (U.INC-T7) and is not reachable by cycling.
        /// </summary>
        public static EyeCursorMode Cycle(EyeCursorMode mode)
        {
            switch (mode)
            {
                case EyeCursorMode.Look: return EyeCursorMode.Talk;
                case EyeCursorMode.Talk: return EyeCursorMode.Look;
                default: return mode;
            }
        }

        /// <summary>
        /// Whether this frame's input cycles the mode: any mouse-wheel movement, or a right-button
        /// press while the gun is holstered. The original toggled on a global right press
        /// (obj_mouse event 54); with the gun out that press aims instead (U.INC-T9).
        /// </summary>
        public static bool ShouldCycle(float wheelDelta, bool rightPressed, bool weaponDrawn)
        {
            return wheelDelta != 0f || (rightPressed && !weaponDrawn);
        }
    }
}
