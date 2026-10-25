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
    /// The panel's reference resolution is the original's 1920x1080 view, matched purely by screen
    /// height (match = 1): the same <c>Screen.height / 1080f</c> scale every IMGUI class used to
    /// compute by hand, so every element below can be laid out directly in original GameMaker pixels
    /// and the panel does the scaling.
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
            panelSettings.referenceResolution = new Vector2Int(1920, 1080);
            panelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panelSettings.match = 1f; // height-only match, as every OnGUI class's Screen.height/1080f scale did
            panelSettings.sortingOrder = 100f;

            document = host.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;

            VisualElement root = document.rootVisualElement;
            root.pickingMode = PickingMode.Ignore;
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

            Vector3 screen = camera.WorldToScreenPoint(world);
            // Top-left origin, like the OS screen point every other HUD element's hit-testing uses.
            panelPosition = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(screen.x, Screen.height - screen.y));
            return true;
        }

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
