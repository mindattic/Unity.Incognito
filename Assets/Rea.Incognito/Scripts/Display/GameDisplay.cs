using Incognito.Controls;
using Incognito.Settings;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Incognito.Display
{
    /// <summary>
    /// obj_startup's screen-ratio setup and the game's display handling (U.INC-T32), applied with
    /// <see cref="ScreenRatio"/>'s maths. The 1920x1080 reference view keeps its aspect on any
    /// screen (the config's <c>option_scale = -1</c>): every camera that draws to the screen gets the
    /// letterboxed viewport, and a camera that sees nothing clears the bars to black behind it. The
    /// HUD panel (<see cref="UI.HudRoot"/>) draws into the same rect, and every screen-space reading
    /// (the pointer's height for the eye, the view pan and the gun's aim, the HUD click rects) goes
    /// through <see cref="ScreenToView"/> so it reads original view pixels whatever the window.
    ///
    /// Fullscreen: obj_startup and obj_titlescreen both run <c>window_set_fullscreen(true)</c> on
    /// Create (<see cref="EnterFullscreen"/>, from <see cref="World.TitleScreen"/>), and F1 on the
    /// title, in obj_game and in obj_debug runs <c>window_set_fullscreen(!window_get_fullscreen())</c>
    /// (<see cref="ToggleFullscreen"/>, from <see cref="World.DebugHotkeys"/>). Fullscreen is a
    /// borderless window at the display's own resolution with the view letterboxed in it; windowed is
    /// <see cref="ScreenRatio.WindowedSize"/> (1920x1080, or the largest 16:9 that fits a smaller
    /// display). Installed once, before the first scene loads, and kept across scene changes.
    /// </summary>
    [DefaultExecutionOrder(-2000)]
    public sealed class GameDisplay : MonoBehaviour
    {
        private static GameDisplay instance;
        private Camera barsCamera;

        /// <summary>The reference view fitted to the current screen.</summary>
        public static ScreenRatio Current
        {
            get
            {
                DisplayTuning tuning = GameSettings.Tuning.Display;
                return ScreenRatio.Fit(Screen.width, Screen.height, tuning.ReferenceWidth, tuning.ReferenceHeight);
            }
        }

        /// <summary>The reference view's size in original pixels (1920x1080).</summary>
        public static Vector2 ReferenceSize
        {
            get
            {
                DisplayTuning tuning = GameSettings.Tuning.Display;
                return new Vector2(tuning.ReferenceWidth, tuning.ReferenceHeight);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void EnsureInstalled()
        {
            if (instance != null) return;
            var host = new GameObject("Game Display");
            DontDestroyOnLoad(host);
            instance = host.AddComponent<GameDisplay>();
        }

        /// <summary>
        /// A screen point as Unity reports it (pixels from the bottom-left, e.g. the pointer) in
        /// original view pixels from the view's top-left (GameMaker's mouse/GUI orientation). On a
        /// bar it lands outside 0..1920 / 0..1080.
        /// </summary>
        public static Vector2 ScreenToView(Vector2 screenPoint) => ScreenToView(screenPoint, Screen.width, Screen.height);

        /// <summary><see cref="ScreenToView(Vector2)"/> on a target of the given size (a render
        /// texture rather than the screen).</summary>
        public static Vector2 ScreenToView(Vector2 screenPoint, int targetWidth, int targetHeight)
        {
            DisplayTuning tuning = GameSettings.Tuning.Display;
            ScreenRatio ratio = ScreenRatio.Fit(targetWidth, targetHeight, tuning.ReferenceWidth, tuning.ReferenceHeight);
            Vec2 v = ratio.ScreenToView(new Vec2(screenPoint.x, targetHeight - screenPoint.y));
            return new Vector2(v.x, v.y);
        }

        /// <summary>The pointer in view pixels, held to the view's edges (so the bars read as the
        /// nearest edge of the picture, never past it).</summary>
        public static Vector2 PointerInView
        {
            get
            {
                Vector2 p = GameInput.PointerScreenPosition;
                Vec2 v = Current.ScreenToViewClamped(new Vec2(p.x, Screen.height - p.y));
                return new Vector2(v.x, v.y);
            }
        }

        /// <summary>A rect in view pixels on screen, in GUI coordinates (top-left origin).</summary>
        public static Rect ViewToGuiRect(Rect view)
        {
            Rect2 r = Current.ViewToScreen(new Rect2(view.x, view.y, view.width, view.height));
            return new Rect(r.x, r.y, r.width, r.height);
        }

        /// <summary>Whether a screen point (bottom-left origin, e.g. the pointer) is inside a rect
        /// given in view pixels.</summary>
        public static bool ViewRectContains(Rect view, Vector2 screenPoint)
        {
            return view.Contains(ScreenToView(screenPoint));
        }

        /// <summary>obj_startup / obj_titlescreen's Create: <c>window_set_fullscreen(true)</c>.</summary>
        public static void EnterFullscreen()
        {
            if (!GameSettings.Tuning.Display.StartFullscreen || Application.isEditor) return;
            SetFullscreen(true);
        }

        /// <summary>F1: <c>window_set_fullscreen(!window_get_fullscreen())</c>.</summary>
        public static void ToggleFullscreen()
        {
            if (Application.isEditor) return;
            SetFullscreen(!Screen.fullScreen);
        }

        private static void SetFullscreen(bool fullscreen)
        {
            int displayWidth = UnityEngine.Display.main.systemWidth;
            int displayHeight = UnityEngine.Display.main.systemHeight;
            if (fullscreen)
            {
                Screen.SetResolution(displayWidth, displayHeight, FullScreenMode.FullScreenWindow);
                return;
            }

            Vector2 reference = ReferenceSize;
            Vec2Int window = ScreenRatio.WindowedSize(displayWidth, displayHeight, reference.x, reference.y);
            Screen.SetResolution(window.x, window.y, FullScreenMode.Windowed);
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;

            // The bars: a camera that renders nothing, under every other, clearing the whole screen
            // to black so the area outside the letterboxed view is the original's black border.
            barsCamera = gameObject.AddComponent<Camera>();
            barsCamera.clearFlags = CameraClearFlags.SolidColor;
            barsCamera.backgroundColor = Color.black;
            barsCamera.cullingMask = 0;
            barsCamera.depth = -100f;
            barsCamera.orthographic = true;
            barsCamera.useOcclusionCulling = false;

            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (instance != this) return;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            instance = null;
        }

        // A room's camera is built in its Awake (RoomBootstrap), before any Start reads its aspect,
        // so the viewport is set here first, then again every frame for a resized window.
        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => ApplyViewports();

        private void Update() => ApplyViewports();

        /// <summary>Gives every camera that draws to the screen the letterboxed viewport; a camera
        /// rendering into a texture keeps the whole texture.</summary>
        public static void ApplyViewports()
        {
            Camera bars = instance != null ? instance.barsCamera : null;
            Rect2 n = Current.NormalizedViewport;
            var viewport = new Rect(n.x, n.y, n.width, n.height);
            foreach (Camera cam in Camera.allCameras)
            {
                if (bars != null && cam == bars) continue;
                Rect wanted = cam.targetTexture == null ? viewport : new Rect(0f, 0f, 1f, 1f);
                if (cam.rect != wanted) cam.rect = wanted;
            }
        }
    }
}
