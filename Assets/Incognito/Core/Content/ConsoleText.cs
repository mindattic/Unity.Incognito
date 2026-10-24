using System;
using Incognito.Settings;
using Unity.Shared.Core.Json;

namespace Incognito.Content
{
    /// <summary>
    /// Every string the typed console prints (RECONSTRUCTION.md §6), ported verbatim from
    /// scripts/exec_command.gml/command_list.gml. Kept as data (AGENTS.md law 2/4) instead of
    /// literals embedded in <see cref="Incognito.Terminal.CommandInterpreter"/>'s methods, so the
    /// console's copy can be authored without touching code. Defaults below are the original's
    /// text, so <see cref="Incognito.Terminal.CommandInterpreter"/> behaves identically when no
    /// data file overrides them (as in its own unit tests).
    /// </summary>
    [Serializable]
    public sealed class ConsoleText
    {
        public string HelpText =
            "help\n\n" +
            "   list: List objects in vicinity\n" +
            "   reboot: Reboots an object\n" +
            "   overwrite: Overwrite command level function";

        public string ListUsageText =
            "list\n\n" +
            "   Usage:\n" +
            "   list all: List all objects in vicinity\n" +
            "   list [type]: List all objects of a certain type.\n" +
            "   [sheep, camera, computer].";

        public string SheepHeader = "Detected civilians:";
        public string CameraHeader = "Detected cameras:";
        public string ComputerHeader = "Detected computers:";

        public string NoneLabel = "none";
        public string NoObjectsDetectedText = "No objects detected";
        public string NotRecognizedText = "Command not recognized";
        public string ObjectNotFoundPrefix = "Object not found: ";

        public string RebootSummary = "Reboots an object";
        public string RebootProgressive = "Rebooting";
        public string OverwriteSummary = "Overwrite command level function";
        public string OverwriteProgressive = "Overwriting";

        public void Validate()
        {
            Require(nameof(HelpText), HelpText);
            Require(nameof(ListUsageText), ListUsageText);
            Require(nameof(SheepHeader), SheepHeader);
            Require(nameof(CameraHeader), CameraHeader);
            Require(nameof(ComputerHeader), ComputerHeader);
            Require(nameof(NoneLabel), NoneLabel);
            Require(nameof(NoObjectsDetectedText), NoObjectsDetectedText);
            Require(nameof(NotRecognizedText), NotRecognizedText);
            Require(nameof(ObjectNotFoundPrefix), ObjectNotFoundPrefix);
            Require(nameof(RebootSummary), RebootSummary);
            Require(nameof(RebootProgressive), RebootProgressive);
            Require(nameof(OverwriteSummary), OverwriteSummary);
            Require(nameof(OverwriteProgressive), OverwriteProgressive);
        }

        private static void Require(string field, string value)
        {
            if (string.IsNullOrEmpty(value))
                throw new SettingsValidationException("ConsoleText." + field + " must not be empty.");
        }

        /// <summary>U.INC-T25: parsed with Unity.Shared.Core's engine-free <see cref="JsonObject"/> reader.</summary>
        public static ConsoleText FromJson(JsonObject json)
        {
            var text = new ConsoleText();
            if (json == null) return text;
            text.HelpText = json.GetString(nameof(HelpText), text.HelpText);
            text.ListUsageText = json.GetString(nameof(ListUsageText), text.ListUsageText);
            text.SheepHeader = json.GetString(nameof(SheepHeader), text.SheepHeader);
            text.CameraHeader = json.GetString(nameof(CameraHeader), text.CameraHeader);
            text.ComputerHeader = json.GetString(nameof(ComputerHeader), text.ComputerHeader);
            text.NoneLabel = json.GetString(nameof(NoneLabel), text.NoneLabel);
            text.NoObjectsDetectedText = json.GetString(nameof(NoObjectsDetectedText), text.NoObjectsDetectedText);
            text.NotRecognizedText = json.GetString(nameof(NotRecognizedText), text.NotRecognizedText);
            text.ObjectNotFoundPrefix = json.GetString(nameof(ObjectNotFoundPrefix), text.ObjectNotFoundPrefix);
            text.RebootSummary = json.GetString(nameof(RebootSummary), text.RebootSummary);
            text.RebootProgressive = json.GetString(nameof(RebootProgressive), text.RebootProgressive);
            text.OverwriteSummary = json.GetString(nameof(OverwriteSummary), text.OverwriteSummary);
            text.OverwriteProgressive = json.GetString(nameof(OverwriteProgressive), text.OverwriteProgressive);
            return text;
        }
    }
}
