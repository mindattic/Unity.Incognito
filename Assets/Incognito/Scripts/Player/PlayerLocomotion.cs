using Incognito.Settings;
using Incognito.Terminal;
using Incognito.World;
using UnityEngine;

namespace Incognito.Player
{
    /// <summary>
    /// The player's seat and body view. It seats the player in the scene's <see cref="GameSession"/>,
    /// sends what the player is holding as a <see cref="SetControls"/> command each frame and applies
    /// the world's <see cref="PlayerLocomotionState"/> to the transform and sprite. It never changes
    /// that state itself. See docs/RECONSTRUCTION.md §1. Arm swing and aiming live in
    /// Incognito.Weapon.PlayerWeapon (U.INC-T9); aiming locks movement in the world.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PlayerLocomotion : MonoBehaviour
    {
        [Tooltip("The scene's authority. Found in the scene when unassigned.")]
        [SerializeField] private GameSession session;
        [Tooltip("This player's id in the world; every command from this seat carries it.")]
        [SerializeField] private int playerId = 1;

        public GameSession Session => session;
        public PlayerId Id => new PlayerId(playerId);

        /// <summary>This player in the world; read it, never write it.</summary>
        public PlayerEntity Entity { get; private set; }

        public PlayerLocomotionState State => Entity?.Locomotion;

        /// <summary>Aiming (U.INC-T9) locks walking: the original forced mode = "idle", dx = 0 and
        /// ignored the walk keys, so facing can't flip either.</summary>
        public bool MovementLocked => Entity != null && Entity.MovementLocked;

        /// <summary>Set by a side exit (U.INC-T11, obj_walker's autowalk): the player keeps
        /// walking the way they face at walk speed, ignoring the walk keys, while the room fades.</summary>
        public bool AutoWalk => Entity != null && Entity.AutoWalk;

        private SpriteRenderer spriteRenderer;
        private float baseY;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            baseY = transform.position.y;

            session = GameSession.Find(session);
            if (session == null)
            {
                Debug.LogError("PlayerLocomotion needs a GameSession in the scene.", this);
                enabled = false;
                return;
            }

            // U.INC-T22: locomotion/weapon tuning comes from the validated settings registry, not
            // serialized fields duplicated here.
            var tuning = GameSettings.Tuning;
            Entity = session.World.AddPlayer(Id, new PlayerSetup
            {
                StartX = transform.position.x / WorldUnitsPerOriginalPixel,
                Locomotion = tuning.Locomotion,
                Weapon = tuning.Weapon,
            });
        }

        /// <summary>Sends this player's command to the authority. Returns whether it took effect.</summary>
        public bool Send(GameCommand command) => session.Submit(command);

        private void Update()
        {
            // While the typed console is open the keyboard is typing, not walking (U.INC-T10).
            bool keys = !CommandPrompt.CapturesKeyboard;
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            Send(new SetControls(Id, new PlayerControls(
                moveLeft: keys && (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)),
                moveRight: keys && (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)),
                sprint: keys && shift,
                shiftHeld: shift,
                mouseBelowCentre: MouseBelowCentre(),
                lookingDown: PlayerLookingDown(),
                viewWidth: ViewWidth())));
        }

        // After the session has stepped the world this frame.
        private void LateUpdate() => ApplyView();

        /// <summary>Puts the transform and sprite where the world's state says the player is.</summary>
        public void ApplyView()
        {
            var state = State;
            if (state == null) return;

            transform.position = new Vector3(
                state.PositionX * WorldUnitsPerOriginalPixel,
                baseY + state.BobOffset * WorldUnitsPerOriginalPixel,
                transform.position.z);
            spriteRenderer.flipX = state.Facing == PlayerLocomotionState.FacingDirection.Right;
        }

        /// <summary>Cursor distance below the view's middle, in original px (GM: mouse_y - room_height/2).
        /// The original room was exactly one view tall, so this is screen-relative.</summary>
        private float MouseBelowCentre()
        {
            if (Screen.height <= 0) return 0f;
            float fromTop = 1f - Input.mousePosition.y / Screen.height;
            return (fromTop - 0.5f) * Entity.Weapon.ViewHeight;
        }

        // Original: mouse_y > room_height/2 — the cursor in the lower half of the screen (GM's
        // y grows downward; Unity's screen y grows upward).
        private static bool PlayerLookingDown()
        {
            return Screen.height > 0 && Input.mousePosition.y < Screen.height * 0.5f;
        }

        private const float FallbackViewWidth = 1920f;

        private static float ViewWidth()
        {
            var cam = Camera.main;
            if (cam == null || !cam.orthographic) return FallbackViewWidth;
            return cam.orthographicSize * 2f * cam.aspect / WorldUnitsPerOriginalPixel;
        }

        // The original ran in GameMaker pixel units; a real art pass (U.INC-T11) will set the
        // final scale. 1 original pixel ~= 1/32 Unity unit is a placeholder matching a typical
        // 32px-per-unit pixel-art import.
        public const float WorldUnitsPerOriginalPixel = 1f / 32f;
    }
}
