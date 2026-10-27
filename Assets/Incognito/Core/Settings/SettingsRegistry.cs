using System;
using Incognito.Npc;
using Unity.Shared.Core.Json;

namespace Incognito.Settings
{
    /// <summary>
    /// U.INC-T22's settings registry: every scalar tuning category the game's rules read, in one
    /// validated place, instead of each one's numbers being duplicated as serialized fields on a
    /// MonoBehaviour or left as constants spread through scripts (AGENTS.md law 2/4). Loaded from
    /// <c>Resources/Data/game-tuning.json</c> by <c>Incognito.Settings.GameSettings</c> (Runtime,
    /// which only reads the file's bytes, since that needs UnityEngine); parsing is done here with
    /// Unity.Shared.Core's engine-free JSON reader (U.INC-T25, replacing UnityEngine.JsonUtility),
    /// so this type stays engine-free and can be built, parsed and validated from a plain C# test.
    /// </summary>
    [Serializable]
    public sealed class SettingsRegistry
    {
        public SuspicionTuning Suspicion = new SuspicionTuning();
        public LocomotionTuning Locomotion = new LocomotionTuning();
        public CameraTuning Camera = new CameraTuning();
        public WeaponTuning Weapon = new WeaponTuning();
        public RainTuning Rain = new RainTuning();
        public DisplayTuning Display = new DisplayTuning();
        public OnlineTuning Online = new OnlineTuning();

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
            if (Display == null) throw new SettingsValidationException("SettingsRegistry.Display is required.");
            if (Online == null) throw new SettingsValidationException("SettingsRegistry.Online is required.");

            Suspicion.Validate();
            Locomotion.Validate();
            Camera.Validate();
            Weapon.Validate();
            Rain.Validate();
            Display.Validate();
            Online.Validate();
        }

        /// <summary>
        /// Parses <paramref name="json"/> (<c>game-tuning.json</c>'s text) with Unity.Shared.Core's
        /// <see cref="MiniJson"/>/<see cref="JsonObject"/> reader (U.INC-T25). Throws
        /// <see cref="JsonFormatException"/> on text that is not a JSON object; does not validate
        /// (callers validate separately, as <c>GameSettings.Load</c> does).
        /// </summary>
        public static SettingsRegistry FromJson(string json)
        {
            JsonObject root = JsonObject.Parse(json);
            return new SettingsRegistry
            {
                Suspicion = SuspicionTuning.FromJson(root.GetObject(nameof(Suspicion))),
                Locomotion = LocomotionTuning.FromJson(root.GetObject(nameof(Locomotion))),
                Camera = CameraTuning.FromJson(root.GetObject(nameof(Camera))),
                Weapon = WeaponTuning.FromJson(root.GetObject(nameof(Weapon))),
                Rain = RainTuning.FromJson(root.GetObject(nameof(Rain))),
                Display = DisplayTuning.FromJson(root.GetObject(nameof(Display))),
                Online = OnlineTuning.FromJson(root.GetObject(nameof(Online))),
            };
        }
    }
}
