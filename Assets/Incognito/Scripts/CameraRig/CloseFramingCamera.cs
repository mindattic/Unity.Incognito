using Incognito.Player;
using Incognito.Settings;
using UnityEngine;

namespace Incognito.CameraRig
{
    /// <summary>
    /// Drives <see cref="CloseFramingCameraState"/> from the player and mouse and applies it to
    /// this orthographic camera. See docs/RECONSTRUCTION.md §2. The hacking overlay (U.INC-T7, §5)
    /// hands the view to the cursor or a hacked NPC through <see cref="FollowCursor"/> /
    /// <see cref="FollowTarget"/>, and back with <see cref="FollowPlayer"/>.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CloseFramingCamera : MonoBehaviour
    {
        [SerializeField] private PlayerLocomotion player;

        [Header("Room extent (world units; per-room scene data, not tuning)")]
        [SerializeField] private float roomMinX = 0f;
        [SerializeField] private float roomMaxX = 360f;

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

            // U.INC-T22: framing tuning comes from the validated settings registry; only the room
            // bounds (per-room scene data) stay as this component's own serialized fields.
            var tuning = GameSettings.Tuning.Camera;
            cursorFollowBorder = tuning.CursorFollowBorder;
            cursorFollowPanSpeed = tuning.CursorFollowPanSpeed;

            State = new CloseFramingCameraState(tuning)
            {
                RoomMinX = roomMinX / PlayerLocomotion.WorldUnitsPerOriginalPixel,
                RoomMaxX = roomMaxX / PlayerLocomotion.WorldUnitsPerOriginalPixel,
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

        /// <summary>Back to the close framing on the player (obj_walker's console toggle off:
        /// view_object[0] = camera).</summary>
        public void FollowPlayer()
        {
            followMode = FollowMode.Player;
            followTarget = null;
        }

        /// <summary>
        /// Follow the cursor (obj_walker's console toggle on: view_object[0] = cursor,
        /// view_hborder 32, view_hspeed 10): the view edge-scrolls while the mouse is within 32px
        /// of the left or right edge of the screen.
        /// </summary>
        public void FollowCursor()
        {
            followMode = FollowMode.Cursor;
            followTarget = null;
        }

        /// <summary>
        /// Centre on <paramref name="target"/> (obj_npc's pointer click: view_object[0] = the NPC,
        /// view_hborder = view_wview/2), panning there at the console's view_hspeed of 10.
        /// </summary>
        public void FollowTarget(Transform target)
        {
            followMode = target != null ? FollowMode.Target : FollowMode.Cursor;
            followTarget = target;
        }

        // LateUpdate so the camera frames where the player ended up this frame.
        private void LateUpdate()
        {
            if (player == null) return;
            SyncViewWidth();

            float mouseY = MouseViewportY();
            switch (followMode)
            {
                case FollowMode.Cursor:
                    State.StepToward(Time.deltaTime, State.CursorX(MouseViewportX()),
                        State.ViewWidth * 0.5f - cursorFollowBorder, cursorFollowPanSpeed, mouseY);
                    break;

                case FollowMode.Target when followTarget != null:
                    State.StepToward(Time.deltaTime,
                        followTarget.position.x / PlayerLocomotion.WorldUnitsPerOriginalPixel,
                        0f, cursorFollowPanSpeed, mouseY);
                    break;

                default:
                    State.Step(Time.deltaTime, player.State.PositionX, player.State.Facing, mouseY);
                    break;
            }

            Apply();
        }

        private enum FollowMode { Player, Cursor, Target }

        // obj_walker's console toggle: view_hborder[0] = 32, view_hspeed[0] = 10. Set from the
        // validated CameraTuning in Awake (U.INC-T22).
        private float cursorFollowBorder;
        private float cursorFollowPanSpeed;

        private FollowMode followMode = FollowMode.Player;
        private Transform followTarget;

        private static float MouseViewportX()
        {
            return Screen.width > 0 ? Mathf.Clamp01(Input.mousePosition.x / Screen.width) : 0.5f;
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
