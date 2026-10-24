using UnityEngine;
using UnityEngine.UIElements;

namespace Incognito.UI
{
    /// <summary>
    /// The single runtime UI Toolkit panel every former IMGUI (OnGUI) class draws into (U.INC-T26):
    /// <see cref="World.Room"/>'s name, <see cref="World.DoorExit"/>'s label,
    /// <see cref="Typing.DescriptionTypewriter"/>, <see cref="Terminal.CommandPrompt"/>,
    /// <see cref="Hacking.HackingOverlay"/>, <see cref="World.RoomFade"/> and
    /// <see cref="World.DebugHotkeys"/>'s readout. Nothing else in the project needs its own
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
    /// of all of it. <see cref="EnsureInstalled"/> is safe to call from every consumer's own
    /// Awake/OnEnable — whichever runs first builds the panel once.
    /// </summary>
    public static class HudRoot
    {
        private static bool installed;

        public static VisualElement HackingBackdrop { get; private set; }
        public static VisualElement WorldLabels { get; private set; }
        public static VisualElement ConsolePanel { get; private set; }
        public static VisualElement RoomInfo { get; private set; }
        public static VisualElement Fade { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void EnsureInstalled()
        {
            if (installed) return;
            installed = true;

            var host = new GameObject("Hud Root");
            Object.DontDestroyOnLoad(host);

            var panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panelSettings.referenceResolution = new Vector2Int(1920, 1080);
            panelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panelSettings.match = 1f; // height-only match, as every OnGUI class's Screen.height/1080f scale did
            panelSettings.sortingOrder = 100f;

            var document = host.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;

            VisualElement root = document.rootVisualElement;
            root.pickingMode = PickingMode.Ignore;

            HackingBackdrop = AddLayer(root, "hacking-backdrop");
            WorldLabels = AddLayer(root, "world-labels");
            ConsolePanel = AddLayer(root, "console-panel");
            RoomInfo = AddLayer(root, "room-info");
            Fade = AddLayer(root, "fade");
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
