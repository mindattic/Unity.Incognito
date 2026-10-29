using System;
using Unity.Shared.Core.Json;

namespace Incognito.Settings
{
    /// <summary>
    /// obj_startup / obj_titlescreen's display setup (U.INC-T32): the reference view every room is
    /// authored for (each room's view 0 is wview/hview = wport/hport = 1920x1080, and obj_startup
    /// sets <c>view_wport/view_hport</c> to the window's size) and whether the game goes fullscreen
    /// at start (both objects' Create runs <c>window_set_fullscreen(true)</c>, though the config's
    /// own <c>option_fullscreen</c> is False). The original's <c>option_scale = -1</c> ("keep aspect
    /// ratio") is what letterboxes that view on any other aspect; the maths is
    /// <see cref="Incognito.Display.ScreenRatio"/>.
    /// </summary>
    [Serializable]
    public sealed class DisplayTuning
    {
        /// <summary>The rooms' view width (wview = wport = 1920).</summary>
        public float ReferenceWidth = 1920f;

        /// <summary>The rooms' view height (hview = hport = 1080).</summary>
        public float ReferenceHeight = 1080f;

        /// <summary>obj_startup / obj_titlescreen: <c>window_set_fullscreen(true)</c> on Create.</summary>
        public bool StartFullscreen = true;

        public void Validate()
        {
            if (float.IsNaN(ReferenceWidth) || ReferenceWidth <= 0f)
                throw new SettingsValidationException("DisplayTuning.ReferenceWidth must be positive: " + ReferenceWidth);
            if (float.IsNaN(ReferenceHeight) || ReferenceHeight <= 0f)
                throw new SettingsValidationException("DisplayTuning.ReferenceHeight must be positive: " + ReferenceHeight);
        }

        public static DisplayTuning FromJson(JsonObject json)
        {
            var tuning = new DisplayTuning();
            if (json == null) return tuning;
            tuning.ReferenceWidth = json.GetFloat(nameof(ReferenceWidth), tuning.ReferenceWidth);
            tuning.ReferenceHeight = json.GetFloat(nameof(ReferenceHeight), tuning.ReferenceHeight);
            tuning.StartFullscreen = json.GetBool(nameof(StartFullscreen), tuning.StartFullscreen);
            return tuning;
        }
    }
}
