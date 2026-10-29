using System;
using Incognito.Settings;
using Unity.Shared.Core.Json;

namespace Incognito.Content
{
    /// <summary>
    /// One of the original's sounds (RECONSTRUCTION.md §8, U.INC-T28), as data: its
    /// <c>sound/*.sound.gmx</c> name, the file it was imported from (copied verbatim into
    /// Assets/Rea.Incognito/Audio/Original), whether the original streamed it (the .sound.gmx
    /// <c>&lt;streamed&gt;</c> flag: music and the whispers ambient yes, effects no), and where the
    /// original plays or references it. Every sound the rebuild loads goes through this catalog
    /// (<see cref="GameContent.Sounds"/>) by <see cref="Id"/>, so the editor builder resolves no
    /// sound file by a literal of its own.
    /// </summary>
    [Serializable]
    public sealed class SoundDefinition
    {
        /// <summary>The original's resource name, e.g. <c>sfx_flick</c>.</summary>
        public string Id = string.Empty;

        /// <summary>File name under Assets/Rea.Incognito/Audio/Original, e.g. <c>sfx_flick.wav</c>.</summary>
        public string File = string.Empty;

        /// <summary>The .sound.gmx <c>&lt;streamed&gt;</c> flag: streamed clips import as Streaming,
        /// the rest decompress on load.</summary>
        public bool Streamed;

        /// <summary>Where the original plays or references it (object/script and event), for audit.</summary>
        public string PlayedBy = string.Empty;

        /// <summary>False for a sound the original only names in commented-out code (whispers, in
        /// titlescreen's <c>//current_track = whispers;</c>): imported and catalogued, but nothing
        /// plays it, exactly as in the original.</summary>
        public bool Wired = true;

        public void Validate()
        {
            if (string.IsNullOrEmpty(Id))
                throw new SettingsValidationException("A sound needs an Id.");
            if (string.IsNullOrEmpty(File))
                throw new SettingsValidationException("Sound '" + Id + "' needs a File.");
            if (File.IndexOfAny(new[] { '/', '\\' }) >= 0)
                throw new SettingsValidationException("Sound '" + Id + "'s File '" + File + "' must be a bare file name in Audio/Original.");
            if (string.IsNullOrEmpty(PlayedBy))
                throw new SettingsValidationException("Sound '" + Id + "' needs PlayedBy (where the original plays it).");
        }

        public static SoundDefinition FromJson(JsonObject json)
        {
            var sound = new SoundDefinition();
            if (json == null) return sound;
            sound.Id = json.GetString(nameof(Id), sound.Id);
            sound.File = json.GetString(nameof(File), sound.File);
            sound.Streamed = json.GetBool(nameof(Streamed), sound.Streamed);
            sound.PlayedBy = json.GetString(nameof(PlayedBy), sound.PlayedBy);
            sound.Wired = json.GetBool(nameof(Wired), sound.Wired);
            return sound;
        }
    }

    /// <summary>
    /// The title screen's audio (titlescreen.room.gmx's obj_music instance), as data (U.INC-T28).
    /// The title isn't a <see cref="RoomDefinition"/> (no player, no street), so it gets its own
    /// entry. Both names refer to <see cref="GameContent.Sounds"/>.
    /// </summary>
    [Serializable]
    public sealed class TitleScreenSpec
    {
        /// <summary>The track the title plays: obj_music's global default (music_oppressivegloom),
        /// since the instance's own assignment is commented out.</summary>
        public string Music = string.Empty;

        /// <summary>The track titlescreen's obj_music creation code names in a commented-out line
        /// (<c>//current_track = whispers;</c>). Kept as data, never played, as in the original.</summary>
        public string UnwiredTrack = string.Empty;

        public static TitleScreenSpec FromJson(JsonObject json)
        {
            var title = new TitleScreenSpec();
            if (json == null) return title;
            title.Music = json.GetString(nameof(Music), title.Music);
            title.UnwiredTrack = json.GetString(nameof(UnwiredTrack), title.UnwiredTrack);
            return title;
        }
    }
}
