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
}
