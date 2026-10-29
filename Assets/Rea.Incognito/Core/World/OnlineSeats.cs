using System;

namespace Incognito.World
{
    /// <summary>
    /// How an online seat maps onto the <see cref="PlayerId"/> its commands carry (U.INC-T43). Seats are
    /// numbered from 0 by the Unity.Multiplayer roster and stay the same across a reconnect; players are
    /// numbered from 1, and local play's one seat is <c>P1</c>, so seat 0 (a listen server's own player)
    /// plays as the same id local play does. The mapping is plain arithmetic so a client, the authority
    /// and a test always agree on it.
    /// </summary>
    public static class OnlineSeats
    {
        /// <summary>The player id seat <paramref name="seat"/> plays as (seat 0 is P1).</summary>
        public static PlayerId PlayerFor(int seat)
        {
            if (seat < 0) throw new ArgumentOutOfRangeException(nameof(seat), seat, "A seat index is 0 or more.");
            return new PlayerId(seat + 1);
        }

        /// <summary>The seat <paramref name="player"/> sits in (P1 is seat 0); -1 for an id no seat plays as.</summary>
        public static int SeatOf(PlayerId player) => player.Value >= 1 ? player.Value - 1 : -1;
    }
}
