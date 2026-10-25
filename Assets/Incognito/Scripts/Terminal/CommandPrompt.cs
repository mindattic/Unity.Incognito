using Incognito.Content;
using Incognito.Controls;
using Incognito.Display;
using Incognito.Player;
using Incognito.UI;
using Incognito.World;
using UnityEngine;
using UnityEngine.UIElements;

namespace Incognito.Terminal
{
    /// <summary>
    /// The on-screen typed console (RECONSTRUCTION.md §6, obj_command_prompt): a 580x200 panel
    /// that slides in from the left, a "> " prompt with the last command's output above it, drawn
    /// with a UI Toolkit panel and a native <see cref="TextField"/> for typing (U.INC-T26 — was
    /// IMGUI manually parsing key events and blinking its own "_" i-beam; the engine's own text
    /// field now provides the caret). Enter sends the line to the world as a
    /// <see cref="RunConsoleCommand"/> from this prompt's player, which runs it through the
    /// <see cref="CommandInterpreter"/> against the console objects in <see cref="Vicinity"/> of
    /// <see cref="Origin"/>; the prompt shows the <see cref="ConsoleOutput"/> that comes back.
    ///
    /// The hacking overlay (U.INC-T7, <see cref="Hacking.HackingOverlay"/>) opens it — a
    /// pointer-mode click on an NPC, or its program icon — as obj_npc / obj_mouse_gui opened
    /// console[0]; Escape closes it. While open, the keyboard belongs to the prompt
    /// (<see cref="CapturesKeyboard"/>). Laid out as obj_console (the panel start_game.gml
    /// actually created), over the original spr_console_selected art when assigned;
    /// each keystroke plays <c>sfx_type</c> (U.INC-T11).
    /// </summary>
    public sealed class CommandPrompt : MonoBehaviour
    {
        [Tooltip("The player this console belongs to: its commands carry this player's id.")]
        [SerializeField] private PlayerLocomotion player;
        [SerializeField] private Transform origin;

        [Tooltip("Vicinity radius in original pixels (default: one view width).")]
        [SerializeField] private float vicinityRadius = Vicinity.DefaultRadius;

        [Tooltip("spr_console_selected (580x200). A flat placeholder panel when unset.")]
        [SerializeField] private Texture2D panelArt;

        [Tooltip("sfx_type: played on a keystroke (not with Shift held) unless already playing (U.INC-T11).")]
        [SerializeField] private AudioSource typeSource;
        [SerializeField] private AudioClip typeClip;

        // obj_console layout, in original pixels on the 1920x1080 view.
        private const float PanelWidth = 580f;
        private const float PanelHeight = 200f;
        private const float PanelTop = 1080f - 300f; // console[0].y = room_height - 300
        private const float InputInset = 65f;        // inputX = x + 65
        private const float OutputTop = 35f;         // draw_text(inputX, y + 35, output)
        private const float InputTop = 170f;         // console[0].inputY = y + 170
        private const float CpuTop = 8f;             // draw_text(inputX + 450, y + 8, "CPU: ...")
        private const float CpuRight = 450f;

        private static CommandPrompt openPrompt;

        /// <summary>True while any prompt is open, so gameplay ignores typed keys.</summary>
        public static bool CapturesKeyboard => openPrompt != null && openPrompt.Slide.IsOpen;

        /// <summary>Opening or open — not sliding away and not off.</summary>
        public bool IsShowing =>
            Slide.Current == CommandPromptSlide.Mode.Onscreen || Slide.Current == CommandPromptSlide.Mode.Idle;

        /// <summary>The panel's rect in view pixels.</summary>
        public Rect PanelViewRect => new Rect(Slide.X, PanelTop, PanelWidth, PanelHeight);

        /// <summary>The panel's screen rect in GUI coordinates (top-left origin), through the
        /// letterboxed view (U.INC-T32).</summary>
        public Rect PanelScreenRect => GameDisplay.ViewToGuiRect(PanelViewRect);

        /// <summary>Whether <paramref name="screenPoint"/> (the pointer's screen position,
        /// bottom-left origin) is over the visible panel, so the click is the panel's, not the
        /// world's.</summary>
        public bool ContainsScreenPoint(Vector2 screenPoint)
        {
            if (!Slide.IsOpen) return false;
            return GameDisplay.ViewRectContains(PanelViewRect, screenPoint);
        }

