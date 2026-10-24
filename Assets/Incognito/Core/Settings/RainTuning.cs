using System;
using Unity.Shared.Core.Json;

namespace Incognito.Settings
{
    /// <summary>
    /// obj_rain_emitter/obj_raindrop's tuning (RECONSTRUCTION.md §7): how many drops spawn per
    /// step, how tall one is, how it drifts and falls, the room height drops are culled against,
    /// and the original's 0.75 draw alpha. Kept as data (AGENTS.md law 2/4) instead of constants
    /// and inline magic numbers in <see cref="Incognito.World.RainState"/>; defaults are the
    /// original's values.
    /// </summary>
    [Serializable]
    public sealed class RainTuning
    {
        public float RoomHeight = 1080f;

        /// <summary>Original: 6 drops created per step.</summary>
        public int DropsPerStep = 6;

        /// <summary>spr_rain is 1x4.</summary>
        public int DropHeight = 4;

        /// <summary>Drift per step, original px (irandom_range(-6, -3)): left is negative.</summary>
        public int DriftMin = -6;
        public int DriftMax = -3;

        /// <summary>Fall per step, original px (irandom_range(40, 50)).</summary>
        public int FallMin = 40;
        public int FallMax = 50;

        /// <summary>spr_rain's draw alpha.</summary>
        public float Alpha = 0.75f;

        public void Validate()
        {
            if (float.IsNaN(RoomHeight) || RoomHeight <= 0f)
                throw new SettingsValidationException("RainTuning.RoomHeight must be positive: " + RoomHeight);
            if (DropsPerStep < 0)
                throw new SettingsValidationException("RainTuning.DropsPerStep must not be negative: " + DropsPerStep);
            if (DropHeight < 0)
                throw new SettingsValidationException("RainTuning.DropHeight must not be negative: " + DropHeight);
            if (DriftMin > DriftMax)
                throw new SettingsValidationException("RainTuning.DriftMin must not exceed DriftMax: " + DriftMin + " > " + DriftMax);
            if (FallMin > FallMax)
                throw new SettingsValidationException("RainTuning.FallMin must not exceed FallMax: " + FallMin + " > " + FallMax);
            if (float.IsNaN(Alpha) || Alpha < 0f || Alpha > 1f)
                throw new SettingsValidationException("RainTuning.Alpha must be between 0 and 1: " + Alpha);
        }

        /// <summary>U.INC-T25: parsed with Unity.Shared.Core's engine-free <see cref="JsonObject"/> reader.</summary>
        public static RainTuning FromJson(JsonObject json)
        {
            var tuning = new RainTuning();
            if (json == null) return tuning;
            tuning.RoomHeight = json.GetFloat(nameof(RoomHeight), tuning.RoomHeight);
            tuning.DropsPerStep = json.GetInt(nameof(DropsPerStep), tuning.DropsPerStep);
            tuning.DropHeight = json.GetInt(nameof(DropHeight), tuning.DropHeight);
            tuning.DriftMin = json.GetInt(nameof(DriftMin), tuning.DriftMin);
            tuning.DriftMax = json.GetInt(nameof(DriftMax), tuning.DriftMax);
            tuning.FallMin = json.GetInt(nameof(FallMin), tuning.FallMin);
            tuning.FallMax = json.GetInt(nameof(FallMax), tuning.FallMax);
            tuning.Alpha = json.GetFloat(nameof(Alpha), tuning.Alpha);
            return tuning;
        }
    }
}
