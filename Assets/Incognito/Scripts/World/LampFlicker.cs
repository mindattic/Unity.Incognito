using System;
using UnityEngine;

namespace Incognito.World
{
    /// <summary>
    /// obj_prop_lamppost's light: every step an irandom_range(0, 80) roll of 0 blanks
    /// spr_lamplight01 for that step, so the pooled light under each lamppost stutters now and
    /// then. The light is drawn 100px above the post's top (draw_sprite_ext(..., x, y - 100, ...)).
    /// </summary>
    public sealed class LampFlicker : MonoBehaviour
    {
        public const int Odds = 80;

        [SerializeField] private SpriteRenderer lightRenderer;

        private readonly StepClock clock = new StepClock();
        private readonly System.Random random = new System.Random();

        /// <summary>One step's light alpha for a roll in [0, <see cref="Odds"/>].</summary>
        public static float LightAlpha(int roll) => roll == 0 ? 0f : 1f;

        /// <summary>The light alpha after <paramref name="steps"/> rolls (only the last counts).</summary>
        public static float Roll(int steps, Func<int, int, int> irandomRange, float current)
        {
            for (int i = 0; i < steps; i++) current = LightAlpha(irandomRange(0, Odds));
            return current;
        }

        public void Configure(SpriteRenderer lightRenderer) => this.lightRenderer = lightRenderer;

        private void Update()
        {
            if (lightRenderer == null) return;
            int steps = clock.Advance(Time.deltaTime);
            if (steps == 0) return;
            float a = Roll(steps, (min, max) => random.Next(min, max + 1), lightRenderer.color.a);
            lightRenderer.color = new Color(1f, 1f, 1f, a);
        }
    }
}
