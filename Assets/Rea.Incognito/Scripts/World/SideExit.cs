using UnityEngine;

namespace Incognito.World
{
    /// <summary>
    /// obj_exit_side: an invisible 10px strip just past a room's edge. The world catches the player
    /// walking into it (<see cref="GameWorld.AddSideExit"/>: it puts them on autowalk, so they keep
    /// walking off-screen, and produces <see cref="SideExitReached"/>); this view then sets the next
    /// room and entrance and fades to black, and <see cref="RoomFade"/> loads the room.
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

        private GameSession session;
        private int exitId;

        public void Configure(float stripX, string nextRoom, RoomEntrance entrance, float enterX, Room room, RoomFade fade)
        {
            this.stripX = stripX;
            this.nextRoom = nextRoom;
            this.entrance = entrance;
            this.enterX = enterX;
            this.room = room;
            this.fade = fade;
        }

        // Start: the player's seat (and so the session) is set in its Awake.
        private void Start()
        {
            if (room == null || room.Player == null || room.Player.Session == null) return;
            session = room.Player.Session;
            exitId = session.World.AddSideExit(stripX, room.PlayerWidth);
            session.Played += OnPlayed;
        }

        private void OnDestroy()
        {
            if (session != null) session.Played -= OnPlayed;
        }

        private void OnPlayed(GameEvent e)
        {
            if (!(e is SideExitReached reached) || reached.Exit != exitId || fade == null) return;
            if (reached.Player != room.Player.Id) return;

            RoomTransition.Request(nextRoom, entrance, enterX, room.Player.State.Facing);
            fade.FadeIn();
        }
    }
}
