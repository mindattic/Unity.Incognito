using System;
using Unity.Shared.Core.Json;

namespace Incognito.Settings
{
    /// <summary>
    /// The original weapon handling's tuning (RECONSTRUCTION.md §1): the aim raise delay, the
    /// aim-angle geometry and both arms' walking swing (obj_arm defaults plus obj_walker's
    /// Shift-held values). Kept as data (AGENTS.md law 2/4) instead of constants on
    /// <see cref="Incognito.Weapon.PlayerWeaponState"/>; defaults are the original's values.
    /// </summary>
    [Serializable]
    public sealed class WeaponTuning
    {
        /// <summary>Original: alarm[0] = 5 between the right-click and the settled aim pose.</summary>
        public float AimRaiseSteps = 5f;

        /// <summary>Original room/view height; the aim angle is measured from its middle.</summary>
        public float ViewHeight = 1080f;

        /// <summary>Original: image_angle = (mouse_y - room_height/2) / 30.</summary>
        public float AimDivisor = 30f;

        /// <summary>shoot_bullet.gml: Len = 500 along the arm from its pivot...</summary>
        public float MuzzleReach = 500f;

        /// <summary>...then 88px up (unrotated).</summary>
        public float MuzzleRise = 88f;

        /// <summary>obj_arm's default swing range and speed while walking.</summary>
        public float WalkSwingRange = 3f;
        public float WalkRotSpeed = 0.06f;

        /// <summary>obj_walker's Shift-held (sprint) values.</summary>
        public float SprintSwingRange = 6f;
        public float SprintRotSpeed = 0.24f;

        /// <summary>shoot_bullet.gml: effect_create_below(ef_smoke, originX, originY, 1, c_black).
        /// ef_smoke is a GameMaker built-in particle effect with no original script or asset to
        /// read sizes from, so these are the rebuild's own small values for the puff (U.INC-T41).</summary>
        public int MuzzleSmokeParticles = 6;

        /// <summary>How long one puff particle lives, in 60Hz steps (~0.5s).</summary>
        public int MuzzleSmokeLifeSteps = 30;

        /// <summary>Sideways drift per step, original px.</summary>
        public float MuzzleSmokeDriftMin = -1.5f;
        public float MuzzleSmokeDriftMax = 1.5f;

        /// <summary>Upward rise per step, original px (smoke only ever rises).</summary>
        public float MuzzleSmokeRiseMin = 1f;
        public float MuzzleSmokeRiseMax = 3f;

        /// <summary>One particle's diameter, original px.</summary>
        public float MuzzleSmokeSize = 14f;

