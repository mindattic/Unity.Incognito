using Incognito.Eye;
using UnityEngine;

namespace Incognito.Hacking
{
    /// <summary>
    /// Pure port of obj_gui_console's backdrop fade (RECONSTRUCTION.md §5): spr_circuitwall1
    /// drawn over the whole screen, cross-fading by <c>fadespeed = 0.025</c> per 60fps step
    /// between "off", "fadein" (up to fully opaque — the art itself is ~10% alpha) and "fadeout"
    /// (down to 0, then "off"). The rebuild clamps at 0 and 1 instead of letting the float
    /// overshoot.
    /// </summary>
    public sealed class OverlayFade
    {
        public enum Mode { Off, FadeIn, FadeOut }

        public const float ReferenceFps = 60f;
        public const float FadeSpeed = 0.025f;

        public Mode Current { get; private set; } = Mode.Off;
        public float Alpha { get; private set; }

        private float stepAccumulator;

        public void FadeIn() => Current = Mode.FadeIn;

        public void FadeOut()
        {
            if (Current != Mode.Off) Current = Mode.FadeOut;
        }

        public void Step(float deltaTime)
        {
            stepAccumulator += Mathf.Max(deltaTime, 0f) * ReferenceFps;
            while (stepAccumulator >= 1f)
            {
                stepAccumulator -= 1f;
                StepOnce();
            }
        }

        public void StepOnce()
        {
            switch (Current)
            {
                case Mode.Off:
                    Alpha = 0f;
                    break;
                case Mode.FadeIn:
                    if (Alpha < 1f) Alpha = Mathf.Min(1f, Alpha + FadeSpeed);
                    break;
                case Mode.FadeOut:
                    if (Alpha > 0f) Alpha = Mathf.Max(0f, Alpha - FadeSpeed);
                    else Current = Mode.Off;
                    break;
            }
        }
    }

    /// <summary>
    /// Pure port of obj_program's slide (RECONSTRUCTION.md §5), in original pixels on the
    /// 1080px-tall screen at 60 steps/sec. The icon rests just below the screen
    /// (<c>initialY = room_height</c>); "onscreen" eases it up, <c>y -= 1 + |y| * 0.01</c> per
    /// step, until its bottom edge meets the screen's (<c>endY = room_height - sprite_height</c>);
    /// "offscreen" drops it 10px per step back to rest. obj_icon_pistol's step event is the same
    /// code, so the HUD pistol icon (U.INC-T30) slides with this too.
    /// </summary>
    public sealed class ProgramIconSlide
    {
        public enum Mode { Idle, Onscreen, Offscreen }

        public const float ReferenceFps = 60f;
        public const float OffscreenSpeed = 10f;

        public ProgramIconSlide(float iconHeight, float screenHeight = 1080f)
        {
            InitialY = screenHeight;
            EndY = screenHeight - iconHeight;
            Y = InitialY;
        }

        public float InitialY { get; }
        public float EndY { get; }

        /// <summary>The icon's top edge, from the top of the screen.</summary>
        public float Y { get; private set; }

        public Mode Current { get; private set; } = Mode.Idle;

        /// <summary>Any part of the icon is on screen.</summary>
        public bool Visible => Y < InitialY;

        private float stepAccumulator;

        public void SlideIn()
        {
            // firstPass: y = initialY — but only from rest, so a re-click while it is up doesn't
            // drop it off the screen and replay the rise.
            if (Current == Mode.Idle && !Visible) Y = InitialY;
            if (Y > EndY) Current = Mode.Onscreen;
        }

        public void SlideOut()
        {
            if (Visible) Current = Mode.Offscreen;
        }

        /// <summary>Puts the icon straight at rest up on screen, as a persistent GM instance that
        /// already finished its rise would be when the next room starts.</summary>
        public void Settle()
        {
            Y = EndY;
            Current = Mode.Idle;
        }

        public void Step(float deltaTime)
        {
            stepAccumulator += Mathf.Max(deltaTime, 0f) * ReferenceFps;
            while (stepAccumulator >= 1f)
            {
                stepAccumulator -= 1f;
                StepOnce();
            }
        }

        public void StepOnce()
        {
            if (Current == Mode.Onscreen)
            {
                if (Y > EndY)
                {
                    Y -= 1f + Mathf.Abs(Y) * 0.01f;
                }
                else
                {
                    Y = EndY;
                    Current = Mode.Idle;
                }
            }
            else if (Current == Mode.Offscreen)
            {
                if (Y < InitialY)
                {
                    Y += OffscreenSpeed;
                }
                else
                {
                    Y = InitialY;
                    Current = Mode.Idle;
                }
            }
        }
    }

    /// <summary>
    /// The hacking overlay's rules (RECONSTRUCTION.md §5), engine-free so they can be tested:
    ///
    /// - The console toggle (obj_walker key event 81, Q) flips <see cref="Inverted"/>. On, the
    ///   cursor is forced into pointer mode and the circuit backdrop fades in; off, the cursor
    ///   gets back the mode it had before and the backdrop fades out.
    /// - A pointer-mode click on an NPC hacks it: it becomes the <see cref="Target"/>, the
    ///   program icon rises with its active sprite, and the console opens (obj_npc's pointer
    ///   branch: program[0] onscreen as spr_program_active, console[0] onscreen, view on the NPC).
    /// - Closing the console drops the target; clicking the program icon toggles the console
    ///   on the last hacked NPC (the intent of obj_mouse_gui's commented-out program click).
    ///
    /// Cleanups beyond the original: leaving the console toggle also ends the hack and sinks the
    /// icon (the original left both up), and the mode restored is the one in use when the
    /// toggle went on (the original's <c>last_mode</c> was never updated, so always "look").
    /// </summary>
    public sealed class HackingSession<TTarget> where TTarget : class
    {
        /// <summary>spr_program is 113x86.</summary>
        public const float ProgramIconHeight = 86f;

