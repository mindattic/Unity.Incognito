using Incognito.Content;
using Incognito.Player;
using Incognito.Settings;
using Incognito.UI;
using Incognito.World;
using UnityEngine;
using UnityEngine.UIElements;

namespace Incognito.Npc
{
    /// <summary>
    /// An NPC's view: it seats the NPC in the world (<see cref="NpcPatrolState"/> and
    /// <see cref="SuspicionMeter"/> live there, stepped by the session) and applies that state to the
    /// transform, sprite and the "NN%" suspicion label the original drew over the NPC's head
    /// (obj_npc's draw: fa_center, fnt_large, draw_text_outline(x, y - 200, 1, ...)), a UI Toolkit
    /// label on <see cref="HudRoot.WorldLabels"/> (U.INC-T31; was a world-space TextMesh). It
    /// changes the NPC only by sending commands. See docs/RECONSTRUCTION.md §4. Talk mode (U.INC-T6) is driven from <see cref="NpcInteraction"/>;
    /// pointer mode is U.INC-T7. While the console view is on (obj_walker's <c>inverted</c>) it draws
    /// obj_npc's inverted-blend copy of itself and spr_brain_select over it (U.INC-T39).
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

        [Header("Console view (obj_npc's draw while inverted, U.INC-T39)")]
        [Tooltip("spr_brain_select (200x115, origin 100,72), drawn at the NPC's origin.")]
        [SerializeField] private Sprite brainSelect;
        [Tooltip("Incognito/InvertBlend: bm_inv_dest_color, bm_inv_src_color.")]
        [SerializeField] private Shader invertShader;

        // draw_text_outline(x, y - 200, ...): the label's top is 200px above the NPC's origin.
        private const float LabelHeight = 200f;

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
        private Label suspicionLabel;
        private SpriteRenderer invertedCopy;
        private SpriteRenderer brainSelectRenderer;

        /// <summary>The suspicion label's text as drawn ("NN%"), for tests.</summary>
        public string SuspicionText => suspicionLabel != null ? suspicionLabel.text : string.Empty;

        /// <summary>The inverted-blend copy of the NPC is drawn this frame (console view on).</summary>
        public bool InvertedCopyShowing => invertedCopy != null && invertedCopy.enabled;

        /// <summary>spr_brain_select is drawn over the NPC this frame (console view on).</summary>
        public bool BrainSelectShowing => brainSelectRenderer != null && brainSelectRenderer.enabled;

        /// <summary>The renderer drawing spr_brain_select, for tests (null when no art was wired).</summary>
        public SpriteRenderer BrainSelectRenderer => brainSelectRenderer;

        // Same sorting layer and order as the NPC, each a hair nearer the camera, so they draw right
        // after it in GM's order (draw_self, the inverted copy, then the brain) and nothing else
        // sorts between them.
        private const float OverlayZStep = 0.001f;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            baseY = transform.position.y;

            // obj_npc's draw, if (inverted): draw_self() again under
            // draw_set_blend_mode_ext(bm_inv_dest_color, bm_inv_src_color), then, back on
            // bm_normal, draw_sprite(spr_brain_select, 0, x, y) (unflipped, at the NPC's origin).
            if (invertShader != null)
            {
                invertedCopy = Overlay("Inverted Copy", 1);
                invertedCopy.sharedMaterial = new Material(invertShader) { name = "NPC Inverted Copy" };
            }

            if (brainSelect != null)
            {
                brainSelectRenderer = Overlay("Brain Select", 2);
                brainSelectRenderer.sprite = brainSelect;
            }

            HudRoot.EnsureInstalled();
            suspicionLabel = new Label { pickingMode = PickingMode.Ignore };
            suspicionLabel.style.position = Position.Absolute;
            OutlinedText.Outline(suspicionLabel, GameFonts.Large, 1);
            OutlinedText.CenterHorizontally(suspicionLabel);
            suspicionLabel.style.display = DisplayStyle.None;
            HudRoot.WorldLabels.Add(suspicionLabel);
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

            // U.INC-T22: suspicion tuning comes from the validated settings registry, not a
            // serialized field duplicated on every NPC.
            Entity = session.World.AddNpc(new NpcSetup
            {
                Watching = player != null ? player.Id : default,
                StartX = transform.position.x / Px,
                Waypoints = waypoints,
                WalkSpeed = walkSpeed,
                ArriveRange = arriveRange,
                Suspicion = GameSettings.Tuning.Suspicion,
            });

            // U.INC-T9: a shot NPC is shoved 100px; the world owns the position, so the shove is a command.
            var target = GetComponent<Incognito.Weapon.BulletTarget>();
            if (target != null) target.Struck += OnStruck;
        }

        private SpriteRenderer Overlay(string name, int step)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0f, -OverlayZStep * step);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sharedMaterial = spriteRenderer.sharedMaterial;
            sr.sortingLayerID = spriteRenderer.sortingLayerID;
            sr.sortingOrder = spriteRenderer.sortingOrder;
            sr.enabled = false;
            return sr;
        }

        private void OnDestroy()
        {
            if (invertedCopy != null) Destroy(invertedCopy.sharedMaterial);
            suspicionLabel?.RemoveFromHierarchy();
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

            bool inverted = Hacking.HackingOverlay.ConsoleViewOn;
            if (invertedCopy != null)
            {
                invertedCopy.sprite = spriteRenderer.sprite;
                invertedCopy.flipX = spriteRenderer.flipX;
                invertedCopy.enabled = inverted;
            }

            if (brainSelectRenderer != null) brainSelectRenderer.enabled = inverted;

            if (HudRoot.TryWorldToPanel(Camera.main, transform.position + new Vector3(0f, LabelHeight * Px, 0f), out Vector2 at))
            {
                suspicionLabel.text = Entity.Suspicion.DisplayPercent + "%";
                suspicionLabel.style.left = at.x;
                suspicionLabel.style.top = at.y;
                suspicionLabel.style.display = DisplayStyle.Flex;
            }
            else
            {
                suspicionLabel.style.display = DisplayStyle.None;
            }
        }
    }
}
