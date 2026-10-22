using Incognito.Player;
using UnityEngine;

namespace Incognito.Npc
{
    /// <summary>
    /// Drives an NPC's <see cref="NpcPatrolState"/> and <see cref="SuspicionMeter"/> and applies
    /// them to the transform, sprite and the "NN%" suspicion label the original drew over the
    /// NPC's head (draw_text_outline at y - 200). See docs/RECONSTRUCTION.md §4. Eyes (U.INC-T5)
    /// and click interactions (U.INC-T6/T7) are other tickets.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class NpcController : MonoBehaviour
    {
        [SerializeField] private PlayerLocomotion player;
        [Tooltip("Camera whose view width sets the suspicion ranges (falls back to Camera.main).")]
        [SerializeField] private Camera viewCamera;

        [Header("Patrol (original pixels)")]
        [Tooltip("X positions walked in order, then looped. One waypoint = walk there and stop.")]
        [SerializeField] private float[] waypoints = { 12000f };
        [SerializeField] private float walkSpeed = 5f;
        [SerializeField] private float arriveRange = 20f;

        [Header("Suspicion")]
        [SerializeField] private SuspicionTuning suspicion = new SuspicionTuning();
        [SerializeField] private TextMesh suspicionLabel;
        [Tooltip("Label height above the NPC's origin, in original pixels.")]
        [SerializeField] private float labelHeight = 200f;

        public NpcPatrolState Patrol { get; private set; }
        public SuspicionMeter Suspicion { get; private set; }

        public PlayerLocomotion Player
        {
            get => player;
            set => player = value;
        }

        private const float Px = PlayerLocomotion.WorldUnitsPerOriginalPixel;
        private const float FallbackViewWidth = 1920f;

        private SpriteRenderer spriteRenderer;
        private float baseY;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            baseY = transform.position.y;

            Patrol = new NpcPatrolState { WalkSpeed = walkSpeed, ArriveRange = arriveRange };
            Patrol.SetInitialPositionX(transform.position.x / Px);
            Patrol.SetRoute(waypoints);

            Suspicion = new SuspicionMeter(suspicion);

            // U.INC-T9: a shot NPC is shoved 100px. The patrol owns the position, so the shove
            // goes there too or the next Update would put the NPC back.
            var target = GetComponent<Incognito.Weapon.BulletTarget>();
            if (target != null) target.Hit += Patrol.Knockback;
        }

        private void Update()
        {
            Patrol.Step(Time.deltaTime);

            if (player != null && player.State != null)
            {
                Suspicion.Step(
                    Time.deltaTime,
                    Patrol.PositionX, Patrol.Facing,
                    player.State.PositionX, player.State.Facing,
                    ViewWidth(), PlayerLookingDown());
            }

            transform.position = new Vector3(
                Patrol.PositionX * Px,
                baseY + Patrol.BobOffset * Px,
                transform.position.z);
            spriteRenderer.flipX = Patrol.Facing == PlayerLocomotionState.FacingDirection.Right;

            if (suspicionLabel != null)
            {
                suspicionLabel.text = Suspicion.DisplayPercent + "%";
                suspicionLabel.transform.position = new Vector3(
                    transform.position.x,
                    transform.position.y + labelHeight * Px,
                    suspicionLabel.transform.position.z);
            }
        }

        private float ViewWidth()
        {
            var cam = viewCamera != null ? viewCamera : Camera.main;
            if (cam == null || !cam.orthographic) return FallbackViewWidth;
            return cam.orthographicSize * 2f * cam.aspect / Px;
        }

        // Original: mouse_y > room_height/2 — the cursor in the lower half of the screen (GM's
        // y grows downward; Unity's screen y grows upward).
        private static bool PlayerLookingDown()
        {
            return Screen.height > 0 && Input.mousePosition.y < Screen.height * 0.5f;
        }
    }
}
