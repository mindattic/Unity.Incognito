using UnityEngine;

namespace Incognito.World
{
    /// <summary>
    /// obj_barrier (the shop's x = 800): a 10px wall the player cannot walk through. The wall is a rule
    /// of the world (<see cref="GameWorld.AddBarrier"/>), applied right after the player walks each
    /// step; this component just places it there for its room.
    /// </summary>
    public sealed class RoomBarrier : MonoBehaviour
    {
        [Tooltip("The strip's left edge, original px.")]
        [SerializeField] private float stripX;
        [SerializeField] private Room room;

        public void Configure(float stripX, Room room)
        {
            this.stripX = stripX;
            this.room = room;
        }

        // Start: the player's seat (and so the session) is set in its Awake.
        private void Start()
        {
            if (room == null || room.Player == null || room.Player.Session == null) return;
            room.Player.Session.World.AddBarrier(stripX, room.PlayerWidth);
        }
    }
}
