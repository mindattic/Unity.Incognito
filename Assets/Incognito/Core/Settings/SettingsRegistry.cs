using System;
using Incognito.Npc;

namespace Incognito.Settings
{
    /// <summary>
    /// U.INC-T22's settings registry: every scalar tuning category the game's rules read, in one
    /// validated place, instead of each one's numbers being duplicated as serialized fields on a
    /// MonoBehaviour or left as constants spread through scripts (AGENTS.md law 2/4). Loaded from
    /// <c>Resources/Data/game-tuning.json</c> by <c>Incognito.Settings.GameSettings</c> (Runtime,
    /// since reading a file needs UnityEngine); this type itself stays engine-free so it can be
    /// built and validated from a plain C# test.
    /// </summary>
    [Serializable]
    public sealed class SettingsRegistry
    {
        public SuspicionTuning Suspicion = new SuspicionTuning();
        public LocomotionTuning Locomotion = new LocomotionTuning();
        public CameraTuning Camera = new CameraTuning();
        public WeaponTuning Weapon = new WeaponTuning();
        public RainTuning Rain = new RainTuning();

        /// <summary>The original's values, for callers that need a registry without loading one.</summary>
        public static SettingsRegistry Default => new SettingsRegistry();

        /// <summary>Validates every category; throws <see cref="SettingsValidationException"/> on the first bad value.</summary>
        public void Validate()
        {
            if (Suspicion == null) throw new SettingsValidationException("SettingsRegistry.Suspicion is required.");
            if (Locomotion == null) throw new SettingsValidationException("SettingsRegistry.Locomotion is required.");
            if (Camera == null) throw new SettingsValidationException("SettingsRegistry.Camera is required.");
            if (Weapon == null) throw new SettingsValidationException("SettingsRegistry.Weapon is required.");
            if (Rain == null) throw new SettingsValidationException("SettingsRegistry.Rain is required.");

            Suspicion.Validate();
            Locomotion.Validate();
            Camera.Validate();
            Weapon.Validate();
            Rain.Validate();
        }
    }
}
