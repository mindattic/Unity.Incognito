using System;
using Incognito.Settings;
using Unity.Shared.Core.Json;

namespace Incognito.Content
{
    /// <summary>
    /// One of the original's three fonts (RECONSTRUCTION.md §10, U.INC-T31), as data: its
    /// <c>fonts/*.font.gmx</c> name, the face and point size it was made from, and where the original
    /// draws with it. Every text element the rebuild draws takes its size from this catalog
    /// (<see cref="GameContent.Fonts"/>) by <see cref="Id"/>, not from a literal of its own.
    ///
    /// GameMaker pre-rendered each font into a glyph atlas (<c>fonts/*.png</c>) on the author's
    /// machine, which ran at 120 DPI (125% scaling): fnt_basic's Arial 12pt cells are 23px tall
    /// (Arial's 1.117em line at 20px) and fnt_system's Lucida Console 8pt advances 8px a glyph (its
    /// 0.6em advance at 13.3px). So a point here is <see cref="AtlasDpi"/>/72 of a 1080p reference
    /// pixel, and <see cref="PixelSize"/> is the size the original's text actually appeared at.
    /// The rebuild draws every font as Arial (Unity's built-in LegacyRuntime.ttf, the only face it
    /// can ship): High Tower Text and Lucida Console are Microsoft-licensed system fonts it cannot
    /// redistribute. The size, colour, outline and placement are the original's.
    /// </summary>
    [Serializable]
    public sealed class GameFontDefinition
    {
        /// <summary>The DPI the original's glyph atlases were rendered at (see the class summary).</summary>
        public const float AtlasDpi = 120f;

        /// <summary>The original's resource name, e.g. <c>fnt_large</c>.</summary>
        public string Id = string.Empty;

        /// <summary>The .font.gmx <c>&lt;name&gt;</c>: the face the original used, e.g. <c>High Tower Text</c>.</summary>
        public string Face = string.Empty;

        /// <summary>The .font.gmx <c>&lt;size&gt;</c>, in points.</summary>
        public float PointSize;

        public bool Bold;
        public bool Italic;

        /// <summary>Where the original draws with it (objects and their draw events), for audit.</summary>
        public string UsedBy = string.Empty;

        /// <summary>The text's size on the original's 1920x1080 view, in reference pixels (UI
        /// Toolkit's font-size on the HUD panel): <see cref="PointSize"/> at <see cref="AtlasDpi"/>.</summary>
        public float PixelSize => PointSize * AtlasDpi / 72f;

        public void Validate()
        {
            if (string.IsNullOrEmpty(Id))
                throw new SettingsValidationException("A font needs an Id.");
            if (string.IsNullOrEmpty(Face))
                throw new SettingsValidationException("Font '" + Id + "' needs a Face.");
            if (!(PointSize > 0f))
                throw new SettingsValidationException("Font '" + Id + "' needs a positive PointSize.");
            if (string.IsNullOrEmpty(UsedBy))
                throw new SettingsValidationException("Font '" + Id + "' needs UsedBy (where the original draws with it).");
        }

        public static GameFontDefinition FromJson(JsonObject json)
        {
            var font = new GameFontDefinition();
            if (json == null) return font;
            font.Id = json.GetString(nameof(Id), font.Id);
            font.Face = json.GetString(nameof(Face), font.Face);
            font.PointSize = json.GetFloat(nameof(PointSize), font.PointSize);
            font.Bold = json.GetBool(nameof(Bold), font.Bold);
            font.Italic = json.GetBool(nameof(Italic), font.Italic);
            font.UsedBy = json.GetString(nameof(UsedBy), font.UsedBy);
            return font;
        }
    }

    /// <summary>The ids of the original's three fonts (<see cref="GameContent.Fonts"/>).</summary>
    public static class GameFonts
    {
        /// <summary>fnt_basic: Arial 12pt (obj_float_text, obj_debug, obj_game, obj_startup).</summary>
        public const string Basic = "fnt_basic";

        /// <summary>fnt_large: High Tower Text 16pt (obj_room's name, obj_exit, obj_typewriter, obj_npc's suspicion).</summary>
        public const string Large = "fnt_large";

        /// <summary>fnt_system: Lucida Console 8pt (obj_console, obj_program, obj_icon_pistol).</summary>
        public const string System = "fnt_system";

        public static readonly string[] All = { Basic, Large, System };
    }
}
