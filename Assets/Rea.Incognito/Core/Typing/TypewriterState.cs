
namespace Incognito.Typing
{
    /// <summary>
    /// Pure simulation of obj_typewriter (RECONSTRUCTION.md §4/§6, scripts/type_text.gml): every
    /// <see cref="TypeSpeedSteps"/> 60fps steps (alarm[0] = type_speed = 2) one more character
    /// is revealed; once the whole text shows, each further tick fades it by
    /// <see cref="FadePerTick"/> (alpha -= 0.01) until it is gone and the instance destroys
    /// itself. The tick that reveals the last character already fades by one notch, as the
    /// original's did.
    /// </summary>
    public sealed class TypewriterState
    {
        public const float ReferenceFps = 60f;

        public float TypeSpeedSteps = 2f; // type_speed = 2
        public float FadePerTick = 0.01f; // alpha -= 0.01

        public string Text { get; }
        public int Revealed { get; private set; }
        public float Alpha { get; private set; } = 1f;
        public bool FadingOut { get; private set; }
        public bool Done { get; private set; }

        /// <summary>The text typed so far (the original's copystring).</summary>
        public string Shown => Text.Substring(0, Revealed);

        private float stepsSinceTick;

        public TypewriterState(string text)
        {
            Text = text ?? string.Empty;
        }

        public void Step(float deltaTime)
        {
            if (Done) return;

            stepsSinceTick += CoreMath.Max(deltaTime, 0f) * ReferenceFps;
            while (!Done && stepsSinceTick >= TypeSpeedSteps)
            {
                stepsSinceTick -= TypeSpeedSteps;
                Tick();
            }
        }

        private void Tick()
        {
            if (Revealed < Text.Length)
            {
                Revealed++;
                if (Revealed == Text.Length) FadingOut = true; // "Text Writing Complete"
            }
            else
            {
                FadingOut = true;
            }

            if (!FadingOut) return;

            Alpha -= FadePerTick;
            if (Alpha <= 0f)
            {
                Alpha = 0f;
                Done = true;
            }
        }
    }
}
