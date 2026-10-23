using Incognito.Eye;
using Incognito.Typing;
using UnityEngine;

namespace Incognito.World
{
    /// <summary>
    /// A clickable prop (obj_prop_lamppost / obj_prop_mailbox left-pressed): a left click on it
    /// types its description at the cursor, type_text(mouse_x, mouse_y, description), in any
    /// cursor mode. As with NPCs (U.INC-T6), nothing while aiming — that click is the trigger.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class PropDescription : MonoBehaviour
    {
        [TextArea(2, 6)]
        [SerializeField] private string description = string.Empty;
        [SerializeField] private EyeCursor cursor;
        [SerializeField] private Camera viewCamera;
        [SerializeField] private Texture2D descriptionBorder;

        public string Description => description;

        private Collider2D hitArea;

        public void Configure(string description, EyeCursor cursor, Camera viewCamera, Texture2D descriptionBorder)
        {
            this.description = description;
            this.cursor = cursor;
            this.viewCamera = viewCamera;
            this.descriptionBorder = descriptionBorder;
        }

        private void Awake()
        {
            hitArea = GetComponent<Collider2D>();
        }

        private void Update()
        {
            if (!Input.GetMouseButtonDown(0) || string.IsNullOrEmpty(description)) return;
            if (cursor != null && cursor.Aiming) return;

            var cam = viewCamera != null ? viewCamera : Camera.main;
            if (cam == null) return;

            Vector3 world = cam.ScreenToWorldPoint(Input.mousePosition);
            if (!hitArea.OverlapPoint(world)) return;
            DescriptionTypewriter.Show(new Vector3(world.x, world.y, 0f), description, cam, descriptionBorder);
        }
    }
}
