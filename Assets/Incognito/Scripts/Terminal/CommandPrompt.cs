using Incognito.Player;
using Incognito.World;
using UnityEngine;

namespace Incognito.Terminal
{
    /// <summary>
    /// The on-screen typed console (RECONSTRUCTION.md §6, obj_command_prompt): a 580x200 panel
    /// that slides in from the left, a "> " prompt with a blinking "_" i-beam (toggled every 30
    /// steps), and the last command's output above it. Enter sends the line to the world as a
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
        [SerializeField] private KeyCode closeKey = KeyCode.Escape;

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
        private const float IBeamPeriodSteps = 30f;

        private static CommandPrompt openPrompt;

        /// <summary>True while any prompt is open, so gameplay ignores typed keys.</summary>
        public static bool CapturesKeyboard => openPrompt != null && openPrompt.Slide.IsOpen;

        /// <summary>Opening or open — not sliding away and not off.</summary>
        public bool IsShowing =>
            Slide.Current == CommandPromptSlide.Mode.Onscreen || Slide.Current == CommandPromptSlide.Mode.Idle;

        /// <summary>The panel's screen rect in GUI coordinates (top-left origin).</summary>
        public Rect PanelScreenRect
        {
            get
            {
                float scale = Screen.height / 1080f;
                return new Rect(Slide.X * scale, PanelTop * scale, PanelWidth * scale, PanelHeight * scale);
            }
        }

        /// <summary>Whether <paramref name="screenPoint"/> (Input.mousePosition, bottom-left
        /// origin) is over the visible panel, so the click is the panel's, not the world's.</summary>
        public bool ContainsScreenPoint(Vector2 screenPoint)
        {
            if (!Slide.IsOpen) return false;
            return PanelScreenRect.Contains(new Vector2(screenPoint.x, Screen.height - screenPoint.y));
        }

        /// <summary>obj_console's CPU readout: one console open is one active CPU.</summary>
        public string CpuText => Hacking.CpuReadout.Format(Hacking.CpuReadout.DefaultCpuPower, Slide.IsOpen ? 1 : 0);

        public CommandPromptSlide Slide { get; private set; }

        /// <summary>The line being typed.</summary>
        public string InputText { get; private set; } = string.Empty;

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

        private GameSession session;
        private int openedFrame = -1;
        private GUIStyle textStyle;
        private Texture2D panelTexture;

        private void Awake()
        {
            Slide = new CommandPromptSlide(PanelWidth);
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
            if (panelTexture != null) Destroy(panelTexture);
        }

        // The console's new output for this prompt's player.
        private void OnPlayed(GameEvent e)
        {
            if (e is ConsoleOutput output && output.Player == player.Id) Output = output.Text;
        }

        public void Open()
        {
            if (Slide.IsOpen) return;
            Slide.Open();
            openPrompt = this;
            openedFrame = Time.frameCount;
        }

        public void Close()
        {
            Slide.Close();
        }

        /// <summary>Runs <paramref name="line"/> as if typed and Enter pressed.</summary>
        public void Submit(string line)
        {
            if (player == null) return;

            const float px = PlayerLocomotion.WorldUnitsPerOriginalPixel;
            Vector2 center = origin != null ? (Vector2)origin.position : (Vector2)transform.position;
            player.Send(new RunConsoleCommand(player.Id, line, new Vec2(center.x / px, center.y / px), vicinityRadius));
            InputText = string.Empty;
        }

        private void Update()
        {
            if (Slide.IsOpen && Input.GetKeyDown(closeKey)) Close();

            Slide.Step(Time.deltaTime);
            if (!Slide.IsOpen && openPrompt == this) openPrompt = null;
        }

        private void OnGUI()
        {
            if (!Slide.IsOpen) return;

            HandleTyping(Event.current);

            float scale = Screen.height / 1080f;
            EnsureStyle(scale);

            var panel = PanelScreenRect;
            GUI.DrawTexture(panel, panelArt != null ? panelArt : panelTexture);

            float inputX = panel.x + InputInset * scale;
            float lineHeight = textStyle.lineHeight;

            var cpuStyle = new GUIStyle(textStyle) { alignment = TextAnchor.UpperRight, wordWrap = false };
            GUI.Label(new Rect(inputX, panel.y + CpuTop * scale, CpuRight * scale, lineHeight), CpuText, cpuStyle);

            float inputY = panel.y + InputTop * scale;
            var outputRect = new Rect(inputX, panel.y + OutputTop * scale, panel.width - InputInset * scale - 12f * scale,
                inputY - (panel.y + OutputTop * scale));
            GUI.Label(outputRect, Output, textStyle);

            bool iBeamOn = Mathf.FloorToInt(Time.unscaledTime * CommandPromptSlide.ReferenceFps / IBeamPeriodSteps) % 2 == 0;
            GUI.Label(new Rect(inputX - 12f * scale, inputY, 20f * scale, lineHeight), ">", textStyle);
            GUI.Label(new Rect(inputX, inputY, outputRect.width, lineHeight), InputText + (iBeamOn ? "_" : ""), textStyle);
        }

        private void HandleTyping(Event e)
        {
            if (e.type != EventType.KeyDown) return;
            // The keystroke that opened the prompt must not also be typed into it.
            if (Time.frameCount == openedFrame || Slide.Current == CommandPromptSlide.Mode.Offscreen) return;

            if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter || e.character == '\n')
            {
                // Unity sends Enter as a keyCode event and a '\n' character event; act once.
                if (e.keyCode != KeyCode.None) Submit(InputText);
            }
            else if (e.keyCode == KeyCode.Backspace)
            {
                if (InputText.Length > 0) InputText = InputText.Substring(0, InputText.Length - 1);
            }
            else if (e.character != '\0' && !char.IsControl(e.character))
            {
                InputText += e.character;
            }
            else
            {
                return;
            }

            // obj_command_prompt keyboard event: if (!shift) if (!audio_is_playing(sfx_type)) play it.
            if (!e.shift && typeSource != null && typeClip != null && !typeSource.isPlaying)
            {
                typeSource.clip = typeClip;
                typeSource.Play();
            }

            e.Use();
        }

        private void EnsureStyle(float scale)
        {
            if (panelTexture == null)
            {
                panelTexture = new Texture2D(1, 1);
                panelTexture.SetPixel(0, 0, new Color(0f, 0.05f, 0.03f, 0.85f));
                panelTexture.Apply();
            }

            int fontSize = Mathf.Max(8, Mathf.RoundToInt(18f * scale));
            if (textStyle == null || textStyle.fontSize != fontSize)
            {
                textStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = fontSize,
                    wordWrap = true,
                    alignment = TextAnchor.UpperLeft,
                    normal = { textColor = Color.white },
                };
            }
        }
    }
}
