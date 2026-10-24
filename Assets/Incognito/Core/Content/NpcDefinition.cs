using System;
using Incognito.Settings;

namespace Incognito.Content
{
    /// <summary>
    /// One NPC's placement and content (RECONSTRUCTION.md §4): where it spawns, the route it
    /// patrols, its look-mode description and its console <c>sheep_id</c>. Kept as data
    /// (AGENTS.md law 2/4) instead of literals in SandboxSceneBuilder, so a later room or NPC is
    /// authored, not coded; loaded and validated from <c>Resources/Data/game-content.json</c>.
    /// </summary>
    [Serializable]
    public sealed class NpcDefinition
    {
        public string Id = string.Empty;

        /// <summary>The console's <c>sheep_id</c>, e.g. "0097" (printed as <c>SHP#0097</c>).</summary>
        public string SheepId = string.Empty;

        /// <summary>The <see cref="RoomDefinition.Id"/> this NPC spawns in.</summary>
        public string RoomId = string.Empty;

        /// <summary>Typed at a look-mode click (obj_npc's description; '#' became a newline).</summary>
        public string Description = string.Empty;

        /// <summary>Original px, within its room.</summary>
        public float SpawnX;

        /// <summary>X positions walked in order, then looped. One waypoint = walk there and stop.</summary>
        public float[] Waypoints = Array.Empty<float>();

        public float WalkSpeed = 5f;
        public float ArriveRange = 20f;

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Id))
                throw new SettingsValidationException("An NPC definition needs an Id.");
            string label = "NPC '" + Id + "'";

            if (string.IsNullOrWhiteSpace(SheepId))
                throw new SettingsValidationException(label + " needs a SheepId (the console's sheep_id).");
            if (string.IsNullOrWhiteSpace(RoomId))
                throw new SettingsValidationException(label + " needs a RoomId.");
            if (string.IsNullOrWhiteSpace(Description))
                throw new SettingsValidationException(label + " needs a Description.");
            if (Waypoints == null || Waypoints.Length == 0)
                throw new SettingsValidationException(label + " needs at least one waypoint.");
            if (float.IsNaN(WalkSpeed) || WalkSpeed <= 0f)
                throw new SettingsValidationException(label + "'s WalkSpeed must be positive: " + WalkSpeed);
            if (float.IsNaN(ArriveRange) || ArriveRange <= 0f)
                throw new SettingsValidationException(label + "'s ArriveRange must be positive: " + ArriveRange);
        }
    }
}
