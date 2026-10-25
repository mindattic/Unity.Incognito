// NUnit 4 moved the classic Assert; the tests were written against it (Unity Test Framework).
using Assert = NUnit.Framework.Legacy.ClassicAssert;
using NUnit.Framework.Legacy;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Incognito.Content;
using Incognito.Settings;
using NUnit.Framework;

namespace Incognito.Tests
{
    /// <summary>
    /// U.INC-T31: the original's three fonts are in the shipped game-content.json Fonts catalog at
    /// their face and point size, sized at the DPI their glyph atlases were rendered at, and (when
    /// the original project is on this machine) the transcription matches its fonts/*.font.gmx.
    /// </summary>
    public class OriginalFontParityTests
    {
        private const string OriginalRoot = @"R:\Projects\_PRE_2022\_PRE_2021\_PRE_2020\_PRE_2017\gmlIncognito\09-05-13_2.19am";

        private static readonly Dictionary<string, (string Face, float Points)> Original = new Dictionary<string, (string, float)>
        {
            { "fnt_basic", ("Arial", 12f) },
            { "fnt_large", ("High Tower Text", 16f) },
            { "fnt_system", ("Lucida Console", 8f) },
        };

        private static GameContent content;

        [OneTimeSetUp]
        public void LoadShippedContent()
        {
            var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "Assets", "Incognito", "Resources", "Data", "game-content.json")))
                dir = dir.Parent;
            Assert.IsNotNull(dir, "Could not find the repo root above the test directory.");
            content = GameContent.FromJson(File.ReadAllText(Path.Combine(dir.FullName, "Assets", "Incognito", "Resources", "Data", "game-content.json")));
        }

        [Test]
        public void The_catalog_is_exactly_the_originals_three_fonts()
        {
            CollectionAssert.AreEquivalent(Original.Keys, content.Fonts.Select(f => f.Id));
            CollectionAssert.AreEquivalent(GameFonts.All, Original.Keys);
            foreach (KeyValuePair<string, (string Face, float Points)> font in Original)
            {
                GameFontDefinition def = content.FindFont(font.Key);
                Assert.AreEqual(font.Value.Face, def.Face, font.Key + "'s face");
                Assert.AreEqual(font.Value.Points, def.PointSize, font.Key + "'s point size");
                Assert.IsFalse(def.Bold, font.Key);
                Assert.IsFalse(def.Italic, font.Key);
            }
        }

        [Test]
        public void Each_font_is_drawn_at_its_point_size_at_120_dpi()
        {
            Assert.AreEqual(20f, content.FindFont(GameFonts.Basic).PixelSize, 1e-4f);
            Assert.AreEqual(80f / 3f, content.FindFont(GameFonts.Large).PixelSize, 1e-4f);
            Assert.AreEqual(40f / 3f, content.FindFont(GameFonts.System).PixelSize, 1e-4f);
        }

        [Test]
        public void A_catalog_missing_a_HUD_font_or_repeating_one_is_rejected()
        {
            GameContent Catalog(params GameFontDefinition[] fonts) => new GameContent
            {
                Rooms = new[] { new RoomDefinition { Id = "R", DisplayName = "R", Width = 100f, BackgroundSlices = new[] { "bg" } } },
                Fonts = fonts,
            };
            GameFontDefinition Font(string id) => new GameFontDefinition { Id = id, Face = "Arial", PointSize = 12f, UsedBy = "test" };

            Assert.DoesNotThrow(() => Catalog().Validate(), "a fixture with no fonts at all is fine");
            Assert.DoesNotThrow(() => Catalog(Font("fnt_basic"), Font("fnt_large"), Font("fnt_system")).Validate());
            Assert.Throws<SettingsValidationException>(() => Catalog(Font("fnt_basic"), Font("fnt_large")).Validate());
            Assert.Throws<SettingsValidationException>(() =>
                Catalog(Font("fnt_basic"), Font("fnt_large"), Font("fnt_system"), Font("FNT_BASIC")).Validate());

            var noSize = Font("fnt_basic");
            noSize.PointSize = 0f;
            Assert.Throws<SettingsValidationException>(() => noSize.Validate());
            var noUse = Font("fnt_basic");
            noUse.UsedBy = "";
            Assert.Throws<SettingsValidationException>(() => noUse.Validate());
        }

        // ------------------------------------------------------------ diff against the original

        private static string FontsDirOrIgnore()
        {
            string dir = Path.Combine(OriginalRoot, "fonts");
            if (!Directory.Exists(dir))
                Assert.Ignore("The original 09-05-13 project is not on this machine (" + OriginalRoot + "); the transcription above stands in for it.");
            return dir;
        }

        private static XElement FontGmx(string id) => XDocument.Load(Path.Combine(FontsDirOrIgnore(), id + ".font.gmx")).Root;

        private static XElement Glyph(XElement font, char c) =>
            font.Element("glyphs").Elements("glyph").Single(g => (int)g.Attribute("character") == c);

        [Test]
        public void The_transcription_matches_every_font_gmx()
        {
            string dir = FontsDirOrIgnore();
            CollectionAssert.AreEquivalent(Original.Keys,
                Directory.GetFiles(dir, "*.font.gmx").Select(f => Path.GetFileName(f).Replace(".font.gmx", "")));

            foreach (GameFontDefinition def in content.Fonts)
            {
                XElement font = FontGmx(def.Id);
                Assert.AreEqual((string)font.Element("name"), def.Face, def.Id + "'s <name>");
                Assert.AreEqual((float)(int)font.Element("size"), def.PointSize, def.Id + "'s <size>");
                Assert.AreEqual((int)font.Element("bold") != 0, def.Bold, def.Id + "'s <bold>");
                Assert.AreEqual((int)font.Element("italic") != 0, def.Italic, def.Id + "'s <italic>");
            }
        }

        [Test]
        public void The_glyph_atlases_were_rendered_at_120_dpi()
        {
            // Arial's line (ascent 1854 + descent 434 of 2048 units, 1.117em) at fnt_basic's 20px is
            // 22.3px: GameMaker's glyph cells are 23px tall. At 96 DPI (16px) they would be 18.
            float basic = content.FindFont(GameFonts.Basic).PixelSize;
            Assert.AreEqual((int)Math.Ceiling(basic * (1854 + 434) / 2048f), (int)Glyph(FontGmx(GameFonts.Basic), ' ').Attribute("h"));

            // Lucida Console is monospaced at a 0.6em advance: 8px at fnt_system's 13.3px (6 at 96 DPI).
            float system = content.FindFont(GameFonts.System).PixelSize;
            foreach (char c in "AHg0 ")
                Assert.AreEqual((int)Math.Round(system * 0.6f), (int)Glyph(FontGmx(GameFonts.System), c).Attribute("shift"), "'" + c + "' advance");
        }
    }
}
