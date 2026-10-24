using Incognito.CameraRig;
using Incognito.Controls;
using Incognito.Eye;
using Incognito.Npc;
using Incognito.Terminal;
using Incognito.UI;
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
    /// panel's own HUD layer (<see cref="HudRoot.HackingBackdrop"/>); obj_mouse_gui's
    /// <c>sfx_flick</c> on an icon click is optional until the audio pass (U.INC-T11).
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

        [Header("Audio (optional until U.INC-T11)")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip flickSound;

        // start_game.gml: programX = 40; spr_program is 113x86.
        private const float ProgramX = 40f;
        private const float ProgramWidth = 113f;

        private static HackingOverlay active;

        public HackingSession<NpcInteraction> Session { get; } = new HackingSession<NpcInteraction>();

        public EyeCursor Cursor { get => cursor; set => cursor = value; }
        public CommandPrompt Prompt { get => prompt; set => prompt = value; }
        public CloseFramingCamera ViewCamera { get => viewCamera; set => viewCamera = value; }

        /// <summary>
        /// Whether a click at <paramref name="screenPoint"/> (the pointer's screen position)
        /// lands on the overlay's GUI — the program icon or the console panel — and so must not
        /// also click the world behind it (obj_mouse_gui's <c>over_gui</c>).
        /// </summary>
        public static bool IsOverGui(Vector2 screenPoint) => active != null && active.HitsGui(screenPoint);

        private bool consoleWasShowing;
        private VisualElement backdropElement;
        private VisualElement iconElement;

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
        }

        private void OnDisable()
        {
            NpcInteraction.AnyPointerClicked -= OnNpcPointerClicked;
            if (active == this) active = null;
            backdropElement?.RemoveFromHierarchy();
            iconElement?.RemoveFromHierarchy();
        }

        private void Update()
        {
            // The console took the keyboard: Q is a letter being typed, not the toggle.
            if (!CommandPrompt.CapturesKeyboard && GameInput.Current.ConsoleTogglePressed) ToggleInverted();

            if (GameInput.Current.LeftClickPressed && Session.Program.Visible && ProgramIconContains(GameInput.PointerScreenPosition))
            {
                ProgramIconClicked();
            }

            // Escape (or anything else) closed the console under a running hack: the hack ends.
            bool showing = prompt != null && prompt.IsShowing;
            if (consoleWasShowing && !showing && Session.Target != null)
            {
                Session.ConsoleClosed();
                ApplyView();
            }
            consoleWasShowing = showing;

            Session.Step(Time.deltaTime);
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

        private void ProgramIconClicked()
        {
            if (audioSource != null && flickSound != null) audioSource.PlayOneShot(flickSound);

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

        private bool HitsGui(Vector2 screenPoint)
        {
            if (Session.Program.Visible && ProgramIconContains(screenPoint)) return true;
            return prompt != null && prompt.ContainsScreenPoint(screenPoint);
        }

        private Rect ProgramIconRect()
        {
            float scale = Screen.height / 1080f;
            return new Rect(ProgramX * scale, Session.Program.Y * scale, ProgramWidth * scale,
                HackingSession<NpcInteraction>.ProgramIconHeight * scale);
        }

        private bool ProgramIconContains(Vector2 screenPoint)
        {
            return ProgramIconRect().Contains(new Vector2(screenPoint.x, Screen.height - screenPoint.y));
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
        }
    }
}
