using System;

namespace Incognito.Terminal
{
    /// <summary>
    /// The kinds of object the typed console can query. The lower-cased enum name is the word
    /// typed after <c>list</c>; the original's <c>list</c> usage text advertised exactly these
    /// three (<c>[sheep, camera, computer]</c>, scripts/command_list.gml), though only
    /// <c>sheep</c> had a case. "Sheep" is the in-fiction slang for a targetable civilian.
    /// </summary>
    public enum ConsoleObjectType
    {
        Sheep,
        Camera,
        Computer,
    }

    /// <summary>One queryable object as the console sees it: its type and its id string.</summary>
    public readonly struct ConsoleObject : IEquatable<ConsoleObject>
    {
        public ConsoleObject(ConsoleObjectType type, string id)
        {
            Type = type;
            Id = id ?? string.Empty;
        }

        public ConsoleObjectType Type { get; }

        /// <summary>The bare id, e.g. the original NPC's <c>sheep_id = "0097"</c>.</summary>
        public string Id { get; }

        /// <summary>The id as the console prints it, e.g. <c>SHP#0097</c>.</summary>
        public string Handle => CommandInterpreter.PrefixFor(Type) + "#" + Id;

        public bool Equals(ConsoleObject other) => Type == other.Type && Id == other.Id;
        public override bool Equals(object obj) => obj is ConsoleObject other && Equals(other);
        public override int GetHashCode() => ((int)Type * 397) ^ Id.GetHashCode();
        public override string ToString() => Handle;
    }
}