        /// <summary>obj_console's CPU readout: one console open is one active CPU.</summary>
        public string CpuText => Hacking.CpuReadout.Format(Hacking.CpuReadout.DefaultCpuPower, Slide.IsOpen ? 1 : 0);

        public CommandPromptSlide Slide { get; private set; }

        /// <summary>The line being typed.</summary>
        public string InputText => inputField != null ? inputField.value : string.Empty;

        /// <summary>The last command's output (empty after <c>clear</c>).</summary>
        public string Output { get; private set; } = string.Empty;

        public Transform Origin
        {
            get => origin;
            set => origin = value;
        }

        public PlayerLocomotion Player
        {
            get => player;
            set => player = value;
        }

        /// <summary>Sets the keystroke audio (sfx_type) for the room this prompt belongs to
        /// (U.INC-T26's RoomComposer).</summary>
        public void ConfigureAudio(AudioSource typeSource, AudioClip typeClip)
        {
            this.typeSource = typeSource;
            this.typeClip = typeClip;
        }

        private GameSession session;
        private VisualElement panel;
        private Label cpuLabel;
        private Label outputLabel;
        private TextField inputField;
        private int openedFrame = -1;

        private void Awake()
        {
            Slide = new CommandPromptSlide(PanelWidth);
            HudRoot.EnsureInstalled();
            BuildUi();
        }

        // Start: the player's seat (and so the session) is set in its Awake.
        private void Start()
        {
            session = player != null ? player.Session : null;
            if (session != null) session.Played += OnPlayed;
        }

        private void OnDisable()
        {
            if (openPrompt == this) openPrompt = null;
        }

        private void OnDestroy()
        {
            if (session != null) session.Played -= OnPlayed;
            panel?.RemoveFromHierarchy();
        }

        // The console's new output for this prompt's player.
        private void OnPlayed(GameEvent e)
        {
            if (e is ConsoleOutput output && output.Player == player.Id)
            {
                Output = output.Text;
                if (outputLabel != null) outputLabel.text = Output;
            }
        }

        public void Open()
        {
            if (Slide.IsOpen) return;
            Slide.Open();
            openPrompt = this;
            openedFrame = Time.frameCount;
            inputField.SetValueWithoutNotify(string.Empty);
            // The field must already be displayed to take focus — ApplyPanel() won't flip that on
            // until next Update, and a non-displayed element can't become the focused one.
            panel.style.display = DisplayStyle.Flex;
            inputField.Focus();
        }

        /// <summary>obj_mouse_gui's console click (<c>selected_id = instance.program_id</c>): the
        /// clicked console is the one that takes keys, so an open prompt gets the keyboard focus
        /// back (U.INC-T30).</summary>
        public void Select()
        {
            if (!Slide.IsOpen) return;
            openPrompt = this;
            inputField.Focus();
        }

        public void Close()
        {
            Slide.Close();
            inputField.Blur();
        }

        /// <summary>Runs <paramref name="line"/> as if typed and Enter pressed.</summary>
        public void Submit(string line)
        {
            inputField.SetValueWithoutNotify(string.Empty);
            if (player == null) return;

            const float px = PlayerLocomotion.WorldUnitsPerOriginalPixel;
            Vector2 center = origin != null ? (Vector2)origin.position : (Vector2)transform.position;
            player.Send(new RunConsoleCommand(player.Id, line, new Vec2(center.x / px, center.y / px), vicinityRadius));
        }

        private void Update()
        {
            if (Slide.IsOpen && GameInput.Current.EscapePressed) Close();

            Slide.Step(Time.deltaTime);
            if (!Slide.IsOpen && openPrompt == this) openPrompt = null;

            ApplyPanel();
        }

