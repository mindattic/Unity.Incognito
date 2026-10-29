using System.Text;

namespace Incognito.Text
{
    /// <summary>
    /// GameMaker 8's text conventions (U.INC-T31): GM8's draw_text has no escape sequences, so
    /// every original string marks a line break with <c>#</c>, a blank line with <c>##</c>
    /// (<c>"help##"</c>, <c>"list##"</c>, <c>"Detected civilans:##"</c> in exec_command.gml and
    /// command_list.gml), and a literal hash with <c>\#</c> (command_list.gml's <c>"SHP\#"</c>, drawn
    /// as <c>SHP#</c>). Authored content (game-content.json's descriptions, opening text, labels and
    /// console copy) keeps the original's own form, and is expanded to display text once, as it is
    /// loaded, so nothing downstream ever sees a GM <c>#</c> again and a UI Toolkit label shows it
    /// as the original drew it.
    /// </summary>
    public static class GmText
    {
        /// <summary>GM8's line break character.</summary>
        public const char LineBreak = '#';

        /// <summary>
        /// GM8 text to display text: <c>#</c> becomes <c>\n</c> (so <c>##</c> is a blank line) and
        /// <c>\#</c> becomes a literal <c>#</c>. Anything else, including a <c>\n</c> already in the
        /// text and a backslash not followed by <c>#</c>, passes through unchanged. Null is empty.
        /// </summary>
        public static string Expand(string gm)
        {
            if (string.IsNullOrEmpty(gm)) return string.Empty;
            if (gm.IndexOf(LineBreak) < 0) return gm;

            var sb = new StringBuilder(gm.Length);
            for (int i = 0; i < gm.Length; i++)
            {
                char c = gm[i];
                if (c == '\\' && i + 1 < gm.Length && gm[i + 1] == LineBreak)
                {
                    sb.Append(LineBreak);
                    i++;
                }
                else if (c == LineBreak)
                {
                    sb.Append('\n');
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        /// <summary>The inverse of <see cref="Expand"/>: display text to GM8 form (a literal
        /// <c>#</c> becomes <c>\#</c>, a <c>\n</c> becomes <c>#</c>). <c>Expand(Escape(s)) == s</c>
        /// for any text without a <c>\r</c>.</summary>
        public static string Escape(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            var sb = new StringBuilder(text.Length);
            foreach (char c in text)
            {
                if (c == LineBreak) sb.Append('\\').Append(LineBreak);
                else if (c == '\n') sb.Append(LineBreak);
                else sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
