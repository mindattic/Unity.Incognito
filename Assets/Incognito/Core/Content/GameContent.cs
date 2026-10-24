using System;
using System.Collections.Generic;
using Incognito.Settings;
using Unity.Shared.Core.Json;

namespace Incognito.Content
{
    /// <summary>
    /// The whole game's authored content catalog (U.INC-T22): every room, every NPC and the
    /// console's text, in one validated place instead of literals spread across
    /// RoomSceneBuilder, SandboxSceneBuilder and CommandInterpreter. Loaded from
    /// <c>Resources/Data/game-content.json</c> by <c>Incognito.Settings.GameSettings</c> (Runtime,
    /// which only reads the file's bytes); parsed here with Unity.Shared.Core's engine-free JSON
    /// reader (U.INC-T25, replacing UnityEngine.JsonUtility), so this type stays engine-free and
    /// can be built, parsed and validated from a plain C# test.
    /// </summary>
    [Serializable]
    public sealed class GameContent
    {
        public RoomDefinition[] Rooms = Array.Empty<RoomDefinition>();
        public NpcDefinition[] Npcs = Array.Empty<NpcDefinition>();
        public ConsoleText Console = new ConsoleText();

        public RoomDefinition FindRoom(string id)
        {
            foreach (RoomDefinition room in Rooms)
                if (string.Equals(room.Id, id, StringComparison.OrdinalIgnoreCase))
                    return room;
            return null;
        }

        public NpcDefinition FindNpc(string id)
        {
            foreach (NpcDefinition npc in Npcs)
                if (string.Equals(npc.Id, id, StringComparison.OrdinalIgnoreCase))
                    return npc;
            return null;
        }

        /// <summary>
        /// Validates every room and NPC, then the cross-references between them (a side exit or
        /// door naming a room that exists, an NPC's RoomId existing and its SpawnX inside that
        /// room). Throws <see cref="SettingsValidationException"/> on the first problem found.
        /// </summary>
        public void Validate()
        {
            if (Rooms == null || Rooms.Length == 0)
                throw new SettingsValidationException("GameContent needs at least one room.");

            var roomIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (RoomDefinition room in Rooms)
            {
                room.Validate();
                if (!roomIds.Add(room.Id))
                    throw new SettingsValidationException("Two rooms share the id '" + room.Id + "'.");
            }

            foreach (RoomDefinition room in Rooms)
            {
                foreach (SideExitSpec exit in room.SideExits)
                {
                    if (!roomIds.Contains(exit.To))
                        throw new SettingsValidationException("Room '" + room.Id + "'s side exit targets unknown room '" + exit.To + "'.");
                    CheckEnterX(room.Id, "side exit at x=" + exit.X, exit.Enter, exit.EnterX, FindRoom(exit.To));
                }
                foreach (DoorSpec door in room.Doors)
                {
                    if (!roomIds.Contains(door.To))
                        throw new SettingsValidationException("Room '" + room.Id + "'s door '" + door.Label + "' targets unknown room '" + door.To + "'.");
                    CheckEnterX(room.Id, "door '" + door.Label + "'", door.Enter, door.EnterX, FindRoom(door.To));
                }
            }

            if (Npcs == null)
                throw new SettingsValidationException("GameContent.Npcs must not be null.");

            var npcIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (NpcDefinition npc in Npcs)
            {
                npc.Validate();
                if (!npcIds.Add(npc.Id))
                    throw new SettingsValidationException("Two NPCs share the id '" + npc.Id + "'.");

                RoomDefinition room = FindRoom(npc.RoomId);
                if (room == null)
                    throw new SettingsValidationException("NPC '" + npc.Id + "' names unknown room '" + npc.RoomId + "'.");
                if (npc.SpawnX < 0f || npc.SpawnX > room.Width)
                    throw new SettingsValidationException(
                        "NPC '" + npc.Id + "'s SpawnX " + npc.SpawnX + " is outside room '" + room.Id + "' (width " + room.Width + ").");
            }

            if (Console == null)
                throw new SettingsValidationException("GameContent.Console is required.");
            Console.Validate();
        }

        // An "At" entrance lands the player at a specific x in the destination room; a bad EnterX
        // would otherwise silently place them off the room's strip (U.INC-T22 review).
        private static void CheckEnterX(string fromRoomId, string what, RoomEntranceKind enter, float enterX, RoomDefinition destination)
        {
            if (enter != RoomEntranceKind.At || destination == null) return;
            if (enterX < 0f || enterX > destination.Width)
                throw new SettingsValidationException(
                    "Room '" + fromRoomId + "'s " + what + " enters '" + destination.Id + "' at x=" + enterX +
                    ", outside that room (width " + destination.Width + ").");
        }

        /// <summary>
        /// Parses <paramref name="json"/> (<c>game-content.json</c>'s text) with Unity.Shared.Core's
        /// <see cref="MiniJson"/>/<see cref="JsonObject"/> reader (U.INC-T25). Throws
        /// <see cref="JsonFormatException"/> on text that is not a JSON object; does not validate
        /// (callers validate separately, as <c>GameSettings.Load</c> does).
        /// </summary>
        public static GameContent FromJson(string json)
        {
            JsonObject root = JsonObject.Parse(json);
            return new GameContent
            {
                Rooms = root.GetObjectArray(nameof(Rooms), RoomDefinition.FromJson),
                Npcs = root.GetObjectArray(nameof(Npcs), NpcDefinition.FromJson),
                Console = ConsoleText.FromJson(root.GetObject(nameof(Console))),
            };
        }
    }
}
