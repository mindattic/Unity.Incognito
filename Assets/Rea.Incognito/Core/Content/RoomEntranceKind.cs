namespace Incognito.Content
{
    /// <summary>
    /// Where the player appears in the next room (the original's enter_from), as data. Mirrors
    /// <c>Incognito.World.RoomTransition.RoomEntrance</c> (Runtime); kept as a separate, engine-free
    /// enum here so room data (<see cref="RoomDefinition"/>) can be described and validated by Core
    /// alone, with the Runtime scene builder converting one to the other.
    /// </summary>
    public enum RoomEntranceKind
    {
        /// <summary>"left": one player width in from the room's left edge.</summary>
        Left,

        /// <summary>"right": one player width in from the room's right edge.</summary>
        Right,

        /// <summary>"x": at a given x (enter_x), e.g. back out of the shop at its door.</summary>
        At,
    }
}
