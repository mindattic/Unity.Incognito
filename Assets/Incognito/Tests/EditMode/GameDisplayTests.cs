using Incognito.Display;
using NUnit.Framework;
using UnityEngine;

namespace Incognito.Tests
{
    /// <summary>
    /// U.INC-T32: GameDisplay applies Core's ScreenRatio to Unity: screen cameras get the
    /// letterboxed viewport, a render-texture camera keeps its whole texture, and screen points
    /// read back in view pixels through the same fit.
    /// </summary>
    public class GameDisplayTests
    {
        [Test]
        public void Screen_cameras_get_the_letterboxed_viewport_and_texture_cameras_the_whole_target()
        {
            var screenCam = new GameObject("screen cam").AddComponent<Camera>();
            var textureCam = new GameObject("texture cam").AddComponent<Camera>();
            var target = new RenderTexture(64, 64, 0);
            textureCam.targetTexture = target;
            try
            {
                screenCam.rect = new Rect(0.25f, 0.25f, 0.5f, 0.5f);
                textureCam.rect = new Rect(0.25f, 0.25f, 0.5f, 0.5f);
                GameDisplay.ApplyViewports();

                Rect2 n = GameDisplay.Current.NormalizedViewport;
                Assert.AreEqual(new Rect(n.x, n.y, n.width, n.height), screenCam.rect);
                Assert.AreEqual(new Rect(0f, 0f, 1f, 1f), textureCam.rect);
            }
            finally
            {
                textureCam.targetTexture = null;
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(screenCam.gameObject);
                Object.DestroyImmediate(textureCam.gameObject);
            }
        }

        [Test]
        public void The_view_centre_on_screen_reads_back_as_the_view_centre()
        {
            ScreenRatio r = GameDisplay.Current;
            if (r.ViewWidth == 0) Assert.Ignore("no screen in this run");

            Vector2 centre = new Vector2(r.ViewLeft + r.ViewWidth / 2f, Screen.height - (r.ViewTop + r.ViewHeight / 2f));
            Vector2 view = GameDisplay.ScreenToView(centre);
            Assert.AreEqual(960f, view.x, 1f);
            Assert.AreEqual(540f, view.y, 1f);
            Assert.IsTrue(GameDisplay.ViewRectContains(new Rect(950f, 530f, 20f, 20f), centre));
        }

        [Test]
        public void A_render_texture_target_is_fitted_on_its_own_size()
        {
            // A 1920x1200 target letterboxes 60px top and bottom: its point (0, 1140) from the
            // bottom is the view's top-left corner.
            Vector2 view = GameDisplay.ScreenToView(new Vector2(0f, 1140f), 1920, 1200);
            Assert.AreEqual(0f, view.x, 1e-3f);
            Assert.AreEqual(0f, view.y, 1e-3f);
        }

        [Test]
        public void Gui_rects_are_the_view_rect_scaled_and_offset_onto_the_screen()
        {
            ScreenRatio r = GameDisplay.Current;
            Rect gui = GameDisplay.ViewToGuiRect(new Rect(1784f, 984f, 96f, 96f));
            Assert.AreEqual(r.ViewLeft + 1784f * r.Scale, gui.x, 1e-2f);
            Assert.AreEqual(r.ViewTop + 984f * r.ViewHeight / 1080f, gui.y, 1e-2f);
            Assert.AreEqual(96f * r.Scale, gui.width, 1e-2f);
        }
    }
}
