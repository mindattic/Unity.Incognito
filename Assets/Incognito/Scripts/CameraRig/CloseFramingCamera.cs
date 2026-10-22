using Incognito.Player;
using UnityEngine;

namespace Incognito.CameraRig
{
    /// <summary>
    /// Drives <see cref="CloseFramingCameraState"/> from the player and mouse and applies it to
    /// this orthographic camera. See docs/RECONSTRUCTION.md §2. The console-overlay handoff
    /// (view_object switched to the cursor) belongs to U.INC-T7 and is not handled here.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CloseFramingCamera : MonoBehaviour
    {
        [SerializeField] private PlayerLocomotion player;

        [Header("Room extent (world units)")]
        [SerializeField] private float roomMinX = 0f;
        [SerializeField] private float roomMaxX = 360f;

        [Header("Framing (original pixels)")]
        [SerializeField] private float playerWidth = 512f;
        [SerializeField] private float horizontalDeadzone = 50f;
        [SerializeField] private float horizontalPanSpeed = 20f;
        [SerializeField] private float maxVerticalPan = 135f;

        public CloseFramingCameraState State { get; private set; }

        private Camera cam;
        private float restY;

        public PlayerLocomotion Player
        {
            get => player;
            set => player = value;
        }

        private void Awake()
        {
            cam = GetComponent<Camera>();
            restY = transform.position.y;

            State = new CloseFramingCameraState
            {
                PlayerWidth = playerWidth,
                RoomMinX = roomMinX / PlayerLocomotion.WorldUnitsPerOriginalPixel,
                RoomMaxX = roomMaxX / PlayerLocomotion.WorldUnitsPerOriginalPixel,
                HorizontalDeadzone = horizontalDeadzone,
                HorizontalPanSpeed = horizontalPanSpeed,
                MaxVerticalPan = maxVerticalPan,
            };
        }

        // Start (not Awake) so the player's own Awake has built its locomotion state.
        private void Start()
        {
            if (player == null) return;
            SyncViewWidth();
            State.SnapTo(player.State.PositionX, player.State.Facing, MouseViewportY());
            Apply();
        }

        // LateUpdate so the camera frames where the player ended up this frame.
        private void LateUpdate()
        {
            if (player == null) return;
            SyncViewWidth();
            State.Step(Time.deltaTime, player.State.PositionX, player.State.Facing, MouseViewportY());
            Apply();
        }

        private void SyncViewWidth()
        {
            float worldWidth = cam.orthographicSize * 2f * cam.aspect;
            State.ViewWidth = worldWidth / PlayerLocomotion.WorldUnitsPerOriginalPixel;
        }

        private void Apply()
        {
            transform.position = new Vector3(
                State.CenterX * PlayerLocomotion.WorldUnitsPerOriginalPixel,
                restY + State.VerticalOffset * PlayerLocomotion.WorldUnitsPerOriginalPixel,
                transform.position.z);
        }

        private static float MouseViewportY()
        {
            return Screen.height > 0 ? Input.mousePosition.y / Screen.height : 0.5f;
        }
    }
}
