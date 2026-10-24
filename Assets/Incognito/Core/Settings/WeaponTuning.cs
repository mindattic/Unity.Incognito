using System;

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
        }
    }
}