        public HackingSession()
        {
            Backdrop = new OverlayFade();
            Program = new ProgramIconSlide(ProgramIconHeight);
        }

        /// <summary>obj_walker's global <c>inverted</c>: the console view is up.</summary>
        public bool Inverted { get; private set; }

        /// <summary>The NPC being hacked, or null.</summary>
        public TTarget Target { get; private set; }

        /// <summary>The last NPC hacked while the console view was up; the program icon reopens
        /// the console on it.</summary>
        public TTarget LastTarget { get; private set; }

        /// <summary>The cursor mode to restore when the console view goes down.</summary>
        public EyeCursorMode ModeBeforeInverted { get; private set; } = EyeCursorMode.Look;

        public OverlayFade Backdrop { get; }
        public ProgramIconSlide Program { get; }

        /// <summary>spr_program_active while a hack is on, spr_program otherwise.</summary>
        public bool ProgramActive => Target != null;

        /// <summary>Q: flips the console view and returns the cursor mode to apply.</summary>
        public EyeCursorMode ToggleInverted(EyeCursorMode currentMode)
        {
            Inverted = !Inverted;
            if (Inverted)
            {
                ModeBeforeInverted = currentMode == EyeCursorMode.Pointer ? EyeCursorMode.Look : currentMode;
                Backdrop.FadeIn();
                return EyeCursorMode.Pointer;
            }

            Target = null;
            LastTarget = null;
            Backdrop.FadeOut();
            Program.SlideOut();
            return ModeBeforeInverted;
        }

        /// <summary>A pointer-mode click on <paramref name="npc"/>. Returns true when it starts
        /// (or retargets) a hack, i.e. the console should open and the view centre on it.</summary>
        public bool PointerClicked(TTarget npc)
        {
            if (!Inverted || npc == null) return false;
            Target = npc;
            LastTarget = npc;
            Program.SlideIn();
            return true;
        }

        /// <summary>The console closed (Escape, or the program icon): the hack ends, the icon
        /// stays up as spr_program, and the view goes back to the cursor.</summary>
        public void ConsoleClosed()
        {
            Target = null;
        }

        /// <summary>
        /// A click on the program icon. Returns the NPC to reopen the console on, or null when
        /// the click should close the console (one is open) or do nothing (no hack yet).
        /// </summary>
        public TTarget ProgramClicked(bool consoleOpen)
        {
            if (!Inverted) return null;
            if (consoleOpen)
            {
                ConsoleClosed();
                return null;
            }

            if (LastTarget != null) Target = LastTarget;
            return Target;
        }

        public void Step(float deltaTime)
        {
            Backdrop.Step(deltaTime);
            Program.Step(deltaTime);
        }
    }

    /// <summary>What a left click on the GUI layer hit (obj_mouse_gui, RECONSTRUCTION.md §5).</summary>
    public enum GuiClick { None, ProgramIcon, Console, PistolIcon }

    /// <summary>
    /// obj_mouse_gui's left-press chain (U.INC-T30): <c>if (collision_point(obj_program)) ... else
    /// if (obj_console) ... else if (obj_icon_pistol) toggle_gun();</c> — the first GUI element under
    /// the pointer, in that order, takes the click. A program icon or console click plays
    /// <c>sfx_flick</c>; a pistol icon click plays nothing itself (toggle_gun's own sfx_reload or
    /// sfx_holster sounds).
    /// </summary>
    public static class MouseGui
    {
        public static GuiClick Resolve(bool overProgramIcon, bool overConsole, bool overPistolIcon)
        {
            if (overProgramIcon) return GuiClick.ProgramIcon;
            if (overConsole) return GuiClick.Console;
            if (overPistolIcon) return GuiClick.PistolIcon;
            return GuiClick.None;
        }

        public static bool PlaysFlick(GuiClick click) => click == GuiClick.ProgramIcon || click == GuiClick.Console;
    }

    /// <summary>obj_console's "CPU: x/y%" readout (RECONSTRUCTION.md §5).</summary>
    public static class CpuReadout
    {
        /// <summary>globals.gml: cpu_power = 100.</summary>
        public const int DefaultCpuPower = 100;

        /// <summary>
        /// <c>active_cpus &gt; 0 ? "CPU: " + round(cpu_power / active_cpus) + "/" + cpu_power + "%"
        /// : "CPU: " + cpu_power + "/" + cpu_power + "%"</c>.
        /// </summary>
        public static string Format(int cpuPower, int activeCpus)
        {
            int share = activeCpus > 0 ? Mathf.RoundToInt((float)cpuPower / activeCpus) : cpuPower;
            return $"CPU: {share}/{cpuPower}%";
        }
    }
}
