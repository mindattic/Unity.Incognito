using System;

namespace Incognito.Settings
{
    /// <summary>
    /// Thrown by a tuning or content class's Validate() when a loaded value is missing, malformed
    /// or out of range (AGENTS.md law 4: content and tuning are data, validated on load, not
    /// serialized fields on MonoBehaviours or constants spread through scripts). Loading fails
    /// loudly at startup instead of letting a broken data file reach play.
    /// </summary>
    public sealed class SettingsValidationException : Exception
    {
        public SettingsValidationException(string message) : base(message)
        {
        }
    }
}
