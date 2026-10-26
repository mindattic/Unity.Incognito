// NUnit 4 moved the classic Assert; the tests were written against it (Unity Test Framework).
using Assert = NUnit.Framework.Legacy.ClassicAssert;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Incognito.Tests
{
    /// <summary>
    /// U.INC-T35: the parity checklist in docs/RECONSTRUCTION.md section 11 cannot rot. Every row is
    /// ticked, every <c>tour:</c> row is a <c>Row("id")</c> ParityTourPlayTests still proves (and the
    /// tour proves no id the checklist lacks), every <c>test:</c> row names a test class that exists,
    /// and every <c>decided:</c> row is recorded in BIBLE.md. Reads the real repo files.
    /// </summary>
    public class ParityChecklistTests
    {
        private static string repoRoot;
        private static List<(bool Ticked, string Id)> rows;

        [OneTimeSetUp]
        public void LoadChecklist()
        {
            var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "docs", "RECONSTRUCTION.md")))
                dir = dir.Parent;
            Assert.IsNotNull(dir, "Could not find the repo root (docs/RECONSTRUCTION.md) above the test directory.");
            repoRoot = dir.FullName;

            string doc = File.ReadAllText(Path.Combine(repoRoot, "docs", "RECONSTRUCTION.md"));
            rows = Regex.Matches(doc, @"^\|\s*\[(?<t>[ xX])\]\s*\|\s*`(?<id>[a-z]+:[^`]+)`", RegexOptions.Multiline)
                .Cast<Match>().Select(m => (m.Groups["t"].Value != " ", m.Groups["id"].Value)).ToList();
        }

        private static IEnumerable<string> Ids(string kind) =>
            rows.Where(r => r.Id.StartsWith(kind + ":")).Select(r => r.Id.Substring(kind.Length + 1));

        [Test]
        public void The_checklist_exists_and_every_row_is_ticked()
        {
            Assert.Greater(rows.Count, 40, "docs/RECONSTRUCTION.md section 11 has the parity checklist");
            Assert.AreEqual(rows.Count, rows.Select(r => r.Id).Distinct().Count(), "no row is listed twice");
            Assert.IsEmpty(rows.Where(r => !r.Ticked).Select(r => r.Id), "unticked parity rows");
        }

        [Test]
        public void Every_tour_row_is_proved_by_the_tour_and_the_tour_proves_nothing_unlisted()
        {
            string tour = File.ReadAllText(Path.Combine(repoRoot, "Assets", "Incognito", "Tests", "PlayMode", "ParityTourPlayTests.cs"));
            var proved = Regex.Matches(tour, @"Row\(""([a-z]+\.[^""]+)""\)").Cast<Match>().Select(m => m.Groups[1].Value).ToList();
            var listed = Ids("tour").ToList();

            Assert.IsEmpty(listed.Except(proved), "checklist rows the tour no longer proves");
            Assert.IsEmpty(proved.Except(listed), "tour proofs missing from the checklist");
        }

        [Test]
        public void Every_test_row_names_a_test_class_that_exists()
        {
            string tests = string.Join("\n", Directory.GetFiles(Path.Combine(repoRoot, "Assets", "Incognito", "Tests"), "*.cs", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(Path.Combine(repoRoot, "Tests", "Core.Tests"), "*.cs"))
                .Select(File.ReadAllText));

            foreach (string name in Ids("test"))
                Assert.IsTrue(Regex.IsMatch(tests, @"public\s+(sealed\s+)?class\s+" + Regex.Escape(name) + @"\b"), name + " is not a test class");
        }

        [Test]
        public void Every_decided_row_is_recorded_in_the_bible()
        {
            string bible = File.ReadAllText(Path.Combine(repoRoot, "docs", "BIBLE.md"));
            foreach (string id in Ids("decided"))
                Assert.That(bible, Does.Contain("decided:" + id), id + " is not recorded in BIBLE.md");
        }
    }
}
