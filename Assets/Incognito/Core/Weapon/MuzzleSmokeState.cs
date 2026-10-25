using System;
using System.Collections.Generic;
using Incognito.Settings;

namespace Incognito.Weapon
{
    /// <summary>
    /// shoot_bullet.gml's <c>effect_create_below(ef_smoke, originX, originY, 1, c_black)</c>:
    /// ef_smoke is a GameMaker built-in particle effect, not a script or asset in the original
    /// project, so there is nothing to read sizes or timings from verbatim. This reproduces its
    /// puff — a handful of small black particles that drift sideways and rise from the muzzle while
    /// fading out — as a pure, engine-loop-free simulation, the same split as <see cref="Incognito.World.RainState"/>.
    /// Spawned once per shot (<see cref="Incognito.Weapon.MuzzleSmoke"/>, driven by
    /// <see cref="Incognito.Weapon.PlayerWeapon.Fire"/>) so every shot puffs, closing U.INC-T41.
    /// </summary>
    public sealed class MuzzleSmokeState
    {
        public struct Puff
        {
            /// <summary>Offset from the muzzle, original px, y up.</summary>
            public float X;
            public float Y;
            public float DX;
            public float DY;

            /// <summary>Whole 60Hz steps since this puff spawned.</summary>
            public int Age;
        }

        /// <summary>How long a puff lives, in 60Hz steps.</summary>
        public readonly int LifeSteps;

        public IReadOnlyList<Puff> Puffs => puffs;

        /// <summary>True once every puff has faded out; the owner destroys itself on this.</summary>
        public bool IsDone => puffs.Count == 0;

        private readonly List<Puff> puffs;

        /// <param name="frandomRange">Inclusive (min, max) float roll; defaults to System.Random.</param>
        /// <param name="tuning">Validated tuning (U.INC-T22); null takes the original's own values.</param>
        public MuzzleSmokeState(Func<float, float, float> frandomRange = null, WeaponTuning tuning = null)
        {
            if (frandomRange == null)
            {
                var random = new Random();
                frandomRange = (min, max) => min + (float)random.NextDouble() * (max - min);
            }

            WeaponTuning t = tuning ?? new WeaponTuning();
            LifeSteps = t.MuzzleSmokeLifeSteps;

            puffs = new List<Puff>(Math.Max(t.MuzzleSmokeParticles, 0));
            for (int i = 0; i < t.MuzzleSmokeParticles; i++)
            {
                puffs.Add(new Puff
                {
                    X = 0f,
                    Y = 0f,
                    DX = frandomRange(t.MuzzleSmokeDriftMin, t.MuzzleSmokeDriftMax),
                    DY = frandomRange(t.MuzzleSmokeRiseMin, t.MuzzleSmokeRiseMax),
                    Age = 0,
                });
            }
        }

        /// <summary>Advances whole 60Hz steps, same as <see cref="Incognito.World.RainState.Step"/>.</summary>
        public void Step(int steps)
        {
            for (int s = 0; s < steps; s++)
            {
                for (int i = puffs.Count - 1; i >= 0; i--)
                {
                    Puff p = puffs[i];
                    p.X += p.DX;
                    p.Y += p.DY;
                    p.Age += 1;
                    if (p.Age >= LifeSteps) puffs.RemoveAt(i);
                    else puffs[i] = p;
                }
            }
        }

        /// <summary>1 (just spawned) fading linearly to 0 as the puff's age reaches <see cref="LifeSteps"/>.</summary>
        public float Alpha(in Puff puff) => CoreMath.Clamp(1f - (float)puff.Age / LifeSteps, 0f, 1f);
    }
}
