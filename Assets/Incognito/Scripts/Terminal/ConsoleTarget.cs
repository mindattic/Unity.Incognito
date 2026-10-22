using System;
using System.Collections.Generic;
using UnityEngine;

namespace Incognito.Terminal
{
    /// <summary>
    /// Marks a scene object as visible to the typed console (RECONSTRUCTION.md §6). The id is
    /// authored data, like the original NPC's <c>sheep_id</c> (e.g. "0097"); NPCs (U.INC-T8)
    /// carry one of these with <see cref="ConsoleObjectType.Sheep"/>.
    /// </summary>
    public sealed class ConsoleTarget : MonoBehaviour
    {
        [SerializeField] private ConsoleObjectType type = ConsoleObjectType.Sheep;
        [SerializeField] private string id = "0000";

        private static readonly List<ConsoleTarget> active = new List<ConsoleTarget>();

        /// <summary>Every enabled target in the loaded scenes.</summary>
        public static IReadOnlyList<ConsoleTarget> Active => active;

        /// <summary>Raised when the console's <c>reboot</c> resolves this target.</summary>
        public event Action Rebooted;

        /// <summary>Raised when the console's <c>overwrite</c> resolves this target.</summary>
        public event Action Overwritten;

        public ConsoleObject Object => new ConsoleObject(type, id);

        public void Configure(ConsoleObjectType newType, string newId)
        {
            type = newType;
            id = newId;
        }

        internal void NotifyRebooted() => Rebooted?.Invoke();
        internal void NotifyOverwritten() => Overwritten?.Invoke();

        private void OnEnable()
        {
            if (!active.Contains(this)) active.Add(this);
        }

        private void OnDisable()
        {
            active.Remove(this);
        }
    }
}
