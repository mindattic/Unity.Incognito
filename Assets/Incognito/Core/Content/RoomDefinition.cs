using System;
using Incognito.Settings;

namespace Incognito.Content
{
    /// <summary>One clickable prop's position and look-mode description (a lamppost or mailbox).</summary>
    [Serializable]
    public sealed class PropSpot
    {
        public float X;
        public string Description = string.Empty;

        public void Validate(string label)
        {
            if (string.IsNullOrWhiteSpace(Description))
                throw new SettingsValidationException(label + " needs a Description.");
        }
    }

    /// <summary>obj_exit_side: a strip the player crosses into another room, autowalking and fading.</summary>
    [Serializable]
    public sealed class SideExitSpec
    {
        public float X;
        public string To = string.Empty;
        public RoomEntranceKind Enter;
        public float EnterX;

        public void Validate(string label)
        {
            if (string.IsNullOrWhiteSpace(To))
                throw new SettingsValidationException(label + " must name a destination room.");
            if (!Enum.IsDefined(typeof(RoomEntranceKind), Enter))
                throw new SettingsValidationException(label + " has an unrecognized Enter value: " + Enter);
        }
    }

    /// <summary>obj_exit: a floating door, used with W within reach, to another room.</summary>
    [Serializable]
    public sealed class DoorSpec
    {
        public float X;
        public float Y;
        public string Label = string.Empty;
        public string To = string.Empty;
        public RoomEntranceKind Enter;
        public float EnterX;

        public void Validate(string label)
        {
            if (string.IsNullOrWhiteSpace(Label))
                throw new SettingsValidationException(label + " needs a Label.");
            if (string.IsNullOrWhiteSpace(To))
                throw new SettingsValidationException(label + " must name a destination room.");
            if (!Enum.IsDefined(typeof(RoomEntranceKind), Enter))
                throw new SettingsValidationException(label + " has an unrecognized Enter value: " + Enter);
        }
    }

    /// <summary>
    /// One playable room, transcribed from the original's rooms/*.room.gmx
    /// (RECONSTRUCTION.md §7): its background tiles, props, exits, music and (street00's) opening
    /// poem. Kept as data (AGENTS.md law 2/4) instead of RoomSceneBuilder's hard-coded
    /// <c>RoomSpec[]</c> table; loaded and validated from <c>Resources/Data/game-content.json</c>.
    /// </summary>
    [Serializable]
    public sealed class RoomDefinition
    {
        /// <summary>Also the Unity scene name a transition or a door's <see cref="DoorSpec.To"/>/
        /// <see cref="SideExitSpec.To"/> loads by.</summary>
        public string Id = string.Empty;

        /// <summary>get_room_name.gml: drawn top-left in the room.</summary>
        public string DisplayName = string.Empty;

        public float Width;

        /// <summary>1920-wide background tiles, left to right from x = 0.</summary>
        public string[] BackgroundSlices = Array.Empty<string>();

        /// <summary>Where a directly-opened scene puts the player.</summary>
        public float PlayerStartX;

        public PropSpot[] Lampposts = Array.Empty<PropSpot>();
        public PropSpot[] Mailboxes = Array.Empty<PropSpot>();
        public SideExitSpec[] SideExits = Array.Empty<SideExitSpec>();
        public DoorSpec[] Doors = Array.Empty<DoorSpec>();
        public float[] Barriers = Array.Empty<float>();

        public bool Rain;

        /// <summary>Whether the sandbox's NPC is kept in this room's built scene.</summary>
        public bool KeepNpc;

        /// <summary>obj_music current_track (file name, without extension).</summary>
        public string Music = string.Empty;

        /// <summary>An obj_typewriter placed on entry (street00's intro poem). Empty for none.</summary>
        public string OpeningText = string.Empty;
        public float OpeningTextX;
        public float OpeningTextY;

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Id))
                throw new SettingsValidationException("A room definition needs an Id.");
            string label = "Room '" + Id + "'";

            if (string.IsNullOrWhiteSpace(DisplayName))
                throw new SettingsValidationException(label + " needs a DisplayName.");
            if (float.IsNaN(Width) || Width <= 0f)
                throw new SettingsValidationException(label + "'s Width must be positive: " + Width);
            if (BackgroundSlices == null || BackgroundSlices.Length == 0)
                throw new SettingsValidationException(label + " needs at least one background slice.");
            foreach (string slice in BackgroundSlices)
                if (string.IsNullOrWhiteSpace(slice))
                    throw new SettingsValidationException(label + " has a blank background slice name.");
            if (PlayerStartX < 0f || PlayerStartX > Width)
                throw new SettingsValidationException(label + "'s PlayerStartX " + PlayerStartX + " is outside the room (width " + Width + ").");

            for (int i = 0; i < Lampposts.Length; i++)
            {
                CheckNearRoom(label, "Lamppost " + i, Lampposts[i].X, Width);
                Lampposts[i].Validate(label + "'s lamppost " + i);
            }
            for (int i = 0; i < Mailboxes.Length; i++)
            {
                CheckNearRoom(label, "Mailbox " + i, Mailboxes[i].X, Width);
                Mailboxes[i].Validate(label + "'s mailbox " + i);
            }
            foreach (SideExitSpec exit in SideExits) exit.Validate(label + "'s side exit at x=" + exit.X);
            foreach (DoorSpec door in Doors) door.Validate(label + "'s door '" + door.Label + "'");
            foreach (float barrier in Barriers) CheckNearRoom(label, "Barrier", barrier, Width);
        }

        // Lampposts/mailboxes/barriers sit inside the room; side exits legitimately sit a little
        // outside its strip (e.g. x = -32 or width + 32), so only a wildly out-of-room value (far
        // past that small overhang) is rejected.
        private static void CheckNearRoom(string roomLabel, string what, float x, float width)
        {
            const float margin = 200f;
            if (x < -margin || x > width + margin)
                throw new SettingsValidationException(roomLabel + ": " + what + " at x=" + x + " is far outside the room (width " + width + ").");
        }
    }
}
