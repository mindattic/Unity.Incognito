using System;

namespace Incognito.Display
{
    /// <summary>
    /// obj_startup's "Setup Screen Ratio" (U.INC-T32), as pure maths. Every room's view 0 is a
    /// 1920x1080 view drawn into a port the size of the window (<c>view_wport = window_get_width()</c>,
    /// <c>view_hport = window_get_height()</c>), and the game's <c>option_scale = -1</c> ("keep aspect
    /// ratio") draws that port onto the screen at the largest uniform scale that fits, centred, with
    /// black bars on whichever axis is left over: letterboxed (bars above and below) on a screen
    /// taller than 16:9, pillarboxed (bars left and right) on a wider one, and exactly full screen
    /// at 16:9.
    ///
    /// Screen coordinates here are pixels from the screen's top-left (GameMaker's and the GUI's
    /// orientation); view coordinates are the original's room/GUI pixels on the reference view
    /// (0..1920 across, 0..1080 down). The view's on-screen rect is snapped to whole pixels, as the
    /// camera viewport that shows it is, so the maths and the picture agree to the pixel.
    /// </summary>
    public readonly struct ScreenRatio
    {
        public readonly int ScreenWidth;
        public readonly int ScreenHeight;
        public readonly float ReferenceWidth;
        public readonly float ReferenceHeight;

        /// <summary>The view's left edge on screen, in pixels (the left bar's width).</summary>
        public readonly int ViewLeft;

        /// <summary>The view's top edge on screen, in pixels (the top bar's height).</summary>
        public readonly int ViewTop;

        /// <summary>The view's width on screen, in pixels.</summary>
        public readonly int ViewWidth;

        /// <summary>The view's height on screen, in pixels.</summary>
        public readonly int ViewHeight;

        private ScreenRatio(int screenWidth, int screenHeight, float referenceWidth, float referenceHeight,
            int viewLeft, int viewTop, int viewWidth, int viewHeight)
        {
            ScreenWidth = screenWidth;
            ScreenHeight = screenHeight;
            ReferenceWidth = referenceWidth;
            ReferenceHeight = referenceHeight;
            ViewLeft = viewLeft;
            ViewTop = viewTop;
            ViewWidth = viewWidth;
            ViewHeight = viewHeight;
        }

        /// <summary>
        /// The reference view fitted into a <paramref name="screenWidth"/> x <paramref name="screenHeight"/>
        /// screen, keeping its aspect ratio and centring it. A screen with no area gets an empty view.
        /// </summary>
        public static ScreenRatio Fit(int screenWidth, int screenHeight, float referenceWidth = 1920f, float referenceHeight = 1080f)
        {
            if (!(referenceWidth > 0f) || !(referenceHeight > 0f))
                throw new ArgumentOutOfRangeException(nameof(referenceWidth), "The reference view must have a positive size.");

            screenWidth = Math.Max(0, screenWidth);
            screenHeight = Math.Max(0, screenHeight);
            if (screenWidth == 0 || screenHeight == 0)
                return new ScreenRatio(screenWidth, screenHeight, referenceWidth, referenceHeight, 0, 0, 0, 0);

            float scale = Math.Min(screenWidth / referenceWidth, screenHeight / referenceHeight);
            // The constrained axis fills the screen exactly; the other is rounded to whole pixels.
            int width = Math.Min(screenWidth, (int)Math.Round(referenceWidth * scale));
            int height = Math.Min(screenHeight, (int)Math.Round(referenceHeight * scale));
            return new ScreenRatio(screenWidth, screenHeight, referenceWidth, referenceHeight,
                (screenWidth - width) / 2, (screenHeight - height) / 2, width, height);
        }

        /// <summary>Screen pixels per view pixel (uniform: the view keeps its aspect ratio).</summary>
        public float Scale => ViewWidth > 0 ? ViewWidth / ReferenceWidth : 0f;

        /// <summary>Bars above and below: the screen is taller than the view's aspect.</summary>
        public bool IsLetterboxed => ViewHeight < ScreenHeight;

        /// <summary>Bars left and right: the screen is wider than the view's aspect.</summary>
        public bool IsPillarboxed => ViewWidth < ScreenWidth;

        /// <summary>
        /// The view's rect as fractions of the screen, measured from the bottom-left (what a camera
        /// viewport takes). The bars are symmetric to within a pixel, so y uses the bottom bar.
        /// </summary>
        public Rect2 NormalizedViewport
        {
            get
            {
                if (ScreenWidth == 0 || ScreenHeight == 0) return new Rect2(0f, 0f, 1f, 1f);
                int bottom = ScreenHeight - ViewTop - ViewHeight;
                return new Rect2((float)ViewLeft / ScreenWidth, (float)bottom / ScreenHeight,
                    (float)ViewWidth / ScreenWidth, (float)ViewHeight / ScreenHeight);
            }
        }

        /// <summary>A screen point (top-left origin) in view pixels; outside the view (on a bar) it
        /// lands outside 0..reference, as GameMaker's mouse_x/mouse_y do past the port.</summary>
        public Vec2 ScreenToView(Vec2 screen)
        {
            if (ViewWidth == 0 || ViewHeight == 0) return Vec2.Zero;
            return new Vec2((screen.x - ViewLeft) * ReferenceWidth / ViewWidth,
                (screen.y - ViewTop) * ReferenceHeight / ViewHeight);
        }

        /// <summary><see cref="ScreenToView"/> held to the view's own edges.</summary>
        public Vec2 ScreenToViewClamped(Vec2 screen)
        {
            Vec2 v = ScreenToView(screen);
            return new Vec2(Clamp(v.x, 0f, ReferenceWidth), Clamp(v.y, 0f, ReferenceHeight));
        }

        /// <summary>A view point back on screen (top-left origin).</summary>
        public Vec2 ViewToScreen(Vec2 view)
        {
            return new Vec2(ViewLeft + view.x * ViewWidth / ReferenceWidth,
                ViewTop + view.y * ViewHeight / ReferenceHeight);
        }

        /// <summary>A rect in view pixels as a rect on screen (both top-left origin).</summary>
        public Rect2 ViewToScreen(Rect2 view)
        {
            Vec2 min = ViewToScreen(new Vec2(view.x, view.y));
            float sx = ReferenceWidth > 0f ? ViewWidth / ReferenceWidth : 0f;
            float sy = ReferenceHeight > 0f ? ViewHeight / ReferenceHeight : 0f;
            return new Rect2(min.x, min.y, view.width * sx, view.height * sy);
        }

        /// <summary>Whether a screen point (top-left origin) is on the view rather than a bar.</summary>
        public bool ContainsScreenPoint(Vec2 screen)
        {
            return screen.x >= ViewLeft && screen.x < ViewLeft + ViewWidth
                && screen.y >= ViewTop && screen.y < ViewTop + ViewHeight;
        }

        /// <summary>
        /// The window's client size when not fullscreen. GameMaker opens the window at the first
        /// room's port size (1920x1080) and the config has <c>option_sizeable</c> False, so it is that
        /// size; on a display too small to hold it, it is the largest whole-pixel window with the
        /// same aspect that fits.
        /// </summary>
        public static Vec2Int WindowedSize(int displayWidth, int displayHeight, float referenceWidth = 1920f, float referenceHeight = 1080f)
        {
            if (!(referenceWidth > 0f) || !(referenceHeight > 0f))
                throw new ArgumentOutOfRangeException(nameof(referenceWidth), "The reference view must have a positive size.");

            int fullWidth = (int)Math.Round(referenceWidth);
            int fullHeight = (int)Math.Round(referenceHeight);
            if (displayWidth <= 0 || displayHeight <= 0) return new Vec2Int(fullWidth, fullHeight);

            float scale = Math.Min(1f, Math.Min(displayWidth / referenceWidth, displayHeight / referenceHeight));
            return new Vec2Int(Math.Min(displayWidth, (int)Math.Round(referenceWidth * scale)),
                Math.Min(displayHeight, (int)Math.Round(referenceHeight * scale)));
        }

        private static float Clamp(float v, float min, float max) => v < min ? min : v > max ? max : v;
    }
}
