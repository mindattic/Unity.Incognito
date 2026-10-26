using Incognito.CameraRig;
using Incognito.Content;
using Incognito.Controls;
using Incognito.Display;
using Incognito.Eye;
using Incognito.Npc;
using Incognito.Settings;
using Incognito.Terminal;
using Incognito.UI;
using Incognito.Weapon;
using UnityEngine;
using UnityEngine.UIElements;

namespace Incognito.Hacking
{
    /// <summary>
    /// The hacking / console overlay (RECONSTRUCTION.md §5, U.INC-T7), wiring
    /// <see cref="HackingSession{TTarget}"/> to the scene:
    ///
    /// - Q (obj_walker key event 81) toggles the console view: the cursor goes to pointer mode,
    ///   spr_circuitwall1 fades in over the screen (obj_gui_console), and the camera follows the
    ///   cursor, edge-scrolling within 32px of the screen's sides (view_object = cursor).
    /// - A pointer-mode click on an NPC hacks it (obj_npc's pointer branch): spr_program_active
    ///   rises from the bottom-left (program[0]), the typed console slides in from the left
    ///   (console[0], <see cref="CommandPrompt"/>), and the camera centres on the NPC.
    /// - Escape closes the console; the program icon (obj_mouse_gui) reopens or closes it.
    /// - Q again restores the previous cursor mode, fades the backdrop out, sinks the icon, closes
    ///   the console and hands the camera back to the player.
    ///
    /// Drawn with a UI Toolkit backdrop and icon image (U.INC-T26 — was IMGUI) behind the console
    /// panel's own HUD layer (<see cref="HudRoot.HackingBackdrop"/>); an icon click plays
    /// obj_mouse_gui's <c>sfx_flick</c> (U.INC-T28, set per room by <see cref="ConfigureAudio"/>),
    /// and so does a click on the open console, which selects it (U.INC-T30). Clicks go through
    /// obj_mouse_gui's order (<see cref="MouseGui"/>): program icon, console, then the HUD pistol
    /// icon (<see cref="HudPistolIcon"/>).
    ///
    /// U.INC-T39 draws the rest of the original's GUI layer: obj_mouse_gui's spr_cursor_arrow at the
    /// pointer while <c>over_gui</c> is set and the player is not aiming (hiding obj_mouse's eye
    /// cursor, <see cref="GuiCursor"/>), and obj_gui_menu's spr_stealthwall dashboard backdrop
    /// (<see cref="DashboardMenu"/>), which fades in while the mouse is over the bottom 86px with the
    /// gun holstered and the console off.
    /// </summary>
    public sealed class HackingOverlay : MonoBehaviour
    {
        [SerializeField] private EyeCursor cursor;
        [SerializeField] private CommandPrompt prompt;
        [SerializeField] private CloseFramingCamera viewCamera;

        [Header("Original art")]
        [Tooltip("spr_circuitwall1 (1920x1080), obj_gui_console's backdrop.")]
        [SerializeField] private Texture2D backdrop;
        [Tooltip("spr_program (113x86), the program icon while no hack is running.")]
        [SerializeField] private Texture2D programIcon;
        [Tooltip("spr_program_active (113x86), the program icon during a hack.")]
        [SerializeField] private Texture2D programIconActive;
        [Tooltip("spr_cursor_arrow (94x64, origin 47,32), obj_mouse_gui's GUI cursor (U.INC-T39).")]
        [SerializeField] private Texture2D cursorArrow;
        [Tooltip("spr_stealthwall (1920x1080), obj_gui_menu's dashboard backdrop (U.INC-T39).")]
        [SerializeField] private Texture2D dashboard;

