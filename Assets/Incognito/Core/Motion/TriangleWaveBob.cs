using System;

namespace Incognito.Motion
{
    /// <summary>
    /// The original's hand-rolled walk bob (RECONSTRUCTION.md §1), shared verbatim by obj_walker
    /// and obj_npc: a linear ramp between -Amplitude and +Amplitude at RampSpeed per 60fps step,
    /// flipping direction at each extreme. Not a sine — it must not ease at the ends.
    /// </summary>
    public sealed class TriangleWaveBob
    {
        public float Amplitude = 5f;
        public float RampSpeed = 0.3f;

        public float Offset { get; private set; }

        /// <summary>Fires when the bob reaches its top and starts descending — the original's
        /// play_footstep() call site.</summary>
        public event Action OnPeak;

        private bool rising = true;

        /// <summary>Advances the ramp by <paramref name="steps"/> original 60fps steps.</summary>
        public void Advance(float steps)
        {
            float delta = RampSpeed * steps;
            if (rising)
            {
                Offset += delta;
                if (Offset >= Amplitude)
                {
                    Offset = Amplitude;
                    rising = false;
                    OnPeak?.Invoke();
                }
            }
            else
            {
                Offset -= delta;
                if (Offset <= -Amplitude)
                {
                    Offset = -Amplitude;
                    rising = true;
                }
            }
        }

        public void Reset()
        {
            Offset = 0f;
            rising = true;
        }
    }
}
