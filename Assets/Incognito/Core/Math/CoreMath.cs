using System;

namespace Incognito
{
    /// <summary>
    /// Engine-free stand-ins for the <c>UnityEngine.Mathf</c> calls the rules use, with the same
    /// semantics (RoundToInt rounds half to even, Sign(0) is 1, MoveTowards never overshoots).
    /// </summary>
    public static class CoreMath
    {
        public const float Deg2Rad = (float)(Math.PI * 2.0 / 360.0);

        public static float Max(float a, float b) => a > b ? a : b;
        public static float Min(float a, float b) => a < b ? a : b;
        public static float Abs(float v) => Math.Abs(v);
        public static float Pow(float v, float p) => (float)Math.Pow(v, p);
        public static float Cos(float v) => (float)Math.Cos(v);
        public static float Sin(float v) => (float)Math.Sin(v);
        public static float Sign(float v) => v >= 0f ? 1f : -1f;
        public static int RoundToInt(float v) => (int)Math.Round(v);

        public static float Clamp(float v, float min, float max) => v < min ? min : v > max ? max : v;

        public static float MoveTowards(float current, float target, float maxDelta) =>
            Abs(target - current) <= maxDelta ? target : current + Sign(target - current) * maxDelta;
    }
}