        [Header("Audio (sfx_flick, U.INC-T28)")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip flickSound;

        // start_game.gml: programX = 40; spr_program is 113x86.
        private const float ProgramX = 40f;
        private const float ProgramWidth = 113f;

        private static HackingOverlay active;

        public HackingSession<NpcInteraction> Session { get; } = new HackingSession<NpcInteraction>();

        /// <summary>obj_gui_menu's dashboard backdrop fade (U.INC-T39).</summary>
        public DashboardMenu Dashboard { get; } = new DashboardMenu();

        /// <summary>obj_walker's global <c>inverted</c> for whoever draws by it (obj_npc's
        /// brain-select overlay, U.INC-T39): the console view is up in the active overlay.</summary>
        public static bool ConsoleViewOn => active != null && active.Session.Inverted;

        /// <summary>obj_mouse_gui's spr_cursor_arrow is drawn this frame.</summary>
        public bool ArrowShowing { get; private set; }

        /// <summary>Where the arrow's origin is drawn, in view pixels (the pointer: obj_mouse_gui's
        /// <c>x = mouse_x - view_xview; y = mouse_y - view_yview</c>).</summary>
        public Vector2 ArrowAt { get; private set; }

        public EyeCursor Cursor { get => cursor; set => cursor = value; }
        public CommandPrompt Prompt { get => prompt; set => prompt = value; }
        public CloseFramingCamera ViewCamera { get => viewCamera; set => viewCamera = value; }

        /// <summary>The click sound (obj_mouse_gui: <c>audio_play_sound(sfx_flick,1,0)</c> on a program
        /// icon click) for the room this overlay belongs to (U.INC-T28).</summary>
        public void ConfigureAudio(AudioSource source, AudioClip flick)
        {
            audioSource = source;
            flickSound = flick;
        }

        public AudioClip FlickSound => flickSound;

        /// <summary>
        /// Whether a click at <paramref name="screenPoint"/> (the pointer's screen position)
        /// lands on the overlay's GUI — the program icon or the console panel — and so must not
        /// also click the world behind it (obj_mouse_gui's <c>over_gui</c>).
        /// </summary>
        public static bool IsOverGui(Vector2 screenPoint) =>
            IsOverProgramIcon(screenPoint) || IsOverConsole(screenPoint) || HudPistolIcon.IsOverIcon(screenPoint);

        /// <summary>The pointer is over the risen program icon (obj_mouse_gui's obj_program test).</summary>
        public static bool IsOverProgramIcon(Vector2 screenPoint) =>
            active != null && active.Session.Program.Visible && active.ProgramIconContains(screenPoint);

        /// <summary>The pointer is over the open console panel (obj_mouse_gui's obj_console test).</summary>
        public static bool IsOverConsole(Vector2 screenPoint) =>
            active != null && active.prompt != null && active.prompt.ContainsScreenPoint(screenPoint);

        private bool consoleWasShowing;
        private VisualElement backdropElement;
        private VisualElement iconElement;
        private VisualElement dashboardElement;
        private VisualElement arrowElement;
        private Vector2 arrowOrigin;

        private void OnEnable()
        {
            active = this;
            NpcInteraction.AnyPointerClicked += OnNpcPointerClicked;

            HudRoot.EnsureInstalled();
            backdropElement = new VisualElement { pickingMode = PickingMode.Ignore };
            backdropElement.style.position = Position.Absolute;
            backdropElement.style.left = 0f;
            backdropElement.style.top = 0f;
            backdropElement.style.right = 0f;
            backdropElement.style.bottom = 0f;
            backdropElement.style.backgroundSize = new BackgroundSize(Length.Percent(100f), Length.Percent(100f));
            backdropElement.style.display = DisplayStyle.None;
            HudRoot.HackingBackdrop.Add(backdropElement);

            iconElement = new VisualElement { pickingMode = PickingMode.Ignore };
            iconElement.style.position = Position.Absolute;
            iconElement.style.width = ProgramWidth;
            iconElement.style.height = HackingSession<NpcInteraction>.ProgramIconHeight;
            iconElement.style.backgroundSize = new BackgroundSize(Length.Percent(100f), Length.Percent(100f));
            iconElement.style.display = DisplayStyle.None;
            HudRoot.HackingBackdrop.Add(iconElement);

            // obj_gui_menu: draw_self() at (0, 0) in the GUI layer, the whole view.
            dashboardElement = FullView();
            HudRoot.Dashboard.Add(dashboardElement);

            // obj_mouse_gui: draw_self() at the pointer, spr_cursor_arrow at its GM origin.
            ArtDefinition arrow = GameSettings.Content.FindArt("spr_cursor_arrow");
            arrowOrigin = arrow != null ? new Vector2(arrow.OriginX, arrow.OriginY) : Vector2.zero;
            arrowElement = new VisualElement { pickingMode = PickingMode.Ignore };
            arrowElement.style.position = Position.Absolute;
            arrowElement.style.width = arrow != null ? arrow.Width : 94f;
            arrowElement.style.height = arrow != null ? arrow.Height : 64f;
            arrowElement.style.backgroundSize = new BackgroundSize(Length.Percent(100f), Length.Percent(100f));
            arrowElement.style.display = DisplayStyle.None;
            HudRoot.GuiCursor.Add(arrowElement);
        }

        private static VisualElement FullView()
        {
            var element = new VisualElement { pickingMode = PickingMode.Ignore };
            element.style.position = Position.Absolute;
            element.style.left = 0f;
            element.style.top = 0f;
            element.style.right = 0f;
            element.style.bottom = 0f;
            element.style.backgroundSize = new BackgroundSize(Length.Percent(100f), Length.Percent(100f));
            element.style.display = DisplayStyle.None;
            return element;
        }

        private void OnDisable()
        {
            NpcInteraction.AnyPointerClicked -= OnNpcPointerClicked;
            if (active == this) active = null;
            if (cursor != null) cursor.HiddenByGui = false;
            backdropElement?.RemoveFromHierarchy();
            iconElement?.RemoveFromHierarchy();
            dashboardElement?.RemoveFromHierarchy();
            arrowElement?.RemoveFromHierarchy();
        }

        private void Update()
        {
            // The console took the keyboard: Q is a letter being typed, not the toggle.
            if (!CommandPrompt.CapturesKeyboard && GameInput.Current.ConsoleTogglePressed) ToggleInverted();

            // obj_mouse_gui's chain: the program icon, else the console; the pistol icon (last in
            // the chain) is HudPistolIcon's own, resolved the same way.
            if (GameInput.Current.LeftClickPressed)
            {
                Vector2 p = GameInput.PointerScreenPosition;
                GuiClicked(MouseGui.Resolve(IsOverProgramIcon(p), IsOverConsole(p), HudPistolIcon.IsOverIcon(p)));
            }

            // Escape (or anything else) closed the console under a running hack: the hack ends.
            bool showing = prompt != null && prompt.IsShowing;
            if (consoleWasShowing && !showing && Session.Target != null)
            {
                Session.ConsoleClosed();
                ApplyView();
            }
            consoleWasShowing = showing;

            // obj_gui_menu's step: the mouse over the bottom strip, the gun holstered and the
            // console backdrop gone, fades the dashboard in; anywhere above it, out.
            bool gunOff = cursor == null || !cursor.WeaponDrawn;
            Dashboard.Watch(gunOff, Session.Backdrop.Current == OverlayFade.Mode.Off, GameDisplay.PointerInView.y);

            Session.Step(Time.deltaTime);
            Dashboard.Step(Time.deltaTime);

            // over_gui picks the cursor: obj_mouse_gui's arrow, or obj_mouse's eye.
            bool aiming = cursor != null && cursor.Aiming;
            ArrowShowing = GuiCursor.ShowsArrow(Session.OverGui, aiming);
            ArrowAt = GameDisplay.ScreenToView(GameInput.PointerScreenPosition);
            if (cursor != null) cursor.HiddenByGui = !GuiCursor.ShowsEyeCursor(Session.OverGui, aiming);

            ApplyOverlay();
        }

        public void ToggleInverted()
        {
            var current = cursor != null ? cursor.Mode : EyeCursorMode.Look;
            var mode = Session.ToggleInverted(current);
            if (cursor != null) cursor.Mode = mode;
            if (!Session.Inverted && prompt != null) prompt.Close();
            ApplyView();
        }

        private void OnNpcPointerClicked(NpcInteraction npc)
        {
            if (!Session.PointerClicked(npc)) return;
            if (prompt != null) prompt.Open();
            consoleWasShowing = prompt != null && prompt.IsShowing;
            ApplyView();
        }

        /// <summary>The number of <c>sfx_flick</c> plays so far (so a play test can see the click sounded).</summary>
        public int FlickCount { get; private set; }

        /// <summary>
        /// One obj_mouse_gui left click on <paramref name="hit"/>: a program icon or console click
        /// plays <c>sfx_flick</c>; a program icon click opens or closes the console; a console click
        /// selects it (<c>selected_id = instance.program_id</c>, which is what lets it take keys —
        /// the rebuild has the one console, so selecting it gives its input line the keyboard focus
        /// back, U.INC-T30). The pistol icon's click is <see cref="HudPistolIcon"/>'s own.
        /// </summary>
        public void GuiClicked(GuiClick hit)
        {
            Session.GuiPressed(hit);

            if (MouseGui.PlaysFlick(hit))
            {
                FlickCount++;
                if (audioSource != null && flickSound != null) audioSource.PlayOneShot(flickSound);
            }

            if (hit == GuiClick.ProgramIcon) ProgramIconClicked();
            else if (hit == GuiClick.Console && prompt != null) prompt.Select();
        }

        private void ProgramIconClicked()
        {

            bool open = prompt != null && prompt.IsShowing;
            var reopen = Session.ProgramClicked(open);
            if (open)
            {
                prompt.Close();
            }
            else if (reopen != null && prompt != null)
            {
                prompt.Open();
            }

            consoleWasShowing = prompt != null && prompt.IsShowing;
            ApplyView();
        }

        /// <summary>view_object[0]: the player outside the console view, the hacked NPC during a
        /// hack, otherwise the cursor.</summary>
        private void ApplyView()
        {
            if (viewCamera == null) return;
            if (!Session.Inverted) viewCamera.FollowPlayer();
            else if (Session.Target != null) viewCamera.FollowTarget(Session.Target.transform);
            else viewCamera.FollowCursor();
        }

        /// <summary>The program icon's rect in view pixels (U.INC-T32: hit-tested through the
        /// letterboxed view, not the raw screen).</summary>
        private Rect ProgramIconViewRect()
        {
            return new Rect(ProgramX, Session.Program.Y, ProgramWidth, HackingSession<NpcInteraction>.ProgramIconHeight);
        }

        private bool ProgramIconContains(Vector2 screenPoint)
        {
            return GameDisplay.ViewRectContains(ProgramIconViewRect(), screenPoint);
        }

        private void ApplyOverlay()
        {
            float alpha = Session.Backdrop.Alpha;
            if (alpha > 0f && backdrop != null)
            {
                backdropElement.style.backgroundImage = new StyleBackground(backdrop);
                backdropElement.style.opacity = alpha;
                backdropElement.style.display = DisplayStyle.Flex;
            }
            else
            {
                backdropElement.style.display = DisplayStyle.None;
            }

            Texture2D icon = Session.ProgramActive ? programIconActive : programIcon;
            if (Session.Program.Visible && icon != null)
            {
                iconElement.style.backgroundImage = new StyleBackground(icon);
                iconElement.style.left = ProgramX;
                iconElement.style.top = Session.Program.Y;
                iconElement.style.display = DisplayStyle.Flex;
            }
            else
            {
                iconElement.style.display = DisplayStyle.None;
            }

            if (Dashboard.Alpha > 0f && dashboard != null)
            {
                dashboardElement.style.backgroundImage = new StyleBackground(dashboard);
                dashboardElement.style.opacity = Dashboard.Alpha;
                dashboardElement.style.display = DisplayStyle.Flex;
            }
            else
            {
                dashboardElement.style.display = DisplayStyle.None;
            }

            if (ArrowShowing && cursorArrow != null)
            {
                arrowElement.style.backgroundImage = new StyleBackground(cursorArrow);
                arrowElement.style.left = ArrowAt.x - arrowOrigin.x;
                arrowElement.style.top = ArrowAt.y - arrowOrigin.y;
                arrowElement.style.display = DisplayStyle.Flex;
            }
            else
            {
                arrowElement.style.display = DisplayStyle.None;
            }
        }
    }
}
