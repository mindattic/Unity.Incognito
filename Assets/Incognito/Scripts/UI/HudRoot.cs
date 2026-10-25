using Incognito.Display;
using Incognito.Settings;
using UnityEngine;
using UnityEngine.UIElements;

namespace Incognito.UI
{
    /// <summary>
    /// The single runtime UI Toolkit panel every former IMGUI (OnGUI) class draws into (U.INC-T26):
    /// <see cref="World.Room"/>'s name, <see cref="World.DoorExit"/>'s label,
    /// <see cref="Typing.DescriptionTypewriter"/>, <see cref="Terminal.CommandPrompt"/>,
    /// <see cref="Hacking.HackingOverlay"/>, <see cref="World.RoomFade"/> and
    /// <see cref="World.DebugHotkeys"/>'s readout, plus (U.INC-T31) <see cref="Npc.NpcController"/>'s
    /// suspicion and <see cref="FloatText"/>, all styled by <see cref="OutlinedText"/>. Nothing else in the project needs its own
    /// PanelSettings asset or UIDocument — no Inspector wiring, just this one code-built panel.
    ///
    /// The panel's reference resolution is the original's 1920x1080 view, fitted to the screen
    /// keeping its aspect (U.INC-T32, obj_startup's screen ratio: Shrink, the same fit
    /// <see cref="GameDisplay"/> letterboxes the camera with), and every layer sits in a 1920x1080
    /// stage centred on the picture, so every element below can be laid out directly in original
    /// GameMaker view pixels and the panel does the scaling and the bars.
    ///
    /// Layers are fixed child containers, added back to front, mirroring the original's GUI.depth
    /// ordering (a lower depth drew on top): the hacking backdrop (obj_gui_console, depth 10) is
    /// behind everything; then the room's floating world labels and the console panel (undepthed,
    /// depth 0); then the room name and debug readout; then the fade (obj_fade, depth -100) on top
    /// of all of it but the HUD pistol icon (obj_icon_pistol, depth -101, U.INC-T30). <see cref="EnsureInstalled"/> is safe to call from every consumer's own
    /// Awake/OnEnable — whichever runs first builds the panel once.
    /// </summary>
    public static class HudRoot
    {
        // A UnityEngine.Object reference, not a bool: with domain reload disabled on Enter Play
        // Mode, static fields survive from the previous session but the "Hud Root" GameObject they
        // pointed at was destroyed when that session stopped, so a bool guard would wrongly skip
        // rebuilding and every layer property would point at a dead panel. Unity's overridden
        // null-check on a destroyed object still reports true here, so this stays correct.
        private static UIDocument document;

        public static VisualElement HackingBackdrop { get; private set; }
        public static VisualElement WorldLabels { get; private set; }
        public static VisualElement ConsolePanel { get; private set; }
        public static VisualElement RoomInfo { get; private set; }
        public static VisualElement Fade { get; private set; }
        /// <summary>The HUD pistol icon (obj_icon_pistol, depth -101): one depth above obj_fade, so
        /// it stays drawn over a room fade (U.INC-T30).</summary>
        public static VisualElement HudIcons { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void EnsureInstalled()
        {
            if (document != null) return;

            var host = new GameObject("Hud Root");
            Object.DontDestroyOnLoad(host);

            var panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panelSettings.referenceResolution = new Vector2Int(Mathf.RoundToInt(ReferenceWidth), Mathf.RoundToInt(ReferenceHeight));
            // U.INC-T32: Shrink is "keep aspect ratio" — the largest scale at which the whole
            // 1920x1080 reference fits — the same fit Display.ScreenRatio gives the letterboxed
            // camera viewport, so the stage below lands exactly on the picture.
            panelSettings.screenMatchMode = PanelScreenMatchMode.Shrink;
            panelSettings.sortingOrder = 100f;

            document = host.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;

            VisualElement panelRoot = document.rootVisualElement;
            panelRoot.pickingMode = PickingMode.Ignore;
            // Fill the panel explicitly: the stage below is absolute, so it gives the document root
            // no height of its own, and its 50% top would otherwise resolve against zero.
            panelRoot.style.position = Position.Absolute;
            panelRoot.style.left = 0f;
            panelRoot.style.top = 0f;
            panelRoot.style.right = 0f;
            panelRoot.style.bottom = 0f;

            // The stage: exactly the reference view, centred in the panel (whose leftover axis is
            // the letterbox or pillarbox bars) and clipped to it, so every layer is laid out in
            // original view pixels from the view's own top-left, never the screen's.
            var root = new VisualElement { name = "view", pickingMode = PickingMode.Ignore };
            root.style.position = Position.Absolute;
            root.style.width = ReferenceWidth;
            root.style.height = ReferenceHeight;
            root.style.left = Length.Percent(50f);
            root.style.top = Length.Percent(50f);
            root.style.marginLeft = -ReferenceWidth / 2f;
            root.style.marginTop = -ReferenceHeight / 2f;
            root.style.overflow = Overflow.Hidden;
            panelRoot.Add(root);
            // No themeStyleSheet is assigned (there is no committed theme asset to point at), so
            // every element needs its own explicit font: UI Toolkit's runtime panel otherwise has
            // none to fall back on and text silently fails to render. Font is an inherited style,
            // so setting it once here covers every label this file's consumers create. It is the
            // one face for all three original fonts; each label sets its own size
            // (OutlinedText.SetFont, U.INC-T31).
            root.style.unityFontDefinition = FontDefinition.FromFont(Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

            HackingBackdrop = AddLayer(root, "hacking-backdrop");
            WorldLabels = AddLayer(root, "world-labels");
            ConsolePanel = AddLayer(root, "console-panel");
            RoomInfo = AddLayer(root, "room-info");
            Fade = AddLayer(root, "fade");
            HudIcons = AddLayer(root, "hud-icons");
        }

        /// <summary>
        /// Where <paramref name="world"/> is on the panel, in reference pixels from its top-left (the
        /// original's GUI space): for labels anchored to something in the room (a door's name, a
        /// typed description, an NPC's suspicion, floating text). False when there is no camera or
        /// no panel yet.
        /// </summary>
        public static bool TryWorldToPanel(Camera camera, Vector3 world, out Vector2 panelPosition)
        {
            panelPosition = default;
            if (camera == null || document == null) return false;
            IPanel panel = document.rootVisualElement.panel;
            if (panel == null) return false;

            // WorldToScreenPoint honours the camera's letterboxed viewport; GameDisplay maps that
            // screen point into view pixels, which is the stage's own space (U.INC-T32).
            Vector3 screen = camera.WorldToScreenPoint(world);
            RenderTexture target = camera.targetTexture;
            panelPosition = target != null
                ? GameDisplay.ScreenToView(new Vector2(screen.x, screen.y), target.width, target.height)
                : GameDisplay.ScreenToView(new Vector2(screen.x, screen.y));
            return true;
        }

        private static float ReferenceWidth => GameSettings.Tuning.Display.ReferenceWidth;
        private static float ReferenceHeight => GameSettings.Tuning.Display.ReferenceHeight;

        private static VisualElement AddLayer(VisualElement root, string name)
        {
            var layer = new VisualElement { name = name, pickingMode = PickingMode.Ignore };
            layer.style.position = Position.Absolute;
            layer.style.left = 0f;
            layer.style.top = 0f;
            layer.style.right = 0f;
            layer.style.bottom = 0f;
            root.Add(layer);
            return layer;
        }
    }
}
