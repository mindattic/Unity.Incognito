using Incognito.Player;
using Incognito.World;
using UnityEngine;

namespace Incognito.Npc
{
    /// <summary>
    /// An NPC's view: it seats the NPC in the world (<see cref="NpcPatrolState"/> and
    /// <see cref="SuspicionMeter"/> live there, stepped by the session) and applies that state to the
    /// transform, sprite and the "NN%" suspicion label the original drew over the NPC's head
    /// (draw_text_outline at y - 200). It changes the NPC only by sending commands. See
    /// docs/RECONSTRUCTION.md §4. Talk mode (U.INC-T6) is driven from <see cref="NpcInteraction"/>;
    /// pointer mode is U.INC-T7.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class NpcController : MonoBehaviour
    {
        [Tooltip("The player this NPC watches (its seat supplies the session and the player id).")]
        [SerializeField] private PlayerLocomotion player;

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

        /// <summary>This NPC in the world; read it, never write it.</summary>
        public NpcEntity Entity { get; private set; }

        public NpcPatrolState Patrol => Entity?.Patrol;
        public SuspicionMeter Suspicion => Entity?.Suspicion;

        public PlayerLocomotion Player
        {
            get => player;
            set => player = value;
        }

        private const float Px = PlayerLocomotion.WorldUnitsPerOriginalPixel;

        private SpriteRenderer spriteRenderer;
        private GameSession session;
        private float baseY;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            baseY = transform.position.y;
        }

        // Start, not Awake: the player's seat (and so the session) is set in its Awake.
        private void Start()
        {
            session = GameSession.Find(player != null ? player.Session : null);
            if (session == null)
            {
                Debug.LogError("NpcController needs a GameSession in the scene.", this);
                enabled = false;
                return;
            }

            // U.INC-T22: this NPC's own suspicion tuning is Inspector-editable data; validated on
            // load rather than trusted blindly, same as the data files the settings registry loads.
            suspicion.Validate();

            Entity = session.World.AddNpc(new NpcSetup
            {
                Watching = player != null ? player.Id : default,
                StartX = transform.position.x / Px,
                Waypoints = waypoints,
                WalkSpeed = walkSpeed,
                ArriveRange = arriveRange,
                Suspicion = suspicion,
            });

            // U.INC-T9: a shot NPC is shoved 100px; the world owns the position, so the shove is a command.
            var target = GetComponent<Incognito.Weapon.BulletTarget>();
            if (target != null) target.Struck += OnStruck;
        }

        private void OnDestroy()
        {
            var target = GetComponent<Incognito.Weapon.BulletTarget>();
            if (target != null) target.Struck -= OnStruck;
            if (session != null && Entity != null) session.World.RemoveNpc(Entity.Id);
        }

        private void OnStruck(float shove, PlayerId shooter)
        {
            session.Submit(new ShoveNpc(shooter, Entity.Id, shove));
        }

        /// <summary>Talk-mode click (U.INC-T6): the NPC walks over to the player and starts talking.</summary>
        public void StartTalking()
        {
            if (player == null || Entity == null) return;
            // GM's sprite_width: the NPC's on-screen width in original pixels.
            session.Submit(new TalkToNpc(player.Id, Entity.Id, spriteRenderer.bounds.size.x / Px));
        }

        // After the session has stepped the world this frame.
        private void LateUpdate()
        {
            if (Entity == null) return;
            var patrol = Entity.Patrol;

            transform.position = new Vector3(
                patrol.PositionX * Px,
                baseY + patrol.BobOffset * Px,
                transform.position.z);
            spriteRenderer.flipX = patrol.Facing == PlayerLocomotionState.FacingDirection.Right;

            if (suspicionLabel != null)
            {
                suspicionLabel.text = Entity.Suspicion.DisplayPercent + "%";
                suspicionLabel.transform.position = new Vector3(
                    transform.position.x,
                    transform.position.y + labelHeight * Px,
                    suspicionLabel.transform.position.z);
            }
        }
    }
}
