using Incognito.Player;

namespace Incognito.World
{
    /// <summary>Where the player appears in the next room (the original's enter_from).</summary>
    public enum RoomEntrance
    {
        /// <summary>"left": one player width in from the room's left edge.</summary>
        Left,
        /// <summary>"right": one player width in from the room's right edge.</summary>
        Right,
        /// <summary>"x": at a given x (enter_x), e.g. back out of the shop at its door.</summary>
        At,
    }

    /// <summary>
    /// The original's room-change globals (next_room, enter_from, enter_x), carried across a scene
    /// load. An exit sets them and starts the fade; the next room's <see cref="Room"/> consumes
    /// them to place the player. A scene opened directly (no pending transition) keeps the
    /// player where the scene put it.
    /// </summary>
    public static class RoomTransition
    {
        public static bool Pending { get; private set; }
        public static string NextRoom { get; private set; }
        public static RoomEntrance Entrance { get; private set; }
        public static float EnterX { get; private set; }

        /// <summary>The player's facing when they left. The original kept one persistent player
        /// object, so it walked into the next room still facing the way it walked out.</summary>
        public static PlayerLocomotionState.FacingDirection Facing { get; private set; }

        public static void Request(string nextRoom, RoomEntrance entrance, float enterX,
            PlayerLocomotionState.FacingDirection facing)
        {
            Pending = true;
            NextRoom = nextRoom;
            Entrance = entrance;
            EnterX = enterX;
            Facing = facing;
        }

        /// <summary>Takes the pending transition (if any) so it applies once.</summary>
        public static bool TryConsume(out RoomEntrance entrance, out float enterX,
            out PlayerLocomotionState.FacingDirection facing)
        {
            entrance = Entrance;
            enterX = EnterX;
            facing = Facing;
            if (!Pending) return false;
            Pending = false;
            return true;
        }

        /// <summary>
        /// obj_room create: "right" puts the player at room_width - player_width, "left" at
        /// player_width, "x" at enter_x. Original pixels.
        /// </summary>
        public static float EntryX(RoomEntrance entrance, float roomWidth, float playerWidth, float enterX)
        {
            switch (entrance)
            {
                case RoomEntrance.Right: return roomWidth - playerWidth;
                case RoomEntrance.Left: return playerWidth;
                default: return enterX;
            }
        }
    }
}
