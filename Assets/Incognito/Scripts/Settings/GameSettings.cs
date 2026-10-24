using System;
using Incognito.Content;
using UnityEngine;

namespace Incognito.Settings
{
    /// <summary>
    /// Loads and validates the settings registry and content catalog (U.INC-T22, AGENTS.md law
    /// 2/4: content and tuning are data, validated on load) from
    /// <c>Resources/Data/game-tuning.json</c> and <c>Resources/Data/game-content.json</c>, once,
    /// and caches them. A missing or invalid file throws immediately rather than falling back
    /// silently, so a broken data file is caught at startup, not mid-play.
    /// </summary>
    public static class GameSettings
    {
        private const string TuningPath = "Data/game-tuning";
        private const string ContentPath = "Data/game-content";

        private static SettingsRegistry tuning;
        private static GameContent content;

        /// <summary>The validated scalar tuning (suspicion, locomotion, camera, weapon, rain).</summary>
        public static SettingsRegistry Tuning => tuning ?? (tuning = Load<SettingsRegistry>(TuningPath));

        /// <summary>The validated room/NPC/console content catalog.</summary>
        public static GameContent Content => content ?? (content = Load<GameContent>(ContentPath));

        /// <summary>Forces the next access to re-read and re-validate from disk.</summary>
        public static void Reset()
        {
            tuning = null;
            content = null;
        }

        private static T Load<T>(string resourcePath) where T : class
        {
            var asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null)
                throw new SettingsValidationException("Missing data file: Resources/" + resourcePath + ".json");

            T loaded;
            try
            {
                loaded = JsonUtility.FromJson<T>(asset.text);
            }
            catch (Exception e)
            {
                throw new SettingsValidationException("Could not parse " + resourcePath + ".json: " + e.Message);
            }

            if (loaded == null)
                throw new SettingsValidationException(resourcePath + ".json is empty or malformed.");

            Validate(loaded);
            return loaded;
        }

        private static void Validate(object loaded)
        {
            switch (loaded)
            {
                case SettingsRegistry registry: registry.Validate(); break;
                case GameContent gameContent: gameContent.Validate(); break;
            }
        }
    }
}
