using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Incognito.Content;

namespace Incognito.Terminal
{
    /// <summary>
    /// Pure port of the original typed-command terminal (RECONSTRUCTION.md §6:
    /// scripts/exec_command.gml + scripts/command_list.gml). Input is lower-cased and split on
    /// spaces, then dispatched on the first word. The result of each command replaces the
    /// console's output; <c>clear</c> returns an empty output.
    ///
    /// Ported verbatim: <c>help</c> text, <c>list</c> usage text, <c>list sheep</c>
    /// (<c>SHP#&lt;sheep_id&gt;</c> per civilian), <c>clear</c>, and "Command not recognized" for
    /// anything else. GM8's <c>#</c> line break becomes <c>\n</c>.
    ///
    /// Designed beyond the original (its stubs — see BIBLE.md "Decisions made"):
    /// <c>list camera</c>/<c>list computer</c> follow <c>list sheep</c>'s pattern, <c>list all</c>
    /// lists every type, and <c>reboot</c>/<c>overwrite</c> resolve a target in the vicinity and
    /// raise <see cref="Rebooted"/>/<see cref="Overwritten"/> with no built-in consequence.
    /// Incidental GM8 quirks cleaned up: runs of spaces no longer produce empty words, each
    /// listed id gets its own line (the original concatenated them), a blank line runs nothing,
    /// and "civilans" is spelled correctly.
    /// </summary>
    public sealed class CommandInterpreter
    {
        /// <summary>
        /// The original's text, kept as a back-compat constant for callers that just want the
        /// known default (as this class's own tests do). The live value a built interpreter prints
        /// is <see cref="ConsoleText.NotRecognizedText"/> (U.INC-T22): data, not a literal in this
        /// class's methods, so the console's copy can be authored without touching code.
        /// </summary>
        public const string NotRecognized = "Command not recognized";

        private static readonly ConsoleObjectType[] AllTypes =
            (ConsoleObjectType[])Enum.GetValues(typeof(ConsoleObjectType));

        private readonly Func<IEnumerable<ConsoleObject>> vicinity;
        private readonly ConsoleText text;

        /// <param name="vicinity">Returns the objects currently in range of the player.</param>
        /// <param name="text">The console's text (U.INC-T22); null takes the original's own copy.</param>
        public CommandInterpreter(Func<IEnumerable<ConsoleObject>> vicinity, ConsoleText text = null)
        {
            this.vicinity = vicinity ?? throw new ArgumentNullException(nameof(vicinity));
            this.text = text ?? new ConsoleText();
        }

        /// <summary>Raised when <c>reboot &lt;id&gt;</c> resolves a target.</summary>
        public event Action<ConsoleObject> Rebooted;

        /// <summary>Raised when <c>overwrite &lt;id&gt;</c> resolves a target.</summary>
        public event Action<ConsoleObject> Overwritten;

        public static string PrefixFor(ConsoleObjectType type)
        {
            switch (type)
            {
                case ConsoleObjectType.Sheep: return "SHP";
                case ConsoleObjectType.Camera: return "CAM";
                case ConsoleObjectType.Computer: return "CMP";
                default: throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        private string HeaderFor(ConsoleObjectType type)
        {
            switch (type)
            {
                case ConsoleObjectType.Sheep: return text.SheepHeader;
                case ConsoleObjectType.Camera: return text.CameraHeader;
                case ConsoleObjectType.Computer: return text.ComputerHeader;
                default: throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        /// <summary>The original's split_string: lower-case, then split on spaces.</summary>
        public static string[] Tokenize(string input)
        {
            return (input ?? string.Empty)
                .ToLowerInvariant()
                .Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>Runs one typed line and returns the console's new output.</summary>
        public string Execute(string input)
        {
            string[] command = Tokenize(input);
            if (command.Length == 0) return string.Empty;

            switch (command[0])
            {
                case "help": return Help();
                case "list": return List(command);
                case "reboot": return Target(command, "reboot", text.RebootSummary, text.RebootProgressive, Rebooted);
                case "overwrite": return Target(command, "overwrite", text.OverwriteSummary, text.OverwriteProgressive, Overwritten);
                case "clear": return string.Empty;
                default: return text.NotRecognizedText;
            }
        }

        private string Help() => text.HelpText;

        private string List(string[] command)
        {
            if (command.Length == 1)
            {
                return text.ListUsageText;
            }

            List<ConsoleObject> nearby = vicinity().ToList();

            if (command[1] == "all")
            {
                var sections = AllTypes
                    .Where(t => nearby.Any(o => o.Type == t))
                    .Select(t => Section(t, nearby))
                    .ToList();
                return sections.Count == 0 ? text.NoObjectsDetectedText : string.Join("\n\n", sections);
            }

            foreach (ConsoleObjectType type in AllTypes)
            {
                if (command[1] == TypeWord(type)) return Section(type, nearby);
            }

            return text.NotRecognizedText;
        }

        private string Section(ConsoleObjectType type, IEnumerable<ConsoleObject> nearby)
        {
            var sb = new StringBuilder(HeaderFor(type)).Append('\n');
            bool any = false;
            foreach (ConsoleObject obj in nearby.Where(o => o.Type == type))
            {
                sb.Append('\n').Append(obj.Handle);
                any = true;
            }

            if (!any) sb.Append("\n   ").Append(text.NoneLabel);
            return sb.ToString();
        }

        private string Target(string[] command, string verb, string summary, string progressive, Action<ConsoleObject> raise)
        {
            if (command.Length == 1)
            {
                return verb + "\n\n" +
                       "   Usage:\n" +
                       "   " + verb + " [id]: " + summary + ".\n" +
                       "   e.g. " + verb + " shp#0097 (see list).";
            }

            string wanted = command[1];
            foreach (ConsoleObject obj in vicinity())
            {
                if (Matches(obj, wanted))
                {
                    raise?.Invoke(obj);
                    return progressive + " " + obj.Handle + "...";
                }
            }

            return text.ObjectNotFoundPrefix + wanted;
        }

        /// <summary>Accepts the printed handle (<c>shp#0097</c>) or the bare id (<c>0097</c>).</summary>
        private static bool Matches(ConsoleObject obj, string wanted)
        {
            return string.Equals(obj.Handle, wanted, StringComparison.OrdinalIgnoreCase)
                || string.Equals(obj.Id, wanted, StringComparison.OrdinalIgnoreCase);
        }

        public static string TypeWord(ConsoleObjectType type) => type.ToString().ToLowerInvariant();
    }
}
