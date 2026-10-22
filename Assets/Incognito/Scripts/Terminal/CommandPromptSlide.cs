using UnityEngine;

namespace Incognito.Terminal
{
    /// <summary>
    /// Pure port of obj_command_prompt's slide-in/slide-out (Step event), in original pixels
    /// at the original 60 steps/sec. The panel rests fully off the left edge (x = -width).
    /// Opening eases in, <c>x += 1 + ceil(|x| * 0.1)</c> per step, until it reaches x = 0;
    /// closing slides out linearly at 20px per step.
    /// </summary>
    public sealed class CommandPromptSlide
    {
        public enum Mode { Off, Onscreen, Idle, Offscreen }

        public const float ReferenceFps = 60f;
        public const float OffscreenSpeed = 20f;

        public CommandPromptSlide(float width)
        {
            Width = width;
            X = -width;
        }

        /// <summary>Panel width (original spr_command_prompt: 580).</summary>
        public float Width { get; }

        /// <summary>Panel left edge; -Width when hidden, 0 when fully open.</summary>
        public float X { get; private set; }

        public Mode Current { get; private set; } = Mode.Off;

        /// <summary>True from the moment it starts opening until it has fully slid away.</summary>
        public bool IsOpen => Current != Mode.Off;

        private float stepAccumulator;

        public void Open()
        {
            if (Current == Mode.Off) X = -Width; // original firstPass reset
            if (Current != Mode.Idle) Current = Mode.Onscreen;
        }

        public void Close()
        {
            if (Current != Mode.Off) Current = Mode.Offscreen;
        }

        /// <summary>Advances by whole 60fps steps, carrying the remainder to the next call.</summary>
        public void Step(float deltaTime)
        {
            stepAccumulator += deltaTime * ReferenceFps;
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
                if (X < 0f)
                {
                    X += 1f + Mathf.Ceil(Mathf.Abs(X) * 0.1f);
                }
                else
                {
                    X = 0f;
                    Current = Mode.Idle;
                }
            }
            else if (Current == Mode.Offscreen)
            {
                if (X > -Width)
                {
                    X -= OffscreenSpeed;
                }
                else
                {
                    X = -Width;
                    Current = Mode.Off;
                }
            }
        }
    }
}
