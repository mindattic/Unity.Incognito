using Incognito.Player;
using UnityEngine;

namespace Incognito.World
{
    /// <summary>
    /// obj_exit_side: an invisible 10px strip just past a room's edge. Walking into it sets the
    /// next room and entrance, puts the player on autowalk (they keep walking off-screen) and
    /// fades to black; <see cref="RoomFade"/> then loads the room.
    /// </summary>
    public sealed class SideExit : MonoBehaviour
    {
        [Tooltip("The strip's left edge, original px (the instance x).")]
        [SerializeField] private float stripX;
        [SerializeField] private string nextRoom = string.Empty;
        [SerializeField] private RoomEntrance entrance = RoomEntrance.Right;
        [Tooltip("enter_x, for RoomEntrance.At.")]
        [SerializeField] private float enterX;
        [SerializeField] private Room room;
        [SerializeField] private RoomFade fade;

        private bool taken;

        public void Configure(float stripX, string nextRoom, RoomEntrance entrance, float enterX, Room room, RoomFade fade)
        {
            this.stripX = stripX;
            this.nextRoom = nextRoom;
            this.entrance = entrance;
            this.enterX = enterX;
            this.room = room;
            this.fade = fade;
        }

        private void Update()
        {
            if (taken || room == null || room.Player == null || fade == null) return;
            PlayerLocomotion player = room.Player;
            if (!RoomExitRules.Overlaps(player.State.PositionX, room.PlayerWidth, stripX)) return;

            taken = true;
            RoomTransition.Request(nextRoom, entrance, enterX, player.State.Facing);
            player.AutoWalk = true;
            fade.FadeIn();
        }
    }
}