        private void BuildUi()
        {
            panel = new VisualElement { pickingMode = PickingMode.Position };
            panel.style.position = Position.Absolute;
            panel.style.top = PanelTop;
            panel.style.width = PanelWidth;
            panel.style.height = PanelHeight;
            panel.style.backgroundSize = new BackgroundSize(Length.Percent(100f), Length.Percent(100f));
            if (panelArt != null) panel.style.backgroundImage = new StyleBackground(panelArt);
            else panel.style.backgroundColor = new Color(0f, 0.05f, 0.03f, 0.85f);
            panel.style.display = DisplayStyle.None;
            HudRoot.ConsolePanel.Add(panel);

            cpuLabel = new Label { pickingMode = PickingMode.Ignore };
            StyleText(cpuLabel);
            cpuLabel.style.position = Position.Absolute;
            cpuLabel.style.left = InputInset;
            cpuLabel.style.top = CpuTop;
            cpuLabel.style.width = CpuRight;
            cpuLabel.style.unityTextAlign = TextAnchor.UpperRight;
            panel.Add(cpuLabel);

            outputLabel = new Label { pickingMode = PickingMode.Ignore };
            StyleText(outputLabel);
            // Spaces kept (the help text's indent), wrapping only at the panel's edge as a safety net.
            outputLabel.style.whiteSpace = WhiteSpace.PreWrap;
            outputLabel.style.position = Position.Absolute;
            outputLabel.style.left = InputInset;
            outputLabel.style.top = OutputTop;
            outputLabel.style.width = PanelWidth - InputInset - 12f;
            outputLabel.style.height = InputTop - OutputTop;
            panel.Add(outputLabel);

            var promptLabel = new Label(">") { pickingMode = PickingMode.Ignore };
            StyleText(promptLabel);
            promptLabel.style.position = Position.Absolute;
            promptLabel.style.left = InputInset - 12f;
            promptLabel.style.top = InputTop;
            panel.Add(promptLabel);

            inputField = new TextField { multiline = false, isDelayed = false };
            inputField.style.position = Position.Absolute;
            inputField.style.left = InputInset;
            inputField.style.top = InputTop - 2f;
            inputField.style.width = PanelWidth - InputInset - 12f;
            StripChrome(inputField);
            inputField.RegisterCallback<KeyDownEvent>(OnKeyDown);
            panel.Add(inputField);
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            // The keystroke (if any) that opened the prompt this same frame must not also be typed
            // into it — Open() may run from the same input poll that is still delivering this event.
            if (Time.frameCount == openedFrame) return;

            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                Submit(inputField.value);
                evt.StopPropagation();
            }

            // obj_command_prompt keyboard event: if (!shift) if (!audio_is_playing(sfx_type)) play it.
            if (!evt.shiftKey && typeSource != null && typeClip != null && !typeSource.isPlaying)
            {
                typeSource.clip = typeClip;
                typeSource.Play();
            }
        }

        private void ApplyPanel()
        {
            panel.style.left = Slide.X;
            panel.style.display = Slide.IsOpen ? DisplayStyle.Flex : DisplayStyle.None;
            cpuLabel.text = CpuText;
        }

        // obj_console's draw: c_white, fnt_system, plain draw_text (no outline).
        private static void StyleText(Label label)
        {
            OutlinedText.Plain(label, GameFonts.System, Color.white);
        }

        /// <summary>Strips the TextField's default box chrome so only its text shows, like the
        /// original's plain draw_text input line.</summary>
        private static void StripChrome(TextField field)
        {
            field.style.backgroundColor = Color.clear;
            field.style.marginLeft = 0;
            field.style.marginRight = 0;
            field.style.marginTop = 0;
            field.style.marginBottom = 0;
            field.style.borderLeftWidth = 0;
            field.style.borderRightWidth = 0;
            field.style.borderTopWidth = 0;
            field.style.borderBottomWidth = 0;
            field.style.paddingLeft = 0;
            field.style.paddingRight = 0;
            field.style.paddingTop = 0;
            field.style.paddingBottom = 0;

            VisualElement input = field.Q(className: TextField.inputUssClassName);
            if (input == null) return;
            input.style.backgroundColor = Color.clear;
            input.style.borderLeftWidth = 0;
            input.style.borderRightWidth = 0;
            input.style.borderTopWidth = 0;
            input.style.borderBottomWidth = 0;
            input.style.marginLeft = 0;
            input.style.paddingLeft = 0;
            input.style.paddingTop = 0;
            input.style.color = Color.white;
            OutlinedText.SetFont(input, GameFonts.System);
        }
    }
}
