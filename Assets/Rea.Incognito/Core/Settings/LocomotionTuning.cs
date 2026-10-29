using System;
using Unity.Shared.Core.Json;

namespace Incognito.Settings
{
    /// <summary>
    /// obj_walker's movement tuning (RECONSTRUCTION.md §1): the triangle-wave walk bob, the
    /// snap-to-speed walk with its ramped sprint, and the friction-based stop. Kept as data
    /// (AGENTS.md law 2/4) instead of constants baked into
    /// <see cref="Incognito.Player.PlayerLocomotionState"/>; defaults are the original's values.
    /// </summary>
    [Serializable]
    public sealed class LocomotionTuning
    {
        /// <summary>walkAmount: how far the bob ramps above/below the resting height, original px.</summary>
        public float BobAmplitude = 5f;

        /// <summary>The bob's own walkSpeed: how fast it ramps, original px per step.</summary>
        public float BobRampSpeed = 0.3f;

        /// <summary>walk_speed: horizontal speed while walking, original px per step.</summary>
        public float WalkSpeed = 4f;

        /// <summary>Shift held multiplies the walk speed into the sprint cap by this.</summary>
        public float SprintMultiplier = 2f;

        /// <summary>Shift-held ramp toward the sprint cap, original px per step per step.</summary>
        public float SprintAccelStep = 0.1f;

        /// <summary>Releasing the walk keys: dx *= Friction per step until below StopThreshold.</summary>
        public float Friction = 0.9f;

        public float StopThreshold = 1f;

        public void Validate()
        {
            if (float.IsNaN(BobAmplitude) || BobAmplitude < 0f)
                throw new SettingsValidationException("LocomotionTuning.BobAmplitude must not be negative: " + BobAmplitude);
            if (float.IsNaN(BobRampSpeed) || BobRampSpeed <= 0f)
                throw new SettingsValidationException("LocomotionTuning.BobRampSpeed must be positive: " + BobRampSpeed);
            if (float.IsNaN(WalkSpeed) || WalkSpeed < 0f)
                throw new SettingsValidationException("LocomotionTuning.WalkSpeed must not be negative: " + WalkSpeed);
            if (float.IsNaN(SprintMultiplier) || SprintMultiplier < 1f)
                throw new SettingsValidationException("LocomotionTuning.SprintMultiplier must be at least 1: " + SprintMultiplier);
            if (float.IsNaN(SprintAccelStep) || SprintAccelStep <= 0f)
                throw new SettingsValidationException("LocomotionTuning.SprintAccelStep must be positive: " + SprintAccelStep);
            if (float.IsNaN(Friction) || Friction <= 0f || Friction >= 1f)
                throw new SettingsValidationException("LocomotionTuning.Friction must be between 0 and 1 (exclusive): " + Friction);
            if (float.IsNaN(StopThreshold) || StopThreshold <= 0f)
                throw new SettingsValidationException("LocomotionTuning.StopThreshold must be positive: " + StopThreshold);
        }

        /// <summary>U.INC-T25: parsed with Unity.Shared.Core's engine-free <see cref="JsonObject"/> reader.</summary>
        public static LocomotionTuning FromJson(JsonObject json)
        {
            var tuning = new LocomotionTuning();
            if (json == null) return tuning;
            tuning.BobAmplitude = json.GetFloat(nameof(BobAmplitude), tuning.BobAmplitude);
            tuning.BobRampSpeed = json.GetFloat(nameof(BobRampSpeed), tuning.BobRampSpeed);
            tuning.WalkSpeed = json.GetFloat(nameof(WalkSpeed), tuning.WalkSpeed);
            tuning.SprintMultiplier = json.GetFloat(nameof(SprintMultiplier), tuning.SprintMultiplier);
            tuning.SprintAccelStep = json.GetFloat(nameof(SprintAccelStep), tuning.SprintAccelStep);
            tuning.Friction = json.GetFloat(nameof(Friction), tuning.Friction);
            tuning.StopThreshold = json.GetFloat(nameof(StopThreshold), tuning.StopThreshold);
            return tuning;
        }
    }
}
