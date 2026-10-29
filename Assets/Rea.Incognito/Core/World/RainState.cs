using System;
using System.Collections.Generic;
using Incognito.Settings;

namespace Incognito.World
{
    /// <summary>
    /// obj_rain_emitter + obj_raindrop (RECONSTRUCTION.md §7), in original pixels (y down): every
    /// step the emitter creates drops at a random x across the current view and a random height up
    /// to one room above the top; each falls and drifts, and is destroyed once below the room.
    /// Drop count, size, drift/fall range and draw alpha come from a validated
    /// <see cref="RainTuning"/> (U.INC-T22) instead of constants. Rolls go through an injectable
    /// inclusive <c>irandom_range</c> so tests can pin them.
    /// </summary>
    public sealed class RainState
    {
        public struct Drop
        {
            public float X;
            public float Y;
            public float DX;
            public float DY;
        }

        public float RoomHeight;
        public int DropsPerStep;
        public int DropHeight;
        public int DriftMin;
        public int DriftMax;
        public int FallMin;
        public int FallMax;
        public float Alpha;

        public IReadOnlyList<Drop> Drops => drops;

        private readonly List<Drop> drops = new List<Drop>();
        private readonly Func<int, int, int> irandomRange;

        /// <param name="irandomRange">Inclusive (min, max) integer roll; defaults to System.Random.</param>
        /// <param name="tuning">Validated tuning (U.INC-T22); null takes the original's own values.</param>
        public RainState(Func<int, int, int> irandomRange = null, RainTuning tuning = null)
        {
            if (irandomRange == null)
            {
                var random = new Random();
                irandomRange = (min, max) => random.Next(Math.Min(min, max), Math.Max(min, max) + 1);
            }
            this.irandomRange = irandomRange;

            RainTuning t = tuning ?? new RainTuning();
            RoomHeight = t.RoomHeight;
            DropsPerStep = t.DropsPerStep;
            DropHeight = t.DropHeight;
            DriftMin = t.DriftMin;
            DriftMax = t.DriftMax;
            FallMin = t.FallMin;
            FallMax = t.FallMax;
            Alpha = t.Alpha;
        }

        /// <summary>Advances whole steps with the view's left edge and width (original px).</summary>
        public void Step(int steps, float viewLeft, float viewWidth)
        {
            for (int s = 0; s < steps; s++)
            {
                for (int i = 0; i < DropsPerStep; i++)
                {
                    drops.Add(new Drop
                    {
                        // x = irandom_range(view_xview, view_xview + view_wview)
                        X = irandomRange((int)viewLeft, (int)(viewLeft + viewWidth)),
                        // y = irandom_range(-sprite_height, -room_height)
                        Y = irandomRange(-(int)RoomHeight, -DropHeight),
                        DX = irandomRange(DriftMin, DriftMax),
                        DY = irandomRange(FallMin, FallMax),
                    });
                }

                for (int i = drops.Count - 1; i >= 0; i--)
                {
                    Drop d = drops[i];
                    d.X += d.DX;
                    d.Y += d.DY;
                    if (d.Y > RoomHeight) drops.RemoveAt(i);
                    else drops[i] = d;
                }
            }
        }
    }
}
