using System.Collections.Generic;

namespace Incognito.Terminal
{
    /// <summary>
    /// What "in vicinity" means for the console (the original help text's own word). The
    /// original's <c>list sheep</c> walked every NPC in the room; the rebuild narrows that to
    /// objects within one view width of the player — the same outer range the NPC suspicion
    /// cue uses (RECONSTRUCTION.md §4, <c>distance &lt;= view_wview</c>), so "nearby" means the
    /// same thing in both systems. Pure so it can be unit tested without a scene.
    /// </summary>
    public static class Vicinity
    {
        /// <summary>Original view width in original pixels (view_wview).</summary>
        public const float DefaultRadius = 1920f;

        /// <summary>Objects whose position is within <paramref name="radius"/> of <paramref name="origin"/>, in input order.</summary>
        public static IEnumerable<ConsoleObject> Within(
            Vec2 origin, float radius, IEnumerable<KeyValuePair<ConsoleObject, Vec2>> candidates)
        {
            float radiusSq = radius * radius;
            foreach (var candidate in candidates)
            {
                float dx = candidate.Value.x - origin.x;
                float dy = candidate.Value.y - origin.y;
                if (dx * dx + dy * dy <= radiusSq) yield return candidate.Key;
            }
        }
    }
}
