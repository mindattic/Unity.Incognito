using UnityEngine;
using UnityEngine.InputSystem;

namespace Incognito.Controls
{
    /// <summary>
    /// The one place that reads the Unity Input System (U.INC-T23): maps its devices to the
    /// game's actions — walk, sprint, draw, aim, fire, console, door, cursor mode, wheel and the
    /// debug keys — in code (AGENTS.md law 5: nothing hand-wired in a .inputactions asset), once
    /// per frame, as an engine-free <see cref="InputSnapshot"/> (<see cref="Current"/>) every
    /// other script reads instead of a device or <c>UnityEngine.Input</c> (disabled project-wide
    /// by <c>activeInputHandler</c>) itself. Installed once, before the first scene loads, and
    /// kept across scene changes, like <see cref="World.DebugHotkeys"/>; runs well before any
    /// other script's Update so this frame's snapshot is ready when they read it.
    /// </summary>
    [DefaultExecutionOrder(-2000)]
    public sealed class GameInput : MonoBehaviour
    {
        /// <summary>This frame's input. Idle (every field false/zero) before the first Update, or
        /// with neither a keyboard nor a mouse attached (a headless test run).</summary>
        public static InputSnapshot Current { get; private set; }

        /// <summary>
        /// <see cref="Current"/>'s pointer position as a UnityEngine type, for the call sites that
        /// need one (a world raycast, a GUI rect) — a thin, engine-aware wrapper over the
        /// engine-free snapshot, not a second source of input.
        /// </summary>
        public static Vector2 PointerScreenPosition =>
            new Vector2(Current.PointerScreenPosition.x, Current.PointerScreenPosition.y);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (FindAnyObjectByType<GameInput>() != null) return;
            var host = new GameObject("GameInput");
            DontDestroyOnLoad(host);
            host.AddComponent<GameInput>();
        }

        private void Update()
        {
            Current = Poll();
        }

        /// <summary>
        /// Reads this frame's keyboard and mouse into a snapshot. A static function (not instance
        /// state) so it needs no scene and can't drift from what <see cref="Current"/> actually
        /// holds; a missing device reads as idle rather than throwing.
        /// </summary>
        public static InputSnapshot Poll()
        {
            Keyboard kb = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (kb == null && mouse == null) return default;

            bool leftClickHeld = mouse != null && mouse.leftButton.isPressed;

            return new InputSnapshot
            {
                MoveLeftHeld = kb != null && (kb.leftArrowKey.isPressed || kb.aKey.isPressed),
                MoveRightHeld = kb != null && (kb.rightArrowKey.isPressed || kb.dKey.isPressed),
                ShiftHeld = kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed),
                DrawPressed = kb != null && kb.zKey.wasPressedThisFrame,
                AimPressed = mouse != null && mouse.rightButton.wasPressedThisFrame,
                AimReleased = mouse != null && mouse.rightButton.wasReleasedThisFrame,
                LeftClickPressed = mouse != null && mouse.leftButton.wasPressedThisFrame,
                LeftClickHeld = leftClickHeld,
                FireReleased = mouse != null && mouse.leftButton.wasReleasedThisFrame,
                ConsoleTogglePressed = kb != null && kb.qKey.wasPressedThisFrame,
                DoorUsePressed = kb != null && kb.wKey.wasPressedThisFrame,
                WheelDeltaY = mouse != null ? mouse.scroll.ReadValue().y : 0f,
                EscapePressed = kb != null && kb.escapeKey.wasPressedThisFrame,
                DebugFullscreenTogglePressed = kb != null && kb.f1Key.wasPressedThisFrame,
                DebugReadoutTogglePressed = kb != null && kb.f12Key.wasPressedThisFrame,
                AnyKeyDown = (kb != null && kb.anyKey.wasPressedThisFrame) || leftClickHeld,
                PointerScreenPosition = mouse != null
                    ? new Vec2(mouse.position.ReadValue().x, mouse.position.ReadValue().y)
                    : Vec2.Zero,
            };
        }
    }
}
