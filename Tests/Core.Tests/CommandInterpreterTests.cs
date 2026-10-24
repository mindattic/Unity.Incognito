// NUnit 4 moved the classic Assert; the tests were written against it (Unity Test Framework).
using Assert = NUnit.Framework.Legacy.ClassicAssert;
using CollectionAssert = NUnit.Framework.Legacy.CollectionAssert;
using StringAssert = NUnit.Framework.Legacy.StringAssert;
using System.Collections.Generic;
using System.Linq;
using Incognito.Terminal;
using NUnit.Framework;

namespace Incognito.Tests
{
    public class CommandInterpreterTests
    {
        private List<ConsoleObject> nearby;
        private CommandInterpreter interpreter;

        [SetUp]
        public void SetUp()
        {
            nearby = new List<ConsoleObject>
            {
                new ConsoleObject(ConsoleObjectType.Sheep, "0097"),
                new ConsoleObject(ConsoleObjectType.Sheep, "0102"),
                new ConsoleObject(ConsoleObjectType.Camera, "07"),
            };
            interpreter = new CommandInterpreter(() => nearby);
        }

        [Test]
        public void Help_lists_list_reboot_and_overwrite_as_in_the_original()
        {
            Assert.AreEqual(
                "help\n\n" +
                "   list: List objects in vicinity\n" +
                "   reboot: Reboots an object\n" +
                "   overwrite: Overwrite command level function",
                interpreter.Execute("help"));
        }

        [Test]
        public void Input_is_lower_cased_before_dispatch()
        {
            Assert.AreEqual(interpreter.Execute("help"), interpreter.Execute("HeLP"));
        }

        [Test]
        public void Bare_list_prints_usage()
        {
            string output = interpreter.Execute("list");
            StringAssert.StartsWith("list\n\n   Usage:\n", output);
            StringAssert.Contains("list all: List all objects in vicinity", output);
            StringAssert.Contains("[sheep, camera, computer].", output);
        }

        [Test]
        public void List_sheep_prints_each_civilian_handle_on_its_own_line()
        {
            Assert.AreEqual("Detected civilians:\n\nSHP#0097\nSHP#0102", interpreter.Execute("list sheep"));
        }

        [Test]
        public void List_of_a_type_with_nothing_nearby_says_none()
        {
            Assert.AreEqual("Detected computers:\n\n   none", interpreter.Execute("list computer"));
        }

        [Test]
        public void List_all_groups_every_present_type()
        {
            Assert.AreEqual(
                "Detected civilians:\n\nSHP#0097\nSHP#0102\n\nDetected cameras:\n\nCAM#07",
                interpreter.Execute("list all"));
        }

        [Test]
        public void List_all_with_nothing_nearby()
        {
            nearby.Clear();
            Assert.AreEqual("No objects detected", interpreter.Execute("list all"));
        }

        [Test]
        public void List_unknown_type_is_not_recognized()
        {
            Assert.AreEqual(CommandInterpreter.NotRecognized, interpreter.Execute("list toasters"));
        }

        [Test]
        public void Clear_empties_the_output()
        {
            Assert.AreEqual(string.Empty, interpreter.Execute("clear"));
        }

        [Test]
        public void Unknown_command_is_not_recognized()
        {
            Assert.AreEqual("Command not recognized", interpreter.Execute("hack the planet"));
        }

        [Test]
        public void Blank_line_does_nothing()
        {
            Assert.AreEqual(string.Empty, interpreter.Execute("   "));
        }

        [Test]
        public void Extra_spaces_do_not_create_empty_words()
        {
            Assert.AreEqual(interpreter.Execute("list sheep"), interpreter.Execute("  list   sheep "));
        }

        [Test]
        public void Reboot_by_handle_raises_event_for_that_object()
        {
            var raised = new List<ConsoleObject>();
            interpreter.Rebooted += raised.Add;

            Assert.AreEqual("Rebooting SHP#0102...", interpreter.Execute("reboot SHP#0102"));
            CollectionAssert.AreEqual(new[] { nearby[1] }, raised);
        }

        [Test]
        public void Overwrite_by_bare_id_raises_event()
        {
            var raised = new List<ConsoleObject>();
            interpreter.Overwritten += raised.Add;

            Assert.AreEqual("Overwriting CAM#07...", interpreter.Execute("overwrite 07"));
            CollectionAssert.AreEqual(new[] { nearby[2] }, raised);
        }

        [Test]
        public void Reboot_of_missing_object_reports_not_found_and_raises_nothing()
        {
            bool raised = false;
            interpreter.Rebooted += _ => raised = true;

            Assert.AreEqual("Object not found: shp#9999", interpreter.Execute("reboot shp#9999"));
            Assert.IsFalse(raised);
        }

        [Test]
        public void Bare_reboot_and_overwrite_print_usage()
        {
            StringAssert.StartsWith("reboot\n\n   Usage:\n", interpreter.Execute("reboot"));
            StringAssert.StartsWith("overwrite\n\n   Usage:\n", interpreter.Execute("overwrite"));
        }
    }
}
