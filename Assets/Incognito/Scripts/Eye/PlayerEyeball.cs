using Incognito.Player;
using UnityEngine;

namespace Incognito.Eye
{
    /// <summary>
    /// The player's diegetic in-world eyeball (obj_eye, RECONSTRUCTION.md §3): drives
    /// <see cref="EyeballState"/> from the player and mouse and lays out the original's
    /// spr_eyewhite / spr_eye_ball / spr_eyelid_left|right stack. Like the original (eye depth 1,
    /// player depth 0) the stack renders BEHIND the player sprite, showing through the art's eye
    /// socket, so the resting eyelid above the socket stays hidden behind the face.
    /// Separate from <see cref="EyeCursor"/>, the eye-shaped OS cursor — two systems, not one.
    /// </summary>
    [DefaultExecutionOrder(100)] // after the player's Update and the camera's LateUpdate
    public sealed class PlayerEyeball : MonoBehaviour
    {
        [SerializeField] private PlayerLocomotion player;
        [SerializeField] private Camera viewCamera;

        [Header("Sprites (spr_eyewhite, spr_eye_ball, spr_eyelid_left/right)")]
        [SerializeField] private Sprite eyeWhite;
        [SerializeField] private Sprite pupil;
        [SerializeField] private Sprite eyelidLeft;
        [SerializeField] private Sprite eyelidRight;

        [Tooltip("Sorting order of the player sprite; the eye stack sits just behind it.")]
        [SerializeField] private int playerSortingOrder = 0;

        // The original room was exactly one view tall, so mouse_y was the cursor's height from the
        // top of a 1080px screen. The rebuild keeps that reading (screen-relative) for the pupil.
        private const float ReferenceViewHeight = 1080f;

        public EyeballState State { get; private set; }

        /// <summary>Set by the weapon system (U.INC-T9) while aimmode == "aiming".</summary>
        public bool Aiming { get; set; }

        public PlayerLocomotion Player { get => player; set => player = value; }
        public Camera ViewCamera { get => viewCamera; set => viewCamera = value; }

        private SpriteRenderer whiteRenderer;
        private SpriteRenderer pupilRenderer;
        private SpriteRenderer lidRenderer;

        private void Awake()
        {
            State = new EyeballState();
            whiteRenderer = CreateLayer("EyeWhite", eyeWhite, playerSortingOrder - 3);
            pupilRenderer = CreateLayer("Pupil", pupil, playerSortingOrder - 2);
            lidRenderer = CreateLayer("Eyelid", eyelidLeft, playerSortingOrder - 1);
        }

        private SpriteRenderer CreateLayer(string layerName, Sprite sprite, int order)
        {
            var go = new GameObject(layerName, typeof(SpriteRenderer));
            go.transform.SetParent(transform, false);
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }

        private void LateUpdate()
        {
            if (player == null || player.State == null) return;
            var cam = viewCamera != null ? viewCamera : Camera.main;
            if (cam == null) return;

            const float px = PlayerLocomotion.WorldUnitsPerOriginalPixel;
            Vector3 playerPos = player.transform.position;
            Vector3 mouseScreen = Input.mousePosition;
            Vector3 mouseWorld = cam.ScreenToWorldPoint(mouseScreen);

            float mouseOffsetX = (mouseWorld.x - playerPos.x) / px;
            float mouseY = Screen.height > 0
                ? (1f - mouseScreen.y / Screen.height) * ReferenceViewHeight
                : ReferenceViewHeight * 0.5f;

            var facing = player.State.Facing;
            State.Step(Time.deltaTime, facing, mouseOffsetX, mouseY, Aiming);

            // Socket = (player.x + offsetX, player.y); GM y runs down, world y runs up, so every
            // original y offset is negated below. Sprites are imported with GM's own origins.
            transform.position = new Vector3(
                playerPos.x + State.SocketOffsetX * px, playerPos.y, playerPos.z);

            // draw_sprite(spr_eyewhite, 0, x - 10, y - 10)
            whiteRenderer.transform.localPosition = new Vector3(-10f * px, 10f * px, 0f);
            // draw_sprite(spr_eye_ball, 0, pupilX, pupilY)
            pupilRenderer.transform.localPosition = new Vector3(
                State.PupilOffset.x * px, -State.PupilOffset.y * px, 0f);
            // draw_sprite(eyelid_asset, 0, x - 10, eyelidY - 10), eyelidY = y - eyelid_height + eyelidDY
            lidRenderer.sprite = facing == PlayerLocomotionState.FacingDirection.Left ? eyelidLeft : eyelidRight;
            lidRenderer.transform.localPosition = new Vector3(
                -10f * px, (State.EyelidHeight - State.EyelidDrop + 10f) * px, 0f);
        }
    }
}
