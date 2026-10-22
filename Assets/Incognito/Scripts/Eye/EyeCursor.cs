using UnityEngine;

namespace Incognito.Eye
{
    /// <summary>
    /// The eye-shaped mouse cursor (obj_mouse, RECONSTRUCTION.md §3): hides the OS cursor, follows
    /// the real mouse 1:1 as an in-world sprite drawn in front of everything (original depth
    /// -5000), and swaps its sprite by <see cref="Mode"/> — with the aim reticle overriding the
    /// mode while aiming. Scroll-wheel look/talk cycling is U.INC-T6; pointer mode is set by the
    /// NPC/console flow (U.INC-T7). Both just write <see cref="Mode"/>.
    /// </summary>
    [DefaultExecutionOrder(100)] // after the camera's LateUpdate, so the cursor never lags it
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class EyeCursor : MonoBehaviour
    {
        [SerializeField] private Camera viewCamera;
        [SerializeField] private EyeCursorMode mode = EyeCursorMode.Look;

        [Header("Sprites (spr_cursor_eye/talk/pointer/reticle)")]
        [SerializeField] private Sprite eyeSprite;
        [SerializeField] private Sprite talkSprite;
        [SerializeField] private Sprite pointerSprite;
        [SerializeField] private Sprite reticleSprite;

        [SerializeField] private int sortingOrder = 5000;

        public EyeCursorMode Mode { get => mode; set => mode = value; }

        /// <summary>Set by the weapon system (U.INC-T9) while aimmode == "aiming".</summary>
        public bool Aiming { get; set; }

        public EyeCursorSprite CurrentSprite => EyeCursorSprites.For(mode, Aiming);

        public Camera ViewCamera { get => viewCamera; set => viewCamera = value; }

        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = sortingOrder;
        }

        private void OnEnable() => Cursor.visible = false;
        private void OnDisable() => Cursor.visible = true;

        private void LateUpdate()
        {
            var cam = viewCamera != null ? viewCamera : Camera.main;
            if (cam == null) return;

            // x = mouse_x; y = mouse_y;
            Vector3 world = cam.ScreenToWorldPoint(Input.mousePosition);
            transform.position = new Vector3(world.x, world.y, transform.position.z);
            spriteRenderer.sprite = SpriteFor(CurrentSprite);
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
