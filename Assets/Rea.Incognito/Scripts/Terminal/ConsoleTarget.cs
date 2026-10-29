using System;
using Incognito.Player;
using Incognito.World;
using UnityEngine;

namespace Incognito.Terminal
{
    /// <summary>
    /// Marks a scene object as visible to the typed console (RECONSTRUCTION.md §6). The id is
    /// authored data, like the original NPC's <c>sheep_id</c> (e.g. "0097"); NPCs (U.INC-T8)
    /// carry one of these with <see cref="ConsoleObjectType.Sheep"/>. It lists the object in the
    /// world where it stands and plays the world's <see cref="ConsoleObjectRebooted"/> /
    /// <see cref="ConsoleObjectOverwritten"/> for it.
    /// </summary>
    public sealed class ConsoleTarget : MonoBehaviour
    {
        [Tooltip("The scene's authority. Found in the scene when unassigned.")]
        [SerializeField] private GameSession session;
        [SerializeField] private ConsoleObjectType type = ConsoleObjectType.Sheep;
        [SerializeField] private string id = "0000";

        /// <summary>Raised when the console's <c>reboot</c> resolves this target.</summary>
        public event Action Rebooted;

        /// <summary>Raised when the console's <c>overwrite</c> resolves this target.</summary>
        public event Action Overwritten;

        public ConsoleObject Object => new ConsoleObject(type, id);

        private bool started;
        private bool listed;
        private ConsoleObject listedAs;
        private Vec2 listedAt;

        public void Configure(ConsoleObjectType newType, string newId)
        {
            type = newType;
            id = newId;
        }

        // Start, not OnEnable: the scene's session is found once the scene has loaded.
        private void Start()
        {
            started = true;
            session = GameSession.Find(session);
            if (session != null) List();
        }

        private void OnEnable()
        {
            if (started && session != null) List();
        }

        private void OnDisable()
        {
            if (session == null) return;
            session.Played -= OnPlayed;
            if (listed) session.World.UnregisterConsoleObject(listedAs);
            listed = false;
        }

        // The object stands where its transform is, in original px; kept current if it moves.
        private void LateUpdate()
        {
            if (!listed) return;
            if (!Where().Equals(listedAt) || !Object.Equals(listedAs)) List();
        }

        private void List()
        {
            if (listed && !Object.Equals(listedAs)) session.World.UnregisterConsoleObject(listedAs);
            listedAs = Object;
            listedAt = Where();
            session.World.RegisterConsoleObject(listedAs, listedAt);
            if (!listed) session.Played += OnPlayed;
            listed = true;
        }

        private Vec2 Where()
        {
            Vector3 p = transform.position;
            return new Vec2(p.x / PlayerLocomotion.WorldUnitsPerOriginalPixel, p.y / PlayerLocomotion.WorldUnitsPerOriginalPixel);
        }

        private void OnPlayed(GameEvent e)
        {
            switch (e)
            {
                case ConsoleObjectRebooted rebooted when rebooted.Target.Equals(Object):
                    Rebooted?.Invoke();
                    break;
                case ConsoleObjectOverwritten overwritten when overwritten.Target.Equals(Object):
                    Overwritten?.Invoke();
                    break;
            }
        }
    }
}
