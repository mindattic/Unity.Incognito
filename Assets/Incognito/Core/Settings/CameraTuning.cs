using System;
using Unity.Shared.Core.Json;

namespace Incognito.Settings
{
    /// <summary>
    /// obj_camera's close-framing tuning (RECONSTRUCTION.md §2): the view size, the player's
    /// cached sprite width, the follow deadzone/speed and the rebuild's vertical pan, plus the
    /// console/hacking overlay's cursor-follow numbers (obj_walker's view_hborder/view_hspeed when
    /// the view hands off to the cursor, §5). Room bounds are per-room scene data, not tuning, so
    /// they live on <see cref="Incognito.CameraRig.CloseFramingCameraState"/> itself. Kept as data
    /// (AGENTS.md law 2/4) instead of constants on
    /// <see cref="Incognito.CameraRig.CloseFramingCameraState"/> and duplicated serialized fields
    /// on the MonoBehaviour; defaults are the original's values.
    /// </summary>
    [Serializable]
    public sealed class CameraTuning
    {
        /// <summary>Original: wview = 1920.</summary>
        public float ViewWidth = 1920f;

        /// <summary>Original: player_width = sprite_get_width(spr_player) = 512.</summary>
        public float PlayerWidth = 512f;

        /// <summary>Half-width of the follow deadzone around view centre (wview/2 - hborder = 960 - 910).</summary>
        public float HorizontalDeadzone = 50f;

        /// <summary>Max view travel per step (hspeed = 20). Zero or negative means instant.</summary>
        public float HorizontalPanSpeed = 20f;

        /// <summary>How far the view may pan up/down from rest when the mouse is at the screen top/bottom.</summary>
        public float MaxVerticalPan = 135f;

        /// <summary>Console toggle: view_hborder[0] = 32 once the view follows the cursor.</summary>
        public float CursorFollowBorder = 32f;

        /// <summary>Console toggle: view_hspeed[0] = 10 while the view follows the cursor or a hacked NPC.</summary>
        public float CursorFollowPanSpeed = 10f;

        public void Validate()
        {
            if (float.IsNaN(ViewWidth) || ViewWidth <= 0f)
                throw new SettingsValidationException("CameraTuning.ViewWidth must be positive: " + ViewWidth);
            if (float.IsNaN(PlayerWidth) || PlayerWidth < 0f)
                throw new SettingsValidationException("CameraTuning.PlayerWidth must not be negative: " + PlayerWidth);
            if (float.IsNaN(HorizontalDeadzone) || HorizontalDeadzone < 0f)
                throw new SettingsValidationException("CameraTuning.HorizontalDeadzone must not be negative: " + HorizontalDeadzone);
            if (float.IsNaN(HorizontalPanSpeed))
                throw new SettingsValidationException("CameraTuning.HorizontalPanSpeed must be a number.");
            if (float.IsNaN(MaxVerticalPan) || MaxVerticalPan < 0f)
                throw new SettingsValidationException("CameraTuning.MaxVerticalPan must not be negative: " + MaxVerticalPan);
            if (float.IsNaN(CursorFollowBorder) || CursorFollowBorder < 0f)
                throw new SettingsValidationException("CameraTuning.CursorFollowBorder must not be negative: " + CursorFollowBorder);
            if (float.IsNaN(CursorFollowPanSpeed))
                throw new SettingsValidationException("CameraTuning.CursorFollowPanSpeed must be a number.");
        }

        /// <summary>U.INC-T25: parsed with Unity.Shared.Core's engine-free <see cref="JsonObject"/> reader.</summary>
        public static CameraTuning FromJson(JsonObject json)
        {
            var tuning = new CameraTuning();
            if (json == null) return tuning;
            tuning.ViewWidth = json.GetFloat(nameof(ViewWidth), tuning.ViewWidth);
            tuning.PlayerWidth = json.GetFloat(nameof(PlayerWidth), tuning.PlayerWidth);
            tuning.HorizontalDeadzone = json.GetFloat(nameof(HorizontalDeadzone), tuning.HorizontalDeadzone);
            tuning.HorizontalPanSpeed = json.GetFloat(nameof(HorizontalPanSpeed), tuning.HorizontalPanSpeed);
            tuning.MaxVerticalPan = json.GetFloat(nameof(MaxVerticalPan), tuning.MaxVerticalPan);
            tuning.CursorFollowBorder = json.GetFloat(nameof(CursorFollowBorder), tuning.CursorFollowBorder);
            tuning.CursorFollowPanSpeed = json.GetFloat(nameof(CursorFollowPanSpeed), tuning.CursorFollowPanSpeed);
            return tuning;
        }
    }
}
