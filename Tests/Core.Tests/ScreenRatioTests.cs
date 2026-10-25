// NUnit 4 moved the classic Assert; the tests were written against it (Unity Test Framework).
using Assert = NUnit.Framework.Legacy.ClassicAssert;
using System;
using Incognito.Display;
using Incognito.Settings;
using NUnit.Framework;

namespace Incognito.Tests
{
    /// <summary>
    /// U.INC-T32: obj_startup's screen-ratio setup. The 1920x1080 view is fitted to the screen at
    /// the largest uniform scale (the config's option_scale = -1, "keep aspect ratio"), centred,
    /// with bars on the leftover axis; screen and view points convert both ways through it.
    /// </summary>
    public class ScreenRatioTests
    {
        [Test]
        public void At_the_reference_resolution_the_view_fills_the_screen_one_to_one()
        {
            var r = ScreenRatio.Fit(1920, 1080);
            Assert.AreEqual(0, r.ViewLeft);
            Assert.AreEqual(0, r.ViewTop);
            Assert.AreEqual(1920, r.ViewWidth);
            Assert.AreEqual(1080, r.ViewHeight);
            Assert.AreEqual(1f, r.Scale);
            Assert.IsFalse(r.IsLetterboxed);
            Assert.IsFalse(r.IsPillarboxed);
        }

        [TestCase(1280, 720, 2f / 3f)]
        [TestCase(2560, 1440, 4f / 3f)]
        [TestCase(3840, 2160, 2f)]
        public void Other_sixteen_by_nine_screens_scale_without_bars(int w, int h, float scale)
        {
            var r = ScreenRatio.Fit(w, h);
            Assert.AreEqual(w, r.ViewWidth);
            Assert.AreEqual(h, r.ViewHeight);
            Assert.AreEqual(scale, r.Scale, 1e-6f);
            Assert.IsFalse(r.IsLetterboxed || r.IsPillarboxed);
        }

        [Test]
        public void A_taller_screen_is_letterboxed_with_equal_bars_above_and_below()
        {
            // 16:10: 1920 wide fits exactly, the view is 1080 tall, 60px bars top and bottom.
            var r = ScreenRatio.Fit(1920, 1200);
            Assert.IsTrue(r.IsLetterboxed);
            Assert.IsFalse(r.IsPillarboxed);
            Assert.AreEqual(0, r.ViewLeft);
            Assert.AreEqual(60, r.ViewTop);
            Assert.AreEqual(1920, r.ViewWidth);
            Assert.AreEqual(1080, r.ViewHeight);

            // 4:3 at 1024x768: scale 1024/1920, view 1024x576, 96px bars.
            r = ScreenRatio.Fit(1024, 768);
            Assert.AreEqual(1024, r.ViewWidth);
            Assert.AreEqual(576, r.ViewHeight);
            Assert.AreEqual(96, r.ViewTop);
            Assert.AreEqual(1024f / 1920f, r.Scale, 1e-6f);
        }

        [Test]
        public void A_wider_screen_is_pillarboxed_with_equal_bars_left_and_right()
        {
            // 21:9 at 2560x1080: the view is 1920x1080, 320px bars either side.
            var r = ScreenRatio.Fit(2560, 1080);
            Assert.IsTrue(r.IsPillarboxed);
            Assert.IsFalse(r.IsLetterboxed);
            Assert.AreEqual(320, r.ViewLeft);
            Assert.AreEqual(0, r.ViewTop);
            Assert.AreEqual(1920, r.ViewWidth);
            Assert.AreEqual(1080, r.ViewHeight);
            Assert.AreEqual(1f, r.Scale);

            // 32:9 at 5120x1440: scale 4/3, view 2560x1440, 1280px bars.
            r = ScreenRatio.Fit(5120, 1440);
            Assert.AreEqual(1280, r.ViewLeft);
            Assert.AreEqual(2560, r.ViewWidth);
            Assert.AreEqual(1440, r.ViewHeight);
        }

