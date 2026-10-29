namespace Incognito.Controls
{
    /// <summary>
    /// One frame's raw input, engine-free (U.INC-T23). Runtime's <c>GameInput</c> maps the Unity
    /// Input System's devices onto this — walk, sprint, draw, aim, fire, console, door, cursor
    /// mode and wheel, the debug keys — once per frame, and every other script reads it from
    /// there instead of touching a device (or the legacy <c>UnityEngine.Input</c> class, disabled
    /// by <c>activeInputHandler</c>) itself. Replaces the 32 scattered legacy Input Manager calls
    /// with the one state object the rules and views actually read, the same way
    /// <see cref="Incognito.World.PlayerControls"/> already carried the player's held keys.
    /// </summary>
    public struct InputSnapshot
    {
        /// <summary>Left arrow or A held. Both held (with <see cref="MoveRightHeld"/>) cancel out,
        /// as <c>PlayerLocomotionState.Step</c>'s own moveLeft&amp;&amp;moveRight check already does.</summary>
        public bool MoveLeftHeld;

        /// <summary>Right arrow or D held.</summary>
        public bool MoveRightHeld;

        /// <summary>Either Shift held: sprinting (while free to walk) and the weapon's wider arm
        /// swing (whether typing or not) both read this.</summary>
        public bool ShiftHeld;

        /// <summary>Z pressed this frame: draws or holsters the gun.</summary>
        public bool DrawPressed;

        /// <summary>The right mouse button pressed this frame: aims with the gun drawn, or cycles
        /// the cursor's look/talk mode while holstered (RECONSTRUCTION.md §3/§9) — the same press,
        /// read both ways.</summary>
        public bool AimPressed;

        /// <summary>The right mouse button released this frame.</summary>
        public bool AimReleased;

        /// <summary>The left mouse button pressed this frame: fires while aiming, or clicks the
        /// world/GUI otherwise — the same press, read both ways.</summary>
        public bool LeftClickPressed;

        /// <summary>The left mouse button currently held (the title screen's "press anything").</summary>
        public bool LeftClickHeld;

        /// <summary>The left mouse button released this frame: stops firing.</summary>
        public bool FireReleased;

        /// <summary>Q pressed this frame: toggles the hacking/console overlay.</summary>
        public bool ConsoleTogglePressed;

        /// <summary>W pressed this frame: uses a door within reach.</summary>
        public bool DoorUsePressed;

        /// <summary>This frame's mouse wheel movement, either direction; 0 when it didn't move.</summary>
        public float WheelDeltaY;

        /// <summary>Escape pressed this frame: quits outside the console, or closes it while
        /// open — the same key, read both ways.</summary>
        public bool EscapePressed;

        /// <summary>F1 pressed this frame: toggles fullscreen.</summary>
        public bool DebugFullscreenTogglePressed;

        /// <summary>F12 pressed this frame: toggles the debug readout.</summary>
        public bool DebugReadoutTogglePressed;

        /// <summary>Any keyboard key pressed this frame, or the left mouse button held — the
        /// title screen's "press anything to start".</summary>
        public bool AnyKeyDown;

        /// <summary>The cursor's screen position in pixels, bottom-left origin (what
        /// <c>UnityEngine.Input.mousePosition</c> read).</summary>
        public Vec2 PointerScreenPosition;
    }
}
