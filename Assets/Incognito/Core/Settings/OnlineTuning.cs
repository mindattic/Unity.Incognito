using System;
using Incognito.Content;
using Unity.Shared.Core.Json;

namespace Incognito.Settings
{
    /// <summary>
    /// Online play's numbers (U.INC-T43, rebuild standard point 9). The original was single-player and
    /// had no online play, so these are the rebuild's own: which room an online session plays in, how
    /// many seats it has, and how far apart the seats start. Seat 0 starts where local play does (the
    /// room's <see cref="RoomDefinition.PlayerStartX"/>); every later seat starts
    /// <see cref="SeatSpacing"/> further into the room, so players never spawn inside each other.
    /// </summary>
    [Serializable]
    public sealed class OnlineTuning
    {
        /// <summary>The <see cref="RoomDefinition.Id"/> an online session plays in.</summary>
        public string Room = "Street00";

        /// <summary>Most players one online session seats (the host included).</summary>
        public int MaxPlayers = 4;

        /// <summary>Original px between consecutive seats' starting positions.</summary>
        public float SeatSpacing = 600f;

        /// <summary>
        /// Where <paramref name="seat"/> starts in <paramref name="room"/>, original px: the room's own
        /// start for seat 0, then <see cref="SeatSpacing"/> per seat toward the room's middle, kept
        /// inside the room.
        /// </summary>
        public float StartFor(RoomDefinition room, int seat)
        {
            if (room == null) throw new ArgumentNullException(nameof(room));
            float inward = room.PlayerStartX > room.Width * 0.5f ? -1f : 1f;
            float x = room.PlayerStartX + inward * Math.Max(0, seat) * SeatSpacing;
            return Math.Max(0f, Math.Min(room.Width, x));
        }

        public void Validate()
        {
            if (string.IsNullOrEmpty(Room))
                throw new SettingsValidationException("OnlineTuning.Room must name a room.");
            if (MaxPlayers < 1 || MaxPlayers > 16)
                throw new SettingsValidationException("OnlineTuning.MaxPlayers must be 1 to 16: " + MaxPlayers);
            if (float.IsNaN(SeatSpacing) || float.IsInfinity(SeatSpacing) || SeatSpacing < 0f)
                throw new SettingsValidationException("OnlineTuning.SeatSpacing must be zero or more: " + SeatSpacing);
        }

        public static OnlineTuning FromJson(JsonObject json)
        {
            var tuning = new OnlineTuning();
            if (json == null) return tuning;
            tuning.Room = json.GetString(nameof(Room), tuning.Room);
            tuning.MaxPlayers = json.GetInt(nameof(MaxPlayers), tuning.MaxPlayers);
            tuning.SeatSpacing = json.GetFloat(nameof(SeatSpacing), tuning.SeatSpacing);
            return tuning;
        }
    }
}
