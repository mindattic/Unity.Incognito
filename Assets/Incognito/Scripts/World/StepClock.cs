namespace Incognito.World
{
    /// <summary>
    /// Turns frame time into whole original 60Hz steps, so per-step random rolls (rain spawns,
    /// lamp flicker) and per-step ramps keep the original's odds and speeds at any frame rate.
    /// </summary>
    public sealed class StepClock
    {
        public const float ReferenceFps = 60f;

        private float pending;

        /// <summary>Adds <paramref name="deltaTime"/> seconds and returns how many whole steps
        /// have now elapsed (the remainder carries over).</summary>
        public int Advance(float deltaTime)
        {
            if (deltaTime > 0f) pending += deltaTime * ReferenceFps;
            int steps = (int)pending;
            pending -= steps;
            return steps;
        }
    }
}