        [Test]
        public void The_view_rect_is_whole_pixels_and_stays_inside_the_screen()
        {
            for (int w = 1; w <= 400; w += 7)
            for (int h = 1; h <= 400; h += 11)
            {
                var r = ScreenRatio.Fit(w, h);
                Assert.GreaterOrEqual(r.ViewLeft, 0);
                Assert.GreaterOrEqual(r.ViewTop, 0);
                Assert.LessOrEqual(r.ViewLeft + r.ViewWidth, w);
                Assert.LessOrEqual(r.ViewTop + r.ViewHeight, h);
                // One axis always fills the screen; the bars differ by at most a pixel.
                Assert.IsTrue(r.ViewWidth == w || r.ViewHeight == h, $"{w}x{h}");
                Assert.LessOrEqual(Math.Abs((w - r.ViewWidth - r.ViewLeft) - r.ViewLeft), 1);
                Assert.LessOrEqual(Math.Abs((h - r.ViewHeight - r.ViewTop) - r.ViewTop), 1);
            }
        }

        [Test]
        public void The_camera_viewport_is_the_view_rect_from_the_bottom_left()
        {
            Rect2 n = ScreenRatio.Fit(1920, 1200).NormalizedViewport;
            Assert.AreEqual(0f, n.x, 1e-6f);
            Assert.AreEqual(60f / 1200f, n.y, 1e-6f);
            Assert.AreEqual(1f, n.width, 1e-6f);
            Assert.AreEqual(1080f / 1200f, n.height, 1e-6f);

            n = ScreenRatio.Fit(2560, 1080).NormalizedViewport;
            Assert.AreEqual(320f / 2560f, n.x, 1e-6f);
            Assert.AreEqual(0f, n.y, 1e-6f);
            Assert.AreEqual(1920f / 2560f, n.width, 1e-6f);
            Assert.AreEqual(1f, n.height, 1e-6f);

            n = ScreenRatio.Fit(0, 0).NormalizedViewport;
            Assert.AreEqual(1f, n.width);
            Assert.AreEqual(1f, n.height);
        }

        [Test]
        public void Screen_points_map_to_view_pixels_through_the_bars()
        {
            // 1024x768 (4:3): the view sits at y 96..672, scale 1024/1920.
            var r = ScreenRatio.Fit(1024, 768);
            Vec2 topLeft = r.ScreenToView(new Vec2(0f, 96f));
            Assert.AreEqual(0f, topLeft.x, 1e-3f);
            Assert.AreEqual(0f, topLeft.y, 1e-3f);

            Vec2 centre = r.ScreenToView(new Vec2(512f, 384f));
            Assert.AreEqual(960f, centre.x, 1e-3f);
            Assert.AreEqual(540f, centre.y, 1e-3f);

            Vec2 bottomRight = r.ScreenToView(new Vec2(1024f, 672f));
            Assert.AreEqual(1920f, bottomRight.x, 1e-3f);
            Assert.AreEqual(1080f, bottomRight.y, 1e-3f);

            // On the top bar the point is above the view (negative y), as GM's mouse_y past the port.
            Assert.Less(r.ScreenToView(new Vec2(10f, 10f)).y, 0f);
            Assert.IsFalse(r.ContainsScreenPoint(new Vec2(10f, 10f)));
            Assert.IsTrue(r.ContainsScreenPoint(new Vec2(10f, 100f)));

            Vec2 held = r.ScreenToViewClamped(new Vec2(10f, 10f));
            Assert.AreEqual(0f, held.y);
            held = r.ScreenToViewClamped(new Vec2(10f, 760f));
            Assert.AreEqual(1080f, held.y);
        }

