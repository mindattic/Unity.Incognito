using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Incognito.Terminal
{
    /// <summary>
    /// The on-screen typed console (RECONSTRUCTION.md §6, obj_command_prompt): a 580x200 panel
    /// that slides in from the left, a "> " prompt with a blinking "_" i-beam (toggled every 30
    /// steps), and the last command's output above it. Enter runs the line through
    /// <see cref="CommandInterpreter"/> against the <see cref="ConsoleTarget"/>s in
    /// <see cref="Vicinity"/> of <see cref="Origin"/>.
    ///
    /// In the original the prompt was opened from the hacking overlay's program icons
    /// (obj_mouse_gui) — that overlay is U.INC-T7. Until it exists, Q (the original's
    /// console-toggle key in obj_walker) opens the prompt and Escape closes it; while open, the
    /// keyboard belongs to the prompt (<see cref="CapturesKeyboard"/>).
    /// Drawn with IMGUI on placeholder colours until the art pass (U.INC-T11); the per-keystroke
    /// <c>sfx_type</c> also waits for the audio pass.
    /// </summary>
    public sealed class CommandPrompt : MonoBehaviour
    {
        [SerializeField] private Transform origin;
        [SerializeField] private KeyCode openKey = KeyCode.Q;
        [SerializeField] private KeyCode closeKey = KeyCode.Escape;

        [Tooltip("Vicinity radius in original pixels (default: one view width).")]
        [SerializeField] private float vicinityRadius = Vicinity.DefaultRadius;

        // Original layout, in original pixels on the 1920x1080 view.
        private const float PanelWidth = 580f;
        private const float PanelHeight = 200f;
        private const float BottomMargin = 100f; // obj_command_prompt: y = room_height - sprite_height - 100
        private const float InputInset = 65f;    // inputX = x + 65
        private const float IBeamPeriodSteps = 30f;

        private static CommandPrompt openPrompt;

        /// <summary>True while any prompt is open, so gameplay ignores typed keys.</summary>
        public static bool CapturesKeyboard => openPrompt != null && openPrompt.Slide.IsOpen;

        public CommandInterpreter Interpreter { get; private set; }
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

        private readonly Dictionary<ConsoleObject, ConsoleTarget> lastQuery = new Dictionary<ConsoleObject, ConsoleTarget>();
        private int openedFrame = -1;
        private GUIStyle textStyle;
        private Texture2D panelTexture;

        private void Awake()
        {
            Slide = new CommandPromptSlide(PanelWidth);
            Interpreter = new CommandInterpreter(QueryVicinity);
            Interpreter.Rebooted += obj => Resolve(obj)?.NotifyRebooted();
            Interpreter.Overwritten += obj => Resolve(obj)?.NotifyOverwritten();
        }

        private void OnDisable()
        {
            if (openPrompt == this) openPrompt = null;
        }

        private void OnDestroy()
        {
            if (panelTexture != null) Destroy(panelTexture);
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
            Output = Interpreter.Execute(line);
            InputText = string.Empty;
        }

        private void Update()
        {
            if (!Slide.IsOpen && !CapturesKeyboard && Input.GetKeyDown(openKey)) Open();
            else if (Slide.IsOpen && Input.GetKeyDown(closeKey)) Close();

            Slide.Step(Time.deltaTime);
            if (!Slide.IsOpen && openPrompt == this) openPrompt = null;
        }

        private IEnumerable<ConsoleObject> QueryVicinity()
        {
            lastQuery.Clear();
            Vector2 center = origin != null ? (Vector2)origin.position : (Vector2)transform.position;
            float radius = vicinityRadius * Player.PlayerLocomotion.WorldUnitsPerOriginalPixel;

            var candidates = ConsoleTarget.Active.Select(t => ((Vector2)t.transform.position, t.Object)).ToList();
            var nearby = Vicinity.Within(center, radius, candidates).ToList();
            foreach (ConsoleTarget target in ConsoleTarget.Active)
            {
                if (nearby.Contains(target.Object)) lastQuery[target.Object] = target;
            }

            return nearby;
        }

        private ConsoleTarget Resolve(ConsoleObject obj) => lastQuery.TryGetValue(obj, out var t) ? t : null;

        private void OnGUI()
        {
            if (!Slide.IsOpen) return;

            HandleTyping(Event.current);

            float scale = Screen.height / 1080f;
            EnsureStyle(scale);

            var panel = new Rect(
                Slide.X * scale,
                Screen.height - (PanelHeight + BottomMargin) * scale,
                PanelWidth * scale,
                PanelHeight * scale);
            GUI.DrawTexture(panel, panelTexture);

            float inputX = panel.x + InputInset * scale;
            float lineHeight = textStyle.lineHeight;
            var outputRect = new Rect(inputX, panel.y + 12f * scale, panel.width - InputInset * scale - 12f * scale,
                panel.height - 24f * scale - lineHeight);
            GUI.Label(outputRect, Output, textStyle);

            bool iBeamOn = Mathf.FloorToInt(Time.unscaledTime * CommandPromptSlide.ReferenceFps / IBeamPeriodSteps) % 2 == 0;
            float inputY = panel.yMax - 12f * scale - lineHeight;
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
