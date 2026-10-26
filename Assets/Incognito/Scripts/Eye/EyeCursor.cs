using Incognito.Controls;
using Incognito.Player;
using Incognito.Weapon;
using UnityEngine;

namespace Incognito.Eye
{
    /// <summary>
    /// The eye-shaped mouse cursor (obj_mouse, RECONSTRUCTION.md §3): hides the OS cursor, follows
    /// the real mouse 1:1 as an in-world sprite drawn in front of everything (original depth
    /// -5000), and swaps its sprite by <see cref="Mode"/> — with the aim reticle overriding the
    /// mode while aiming. The mouse wheel (and a right click while the gun is holstered) cycles
    /// look/talk (U.INC-T6); pointer mode is set by the NPC/console flow (U.INC-T7), which just
    /// writes <see cref="Mode"/>.
    /// </summary>
    [DefaultExecutionOrder(100)] // after the camera's LateUpdate, so the cursor never lags it
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class EyeCursor : MonoBehaviour
    {
        [SerializeField] private Camera viewCamera;
        [Tooltip("The player whose weapon decides the reticle (read from the world, never written).")]
        [SerializeField] private PlayerLocomotion player;
        [SerializeField] private EyeCursorMode mode = EyeCursorMode.Look;

        [Header("Sprites (spr_cursor_eye/talk/pointer/reticle)")]
        [SerializeField] private Sprite eyeSprite;
        [SerializeField] private Sprite talkSprite;
        [SerializeField] private Sprite pointerSprite;
        [SerializeField] private Sprite reticleSprite;

        [Tooltip("Its own sorting layer (ProjectSettings/TagManager.asset), frontmost of all of them, " +
            "so the cursor draws in front of everything regardless of sortingOrder elsewhere (U.INC-T24).")]
        [SerializeField] private string sortingLayer = "Cursor";
        [SerializeField] private int sortingOrder = 5000;

        public EyeCursorMode Mode { get => mode; set => mode = value; }

        /// <summary>obj_mouse's draw is <c>if (!over_gui) draw_self();</c>: the hacking overlay hides
        /// this cursor while its GUI arrow is up (<see cref="Hacking.GuiCursor"/>, U.INC-T39).</summary>
        public bool HiddenByGui { get; set; }

        /// <summary>The eye cursor is drawn this frame.</summary>
        public bool Visible => spriteRenderer != null && spriteRenderer.enabled;

        public PlayerLocomotion Player { get => player; set => player = value; }

        private PlayerWeaponState Weapon => player != null && player.Entity != null ? player.Entity.Weapon : null;

        /// <summary>The player's aimmode == "aiming" (U.INC-T9), read from the world.</summary>
        public bool Aiming => Weapon != null && Weapon.IsAiming;

        /// <summary>The gun is out (aimmode != "off"): a right click then aims rather than
        /// cycling look/talk. Read from the world.</summary>
        public bool WeaponDrawn => Weapon != null && Weapon.Mode != PlayerWeaponState.AimMode.Off;

        public EyeCursorSprite CurrentSprite => EyeCursorSprites.For(mode, Aiming);

        public Camera ViewCamera { get => viewCamera; set => viewCamera = value; }

        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.sortingLayerName = sortingLayer;
            spriteRenderer.sortingOrder = sortingOrder;
        }

        private void OnEnable() => Cursor.visible = false;
        private void OnDisable() => Cursor.visible = true;

        // Runs after PlayerWeapon.Update (execution order 100), and the weapon commands apply at once, so WeaponDrawn is this frame's.
        private void Update()
        {
            if (EyeCursorModes.ShouldCycle(GameInput.Current.WheelDeltaY, GameInput.Current.AimPressed, WeaponDrawn))
            {
                mode = EyeCursorModes.Cycle(mode);
            }
        }

        private void LateUpdate()
        {
            var cam = viewCamera != null ? viewCamera : Camera.main;
            if (cam == null) return;

            // x = mouse_x; y = mouse_y;
            Vector3 world = cam.ScreenToWorldPoint(GameInput.PointerScreenPosition);
            transform.position = new Vector3(world.x, world.y, transform.position.z);
            spriteRenderer.sprite = SpriteFor(CurrentSprite);
            spriteRenderer.enabled = !HiddenByGui;
        }

        private Sprite SpriteFor(EyeCursorSprite sprite)
        {
            switch (sprite)
            {
                case EyeCursorSprite.Reticle: return reticleSprite;
                case EyeCursorSprite.Pointer: return pointerSprite;
                case EyeCursorSprite.Talk: return talkSprite;
                default: return eyeSprite;
            }
        }
    }
}