        [Test]
        public void View_to_screen_is_the_inverse_of_screen_to_view()
        {
            foreach (var r in new[] { ScreenRatio.Fit(1920, 1080), ScreenRatio.Fit(1024, 768), ScreenRatio.Fit(3440, 1440), ScreenRatio.Fit(800, 1280) })
            {
                foreach (var p in new[] { new Vec2(0f, 0f), new Vec2(960f, 540f), new Vec2(1784f, 984f), new Vec2(1920f, 1080f) })
                {
                    Vec2 back = r.ScreenToView(r.ViewToScreen(p));
                    Assert.AreEqual(p.x, back.x, 1e-2f);
                    Assert.AreEqual(p.y, back.y, 1e-2f);
                }
            }
        }

        [Test]
        public void A_view_rect_lands_on_screen_scaled_and_offset_by_the_bars()
        {
            // The HUD pistol icon (96x96 at x 1784, y 984) on a 21:9 2560x1080 screen: shifted
            // right by the 320px left bar, not pinned to the screen's own right edge.
            Rect2 icon = ScreenRatio.Fit(2560, 1080).ViewToScreen(new Rect2(1784f, 984f, 96f, 96f));
            Assert.AreEqual(2104f, icon.x, 1e-3f);
            Assert.AreEqual(984f, icon.y, 1e-3f);
            Assert.AreEqual(96f, icon.width, 1e-3f);

            // Halved on a 960x540 window.
            icon = ScreenRatio.Fit(960, 540).ViewToScreen(new Rect2(1784f, 984f, 96f, 96f));
            Assert.AreEqual(892f, icon.x, 1e-3f);
            Assert.AreEqual(492f, icon.y, 1e-3f);
            Assert.AreEqual(48f, icon.height, 1e-3f);
        }

        [Test]
        public void An_empty_screen_has_an_empty_view_and_maps_to_the_origin()
        {
            var r = ScreenRatio.Fit(0, 0);
            Assert.AreEqual(0, r.ViewWidth);
            Assert.AreEqual(0f, r.Scale);
            Vec2 v = r.ScreenToView(new Vec2(5f, 5f));
            Assert.AreEqual(0f, v.x);
            Assert.AreEqual(0f, v.y);
        }

        [Test]
        public void A_non_positive_reference_is_rejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ScreenRatio.Fit(1920, 1080, 0f, 1080f));
            Assert.Throws<ArgumentOutOfRangeException>(() => ScreenRatio.WindowedSize(1920, 1080, 1920f, -1f));
        }

        [Test]
        public void The_window_is_the_reference_size_when_the_display_holds_it()
        {
            Vec2Int size = ScreenRatio.WindowedSize(2560, 1440);
            Assert.AreEqual(1920, size.x);
            Assert.AreEqual(1080, size.y);

            size = ScreenRatio.WindowedSize(1920, 1080);
            Assert.AreEqual(1920, size.x);
            Assert.AreEqual(1080, size.y);
        }

        [Test]
        public void On_a_smaller_display_the_window_shrinks_keeping_its_aspect()
        {
            Vec2Int size = ScreenRatio.WindowedSize(1366, 768);
            Assert.AreEqual(1365, size.x);
            Assert.AreEqual(768, size.y);

            size = ScreenRatio.WindowedSize(1280, 1024);
            Assert.AreEqual(1280, size.x);
            Assert.AreEqual(720, size.y);

            // Unknown display: the reference size.
            size = ScreenRatio.WindowedSize(0, 0);
            Assert.AreEqual(1920, size.x);
            Assert.AreEqual(1080, size.y);
        }

        [Test]
        public void The_display_tuning_defaults_are_the_originals()
        {
            var tuning = new DisplayTuning();
            Assert.AreEqual(1920f, tuning.ReferenceWidth);
            Assert.AreEqual(1080f, tuning.ReferenceHeight);
            Assert.IsTrue(tuning.StartFullscreen, "obj_startup/obj_titlescreen: window_set_fullscreen(true)");
            Assert.DoesNotThrow(() => tuning.Validate());
            Assert.Throws<SettingsValidationException>(() => new DisplayTuning { ReferenceWidth = 0f }.Validate());
            Assert.Throws<SettingsValidationException>(() => new DisplayTuning { ReferenceHeight = float.NaN }.Validate());
        }
    }
}
