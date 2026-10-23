using System;
using System.Collections.Generic;

namespace Incognito.World
{
    /// <summary>
    /// obj_rain_emitter + obj_raindrop (RECONSTRUCTION.md §7), in original pixels (y down): every
    /// step the emitter creates 6 drops at a random x across the current view and a random height
    /// up to one room above the top; each falls 40–50px and drifts 3–6px left per step, and is
    /// destroyed once below the room. Rolls go through an injectable inclusive
    /// <c>irandom_range</c> so tests can pin them.
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

        public const int DropsPerStep = 6;
        public const int DropHeight = 4; // spr_rain is 1x4

        public float RoomHeight = 1080f;

        public IReadOnlyList<Drop> Drops => drops;

        private readonly List<Drop> drops = new List<Drop>();
        private readonly Func<int, int, int> irandomRange;

        /// <param name="irandomRange">Inclusive (min, max) integer roll; defaults to System.Random.</param>
        public RainState(Func<int, int, int> irandomRange = null)
        {
            if (irandomRange == null)
            {
                var random = new Random();
                irandomRange = (min, max) => random.Next(Math.Min(min, max), Math.Max(min, max) + 1);
            }
            this.irandomRange = irandomRange;
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
                        DX = irandomRange(-6, -3),
                        DY = irandomRange(40, 50),
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
