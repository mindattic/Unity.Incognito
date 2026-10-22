using System.Collections.Generic;
using UnityEngine;

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
            Vector2 origin, float radius, IEnumerable<(Vector2 position, ConsoleObject obj)> candidates)
        {
            float radiusSq = radius * radius;
            foreach (var (position, obj) in candidates)
            {
                if ((position - origin).sqrMagnitude <= radiusSq) yield return obj;
            }
        }
    }
}
