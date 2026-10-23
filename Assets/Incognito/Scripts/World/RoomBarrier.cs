using UnityEngine;

namespace Incognito.World
{
    /// <summary>
    /// obj_barrier (the shop's x = 800): a 10px wall the player cannot walk through. Runs after
    /// the player's Update so it corrects the same frame, before the camera frames it.
    /// </summary>
    [DefaultExecutionOrder(10)]
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

        private void Update()
        {
            if (room == null || room.Player == null) return;
            var player = room.Player;
            float x = player.State.PositionX;
            float blocked = RoomExitRules.Block(x, room.PlayerWidth, stripX);
            if (!Mathf.Approximately(blocked, x)) player.Teleport(blocked);
        }
    }
}
