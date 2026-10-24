using Incognito.Controls;
using Incognito.Player;
using Incognito.Terminal;
using UnityEngine;

namespace Incognito.World
{
    /// <summary>
    /// obj_exit: a floating door icon (spr_icon_exit) with its destination's name above it. It
    /// fades up as the player approaches (<see cref="RoomExitRules.DoorAlphaStep"/>), bobs gently
    /// (<see cref="DoorHoverState"/>), and W within 300px plays sfx_door_open and fades to the
    /// next room. Positioned in original px; the icon's SpriteRenderer is on this object.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class DoorExit : MonoBehaviour
    {
        [Tooltip("Instance position, original px from the room's top-left.")]
        [SerializeField] private Vector2 position;
        [SerializeField] private string label = string.Empty;
        [SerializeField] private string nextRoom = string.Empty;
        [SerializeField] private RoomEntrance entrance = RoomEntrance.Right;
        [SerializeField] private float enterX;
        [SerializeField] private Room room;
        [SerializeField] private RoomFade fade;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip doorOpen;
        [SerializeField] private Camera viewCamera;

        // draw_text_outline_alpha(x, y - 100, ...)
        private const float LabelRise = 100f;

        private readonly DoorHoverState hover = new DoorHoverState();
        private readonly StepClock clock = new StepClock();
        private SpriteRenderer icon;
        private GameSession session;
        private float alpha;
        private bool taken;

        public float Alpha => alpha;

        public void Configure(Vector2 position, string label, string nextRoom, RoomEntrance entrance, float enterX,
            Room room, RoomFade fade, AudioSource audioSource, AudioClip doorOpen, Camera viewCamera)
        {
            this.position = position;
            this.label = label;
            this.nextRoom = nextRoom;
            this.entrance = entrance;
            this.enterX = enterX;
            this.room = room;
            this.fade = fade;
            this.audioSource = audioSource;
            this.doorOpen = doorOpen;
            this.viewCamera = viewCamera;
        }

        private void Awake()
        {
            icon = GetComponent<SpriteRenderer>();
            Apply();
        }

        // Start: the player's seat (and so the session) is set in its Awake.
        private void Start()
        {
            if (room == null || room.Player == null) return;
            session = room.Player.Session;
            if (session != null) session.Played += OnPlayed;
        }

        private void OnDestroy()
        {
            if (session != null) session.Played -= OnPlayed;
        }

        private void Update()
        {
            if (room == null || room.Player == null) return;
            PlayerLocomotion player = room.Player;
            float playerX = player.State.PositionX;

            int steps = clock.Advance(Time.deltaTime);
            hover.Step(steps);
            for (int i = 0; i < steps; i++) alpha = RoomExitRules.DoorAlphaStep(alpha, playerX, position.x);
            Apply();

            if (!CommandPrompt.CapturesKeyboard && GameInput.Current.DoorUsePressed) TryUse();
        }

        /// <summary>
        /// obj_exit's W press: asks the world to use the door. Within <see cref="RoomExitRules.DoorReach"/>
        /// it idles the player and answers with <see cref="DoorUsed"/>, which plays sfx_door_open and
        /// fades to the next room. Returns whether it did.
        /// </summary>
        public bool TryUse()
        {
            if (taken || fade == null || room == null || room.Player == null) return false;
            return room.Player.Send(new UseDoor(room.Player.Id, position.x));
        }

        private void OnPlayed(GameEvent e)
        {
            if (!(e is DoorUsed used) || taken || fade == null) return;
            if (used.Player != room.Player.Id || !Mathf.Approximately(used.DoorX, position.x)) return;

            taken = true;
            if (audioSource != null && doorOpen != null) audioSource.PlayOneShot(doorOpen);
            RoomTransition.Request(nextRoom, entrance, enterX, room.Player.State.Facing);
            fade.FadeIn();
        }

        private void Apply()
        {
            const float px = PlayerLocomotion.WorldUnitsPerOriginalPixel;
            float y = position.y + hover.OffsetY;
            transform.position = new Vector3(position.x * px, (OutlinedText.ReferenceHeight - y) * px, transform.position.z);
            if (icon != null) icon.color = new Color(1f, 1f, 1f, alpha);
        }

        private void OnGUI()
        {
            var cam = viewCamera != null ? viewCamera : Camera.main;
            if (cam == null || alpha <= 0f) return;

            const float px = PlayerLocomotion.WorldUnitsPerOriginalPixel;
            Vector3 screen = cam.WorldToScreenPoint(transform.position + new Vector3(0f, LabelRise * px, 0f));
            float s = OutlinedText.Scale;
            // fa_center, text top at y - 100.
            var rect = new Rect(screen.x - 600f * s, Screen.height - screen.y, 1200f * s, 60f * s);
            OutlinedText.Draw(rect, label, TextAnchor.UpperCenter, alpha);
        }
    }
}
