using System;

namespace Incognito.World
{
    /// <summary>
    /// Which player a command comes from or an event is about. There is no "current player" and no
    /// global player object: every command carries its sender's id, and each player's presentation
    /// holds its own id (a networked player's seat carries the same number to the authority).
    /// </summary>
    public readonly struct PlayerId : IEquatable<PlayerId>
    {
        public readonly int Value;

        public PlayerId(int value)
        {
            Value = value;
        }

        public bool Equals(PlayerId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is PlayerId other && Equals(other);
        public override int GetHashCode() => Value;
        public static bool operator ==(PlayerId a, PlayerId b) => a.Value == b.Value;
        public static bool operator !=(PlayerId a, PlayerId b) => a.Value != b.Value;
        public override string ToString() => "P" + Value;
    }

    /// <summary>An NPC in the <see cref="GameWorld"/>. Ids are never reused within a world.</summary>
    public readonly struct NpcId : IEquatable<NpcId>
    {
        public readonly int Value;

        public NpcId(int value)
        {
            Value = value;
        }

        public bool Equals(NpcId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is NpcId other && Equals(other);
        public override int GetHashCode() => Value;
        public static bool operator ==(NpcId a, NpcId b) => a.Value == b.Value;
        public static bool operator !=(NpcId a, NpcId b) => a.Value != b.Value;
        public override string ToString() => "N" + Value;
    }
}
