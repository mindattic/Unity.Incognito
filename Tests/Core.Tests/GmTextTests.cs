// NUnit 4 moved the classic Assert; the tests were written against it (Unity Test Framework).
using Assert = NUnit.Framework.Legacy.ClassicAssert;
using NUnit.Framework.Legacy;
using System.IO;
using System.Linq;
using Incognito.Content;
using Incognito.Text;
using NUnit.Framework;

namespace Incognito.Tests
{
    /// <summary>
    /// U.INC-T31: GM8's '#' newline, '##' blank line and '\#' literal hash (<see cref="GmText"/>),
    /// and the shipped content being authored in that form and expanded once, on load.
    /// </summary>
    public class GmTextTests
    {
        [Test]
        public void A_hash_is_a_line_break()
        {
            Assert.AreEqual("It's just a mailbox.\n...or is it?", GmText.Expand("It's just a mailbox.#...or is it?"));
        }

        [Test]
        public void Two_hashes_are_a_blank_line()
        {
            // exec_command.gml's help text.
            Assert.AreEqual("help\n\n   list: List objects in vicinity\n",
                GmText.Expand("help##   list: List objects in vicinity#"));
        }

        [Test]
        public void An_escaped_hash_is_a_literal_hash()
        {
            // command_list.gml's "SHP\#" + sheep_id.
            Assert.AreEqual("SHP#0097", GmText.Expand("SHP\\#0097"));
        }

        [Test]
        public void Text_without_a_hash_passes_through_unchanged()
        {
            Assert.AreEqual("Command not recognized", GmText.Expand("Command not recognized"));
            Assert.AreEqual("a\\b\nc", GmText.Expand("a\\b\nc"));
            Assert.AreEqual(string.Empty, GmText.Expand(null));
            Assert.AreEqual(string.Empty, GmText.Expand(string.Empty));
        }

        [Test]
        public void A_trailing_backslash_or_hash_is_handled()
        {
            Assert.AreEqual("end\\", GmText.Expand("end\\"));
            Assert.AreEqual("a#\\", GmText.Expand("a\\#\\"));
            Assert.AreEqual("line\n", GmText.Expand("line#"));
        }

        [Test]
        public void Escape_is_the_inverse_of_expand()
        {
            foreach (string text in new[] { "", "plain", "SHP#0097", "a\nb\n\nc", "x\\#y", "#\n#", "back\\slash" })
                Assert.AreEqual(text, GmText.Expand(GmText.Escape(text)), "round trip of \"" + text + "\"");
            Assert.AreEqual("list##SHP\\#0097", GmText.Escape("list\n\nSHP#0097"));
        }

        [Test]
        public void The_console_defaults_are_the_originals_GM_text_expanded()
        {
            var text = new ConsoleText();
            Assert.AreEqual(
                "help\n\n   list: List objects in vicinity\n   reboot: Reboots an object\n   overwrite: Overwrite command level function",
                text.HelpText);
            StringAssert.StartsWith("list\n\n   Usage:\n", text.ListUsageText);
        }

        // ------------------------------------------------------------ the shipped content

        private static string ContentPath()
        {
            var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "Assets", "Rea.Incognito", "Resources", "Data", "game-content.json")))
                dir = dir.Parent;
            Assert.IsNotNull(dir, "Could not find the repo root above the test directory.");
            return Path.Combine(dir.FullName, "Assets", "Rea.Incognito", "Resources", "Data", "game-content.json");
        }

        [Test]
        public void The_shipped_content_is_authored_in_GM_form()
        {
            string json = File.ReadAllText(ContentPath());
            Assert.IsFalse(json.Contains("\\n"), "game-content.json writes line breaks as the original's '#', not \\n");
            StringAssert.Contains("A young woman with tell tale red eyes and a meandering gait.#Bless the young and stoned.", json);
            StringAssert.Contains("\"help##   list: List objects in vicinity#", json);
        }

        [Test]
        public void The_shipped_content_loads_as_display_text()
        {
            GameContent content = GameContent.FromJson(File.ReadAllText(ContentPath()));

            Assert.AreEqual("A young woman with tell tale red eyes and a meandering gait.\nBless the young and stoned.",
                content.FindNpc("0097").Description);
            RoomDefinition street = content.FindRoom("Street00");
            Assert.AreEqual("It's just a mailbox.\n...or is it?", street.Mailboxes[0].Description);
            Assert.AreEqual(10, street.OpeningText.Split('\n').Length, "the poem's ten lines");
            StringAssert.StartsWith("help\n\n", content.Console.HelpText);

            var display = content.Rooms.SelectMany(r =>
                    new[] { r.DisplayName, r.OpeningText }
                        .Concat(r.Lampposts.Select(p => p.Description))
                        .Concat(r.Mailboxes.Select(p => p.Description))
                        .Concat(r.Doors.Select(d => d.Label)))
                .Concat(content.Npcs.Select(n => n.Description))
                .Concat(new[] { content.Console.HelpText, content.Console.ListUsageText });
            foreach (string s in display)
                Assert.IsFalse(s.Contains("#"), "\"" + s + "\" still has a GM '#'");
        }
    }

    /// <summary>U.INC-T31: obj_float_text's step event (<see cref="FloatTextState"/>).</summary>
    public class FloatTextStateTests
    {
        [Test]
        public void It_starts_opaque_where_it_was_shown()
        {
            var text = new FloatTextState("+1");
            Assert.AreEqual("+1", text.Text);
            Assert.AreEqual(1f, text.Alpha);
            Assert.AreEqual(0f, text.Rise);
            Assert.IsFalse(text.Done);
        }

        [Test]
        public void Each_step_fades_by_0_007_and_rises_one_pixel()
        {
            var text = new FloatTextState("x");
            text.Step(1);
            Assert.AreEqual(0.993f, text.Alpha, 1e-5f);
            Assert.AreEqual(1f, text.Rise);
            text.Step(9);
            Assert.AreEqual(0.93f, text.Alpha, 1e-4f);
            Assert.AreEqual(10f, text.Rise);
        }

        [Test]
        public void It_destroys_itself_on_the_step_after_alpha_reaches_zero()
        {
            // alpha > 0 for 143 steps (1 - 143 * 0.007 < 0), so it rises 143px, then the 144th
            // step finds alpha <= 0 and destroys it.
            var text = new FloatTextState("x");
            text.Step(143);
            Assert.IsFalse(text.Done);
            Assert.AreEqual(143f, text.Rise);
            Assert.AreEqual(0f, text.Alpha, "a dipped-negative alpha draws as 0");

            text.Step(1);
            Assert.IsTrue(text.Done);
            Assert.AreEqual(143f, text.Rise, "a destroyed text no longer moves");

            text.Step(10);
            Assert.AreEqual(143f, text.Rise);
        }

        [Test]
        public void It_is_drawn_10px_above_its_y_with_a_1px_outline()
        {
            Assert.AreEqual(-10f, FloatTextState.DrawOffsetY);
            Assert.AreEqual(1, FloatTextState.OutlineWidth);
        }

        [Test]
        public void Null_text_is_empty()
        {
            Assert.AreEqual(string.Empty, new FloatTextState(null).Text);
        }
    }
}
