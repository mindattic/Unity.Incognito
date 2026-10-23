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
        [SerializeField] private KeyCode useKey = KeyCode.W;
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

        private void Update()
        {
            if (room == null || room.Player == null) return;
            PlayerLocomotion player = room.Player;
            float playerX = player.State.PositionX;

            int steps = clock.Advance(Time.deltaTime);
            hover.Step(steps);
            for (int i = 0; i < steps; i++) alpha = RoomExitRules.DoorAlphaStep(alpha, playerX, position.x);
            Apply();

            if (!taken && fade != null && !CommandPrompt.CapturesKeyboard && Input.GetKeyDown(useKey) && RoomExitRules.CanUseDoor(playerX, position.x))
            {
                taken = true;
                if (audioSource != null && doorOpen != null) audioSource.PlayOneShot(doorOpen);
                RoomTransition.Request(nextRoom, entrance, enterX, player.State.Facing);
                player.State.ForceIdle(); // player.mode = "idle"
                fade.FadeIn();
            }
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