        public void Validate()
        {
            if (float.IsNaN(AimRaiseSteps) || AimRaiseSteps < 0f)
                throw new SettingsValidationException("WeaponTuning.AimRaiseSteps must not be negative: " + AimRaiseSteps);
            if (float.IsNaN(ViewHeight) || ViewHeight <= 0f)
                throw new SettingsValidationException("WeaponTuning.ViewHeight must be positive: " + ViewHeight);
            if (float.IsNaN(AimDivisor) || AimDivisor == 0f)
                throw new SettingsValidationException("WeaponTuning.AimDivisor must not be zero.");
            if (float.IsNaN(MuzzleReach) || MuzzleReach < 0f)
                throw new SettingsValidationException("WeaponTuning.MuzzleReach must not be negative: " + MuzzleReach);
            if (float.IsNaN(MuzzleRise))
                throw new SettingsValidationException("WeaponTuning.MuzzleRise must be a number.");
            if (float.IsNaN(WalkSwingRange) || WalkSwingRange <= 0f)
                throw new SettingsValidationException("WeaponTuning.WalkSwingRange must be positive: " + WalkSwingRange);
            if (float.IsNaN(WalkRotSpeed) || WalkRotSpeed <= 0f)
                throw new SettingsValidationException("WeaponTuning.WalkRotSpeed must be positive: " + WalkRotSpeed);
            if (float.IsNaN(SprintSwingRange) || SprintSwingRange <= 0f)
                throw new SettingsValidationException("WeaponTuning.SprintSwingRange must be positive: " + SprintSwingRange);
            if (float.IsNaN(SprintRotSpeed) || SprintRotSpeed <= 0f)
                throw new SettingsValidationException("WeaponTuning.SprintRotSpeed must be positive: " + SprintRotSpeed);
            if (MuzzleSmokeParticles < 0)
                throw new SettingsValidationException("WeaponTuning.MuzzleSmokeParticles must not be negative: " + MuzzleSmokeParticles);
            if (MuzzleSmokeLifeSteps <= 0)
                throw new SettingsValidationException("WeaponTuning.MuzzleSmokeLifeSteps must be positive: " + MuzzleSmokeLifeSteps);
            if (float.IsNaN(MuzzleSmokeDriftMin) || float.IsNaN(MuzzleSmokeDriftMax) || MuzzleSmokeDriftMin > MuzzleSmokeDriftMax)
                throw new SettingsValidationException("WeaponTuning.MuzzleSmokeDriftMin must not exceed MuzzleSmokeDriftMax: " + MuzzleSmokeDriftMin + " > " + MuzzleSmokeDriftMax);
            if (float.IsNaN(MuzzleSmokeRiseMin) || float.IsNaN(MuzzleSmokeRiseMax) || MuzzleSmokeRiseMin > MuzzleSmokeRiseMax)
                throw new SettingsValidationException("WeaponTuning.MuzzleSmokeRiseMin must not exceed MuzzleSmokeRiseMax: " + MuzzleSmokeRiseMin + " > " + MuzzleSmokeRiseMax);
            if (float.IsNaN(MuzzleSmokeSize) || MuzzleSmokeSize < 0f)
                throw new SettingsValidationException("WeaponTuning.MuzzleSmokeSize must not be negative: " + MuzzleSmokeSize);
        }

        /// <summary>U.INC-T25: parsed with Unity.Shared.Core's engine-free <see cref="JsonObject"/> reader.</summary>
        public static WeaponTuning FromJson(JsonObject json)
        {
            var tuning = new WeaponTuning();
            if (json == null) return tuning;
            tuning.AimRaiseSteps = json.GetFloat(nameof(AimRaiseSteps), tuning.AimRaiseSteps);
            tuning.ViewHeight = json.GetFloat(nameof(ViewHeight), tuning.ViewHeight);
            tuning.AimDivisor = json.GetFloat(nameof(AimDivisor), tuning.AimDivisor);
            tuning.MuzzleReach = json.GetFloat(nameof(MuzzleReach), tuning.MuzzleReach);
            tuning.MuzzleRise = json.GetFloat(nameof(MuzzleRise), tuning.MuzzleRise);
            tuning.WalkSwingRange = json.GetFloat(nameof(WalkSwingRange), tuning.WalkSwingRange);
            tuning.WalkRotSpeed = json.GetFloat(nameof(WalkRotSpeed), tuning.WalkRotSpeed);
            tuning.SprintSwingRange = json.GetFloat(nameof(SprintSwingRange), tuning.SprintSwingRange);
            tuning.SprintRotSpeed = json.GetFloat(nameof(SprintRotSpeed), tuning.SprintRotSpeed);
            tuning.MuzzleSmokeParticles = json.GetInt(nameof(MuzzleSmokeParticles), tuning.MuzzleSmokeParticles);
            tuning.MuzzleSmokeLifeSteps = json.GetInt(nameof(MuzzleSmokeLifeSteps), tuning.MuzzleSmokeLifeSteps);
            tuning.MuzzleSmokeDriftMin = json.GetFloat(nameof(MuzzleSmokeDriftMin), tuning.MuzzleSmokeDriftMin);
            tuning.MuzzleSmokeDriftMax = json.GetFloat(nameof(MuzzleSmokeDriftMax), tuning.MuzzleSmokeDriftMax);
            tuning.MuzzleSmokeRiseMin = json.GetFloat(nameof(MuzzleSmokeRiseMin), tuning.MuzzleSmokeRiseMin);
            tuning.MuzzleSmokeRiseMax = json.GetFloat(nameof(MuzzleSmokeRiseMax), tuning.MuzzleSmokeRiseMax);
            tuning.MuzzleSmokeSize = json.GetFloat(nameof(MuzzleSmokeSize), tuning.MuzzleSmokeSize);
            return tuning;
        }
    }
}
