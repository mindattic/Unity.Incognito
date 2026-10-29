using System;
using Incognito.Settings;
using Unity.Shared.Core.Json;

namespace Incognito.Content
{
    /// <summary>
    /// One of the original's sprites or backgrounds (RECONSTRUCTION.md §10, U.INC-T29), as data:
    /// its <c>sprites/*.sprite.gmx</c> or <c>background/*.background.gmx</c> name, the image it was
    /// copied from verbatim (under Assets/Rea.Incognito/Art/Original), its size and GameMaker origin
    /// (<c>xorig</c>/<c>yorigin</c>, from the top-left; backgrounds have none, so 0,0), how it imports,
    /// and where the original uses it. Every original image the editor builders import goes through
    /// this catalog (<see cref="GameContent.Art"/>) by <see cref="Id"/>, so no builder carries an
    /// origin or a file literal of its own, and the pivot Unity imports is the original's origin.
    /// </summary>
    [Serializable]
    public sealed class ArtDefinition
    {
        /// <summary>The original's resource name, e.g. <c>spr_icon_pistol</c> or <c>bg_black</c>.</summary>
        public string Id = string.Empty;

        /// <summary>Path under Assets/Rea.Incognito/Art/Original, forward slashes, e.g.
        /// <c>spr_icon_pistol.png</c> or <c>Backgrounds/bg_black.png</c>.</summary>
        public string File = string.Empty;

        /// <summary>The image's size in original pixels (the .sprite.gmx <c>width</c>/<c>height</c>).</summary>
        public int Width;
        public int Height;

        /// <summary>GameMaker's origin, from the image's top-left (<c>xorig</c>/<c>yorigin</c>).</summary>
        public int OriginX;
        public int OriginY;

        /// <summary>The painted world art (room backgrounds, street props) imports compressed as a
        /// full-rect quad (an uncompressed 1920x1080 slice is 8MB); the pixel sprites and HUD art
        /// import uncompressed.</summary>
        public bool Compressed;

        /// <summary>Wraps (Repeat) instead of clamping: the title's htiled, scrolling bg_city_black.</summary>
        public bool Tiled;

        /// <summary>Where the original uses it (object/script/room and event), for audit.</summary>
        public string UsedBy = string.Empty;

        /// <summary>False for art the original only uses from objects that are never created
        /// (obj_command_prompt, obj_stealth, obj_fade_stealth): imported and catalogued, since the
        /// rebuild ports or may port those objects' roles, but no live original object draws it.</summary>
        public bool Live = true;

        public void Validate()
        {
            if (string.IsNullOrEmpty(Id))
                throw new SettingsValidationException("An art entry needs an Id.");
            if (string.IsNullOrEmpty(File))
                throw new SettingsValidationException("Art '" + Id + "' needs a File.");
            if (File.IndexOf('\\') >= 0 || File.StartsWith("/") || File.Contains(".."))
                throw new SettingsValidationException("Art '" + Id + "'s File '" + File + "' must be a forward-slash path inside Art/Original.");
            if (!File.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                throw new SettingsValidationException("Art '" + Id + "'s File '" + File + "' must be a .png.");
            if (Width <= 0 || Height <= 0)
                throw new SettingsValidationException("Art '" + Id + "' needs a positive Width and Height.");
            if (OriginX < 0 || OriginX > Width || OriginY < 0 || OriginY > Height)
                throw new SettingsValidationException(
                    "Art '" + Id + "'s origin " + OriginX + "," + OriginY + " is outside its " + Width + "x" + Height + " image.");
            if (string.IsNullOrEmpty(UsedBy))
                throw new SettingsValidationException("Art '" + Id + "' needs UsedBy (where the original uses it).");
        }

        public static ArtDefinition FromJson(JsonObject json)
        {
            var art = new ArtDefinition();
            if (json == null) return art;
            art.Id = json.GetString(nameof(Id), art.Id);
            art.File = json.GetString(nameof(File), art.File);
            art.Width = json.GetInt(nameof(Width), art.Width);
            art.Height = json.GetInt(nameof(Height), art.Height);
            art.OriginX = json.GetInt(nameof(OriginX), art.OriginX);
            art.OriginY = json.GetInt(nameof(OriginY), art.OriginY);
            art.Compressed = json.GetBool(nameof(Compressed), art.Compressed);
            art.Tiled = json.GetBool(nameof(Tiled), art.Tiled);
            art.UsedBy = json.GetString(nameof(UsedBy), art.UsedBy);
            art.Live = json.GetBool(nameof(Live), art.Live);
            return art;
        }
    }
}
