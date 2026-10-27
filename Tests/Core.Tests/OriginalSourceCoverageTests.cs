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
    /// U.INC-T42: the reverse-engineering record names everything. Every object, script, room and
    /// font in the original project is named in docs/RECONSTRUCTION.md or docs/BIBLE.md, either as
    /// what the rebuild ports or with the reason it is not ported, so the REA can't silently miss
    /// one. (Sprites, backgrounds and sounds are diffed against the content catalogs by
    /// OriginalArtParityTests and OriginalAudioParityTests.) A family of numbered resources counts
    /// as named when the docs name its range (<c>obj_cloud1..4</c>, <c>obj_cloud1</c>..<c>obj_cloud4</c>).
    /// Runs only where the original project is on this machine.
    /// </summary>
    public class OriginalSourceCoverageTests
    {
        private const string OriginalRoot = @"R:\Projects\_PRE_2022\_PRE_2021\_PRE_2020\_PRE_2017\gmlIncognito\09-05-13_2.19am";

        private static readonly (string Folder, string Suffix)[] Kinds =
        {
            ("objects", ".object.gmx"),
            ("scripts", ".gml"),
            ("rooms", ".room.gmx"),
            ("fonts", ".font.gmx"),
        };

        private static string RepoRoot()
        {
            var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "docs", "RECONSTRUCTION.md"))) dir = dir.Parent;
            Assert.IsNotNull(dir, "Could not find the repo root (docs/RECONSTRUCTION.md) above the test directory.");
            return dir.FullName;
        }

        private static IEnumerable<string> OriginalNames()
        {
            foreach (var (folder, suffix) in Kinds)
            {
                string path = Path.Combine(OriginalRoot, folder);
                if (!Directory.Exists(path)) continue;
                foreach (string file in Directory.GetFiles(path, "*" + suffix))
                {
                    string name = Path.GetFileName(file);
                    yield return name.Substring(0, name.Length - suffix.Length);
                }
            }
        }

        // The docs' range forms: "obj_cloud1..4", "obj_cloud1..obj_cloud4", "`obj_cloud1`..`obj_cloud4`",
        // "obj_street00_00..05". A name is covered when it sits inside such a range.
        private static readonly Regex Range = new Regex(@"`?([A-Za-z_][A-Za-z0-9_]*?)(\d+)`?\s*\.\.\s*`?(?:\1)?(\d+)`?");

        private static bool Named(string docs, string name, List<(string Prefix, int From, int To, int Width)> ranges)
        {
            if (Regex.IsMatch(docs, @"(?<![A-Za-z0-9_])" + Regex.Escape(name) + @"(?![A-Za-z0-9_])")) return true;

            Match m = Regex.Match(name, @"^(.*?)(\d+)$");
            if (!m.Success) return false;
            string prefix = m.Groups[1].Value;
            int n = int.Parse(m.Groups[2].Value);
            return ranges.Any(r => r.Prefix == prefix && n >= r.From && n <= r.To);
        }

        [Test]
        public void Every_original_object_script_room_and_font_is_named_in_the_docs()
        {
            if (!Directory.Exists(OriginalRoot)) Assert.Ignore("The original project is not on this machine.");

            string root = RepoRoot();
            string docs = File.ReadAllText(Path.Combine(root, "docs", "RECONSTRUCTION.md"))
                        + "\n" + File.ReadAllText(Path.Combine(root, "docs", "BIBLE.md"));

            var ranges = new List<(string, int, int, int)>();
            foreach (Match m in Range.Matches(docs))
            {
                ranges.Add((m.Groups[1].Value, int.Parse(m.Groups[2].Value), int.Parse(m.Groups[3].Value), m.Groups[2].Value.Length));
            }

            List<string> names = OriginalNames().ToList();
            Assert.That(names, Is.Not.Empty, "Read no objects, scripts, rooms or fonts from the original project.");

            List<string> missing = names.Where(n => !Named(docs, n, ranges)).OrderBy(n => n).ToList();
            Assert.That(missing, Is.Empty,
                "Original resources named nowhere in RECONSTRUCTION.md or BIBLE.md (record each as ported or not ported, with the reason): "
                + string.Join(", ", missing));
        }
    }
}
