using System;
using Incognito.Controls;
using Incognito.Eye;
using Incognito.Typing;
using UnityEngine;

namespace Incognito.Npc
{
    /// <summary>What a left click on an NPC does, by cursor mode (obj_npc left-pressed).</summary>
    public enum NpcClickAction { None, Describe, Talk, Pointer }

    public static class NpcClick
    {
        /// <summary>
        /// "look" types the NPC's description at the click, "talk" walks it over to the player,
        /// "pointer" opens the hacking overlay (U.INC-T7). Nothing while aiming: the click is the
        /// trigger then (the original ran both; a gunshot that also struck up a chat was a quirk).
        /// </summary>
        public static NpcClickAction ActionFor(EyeCursorMode mode, bool aiming)
        {
            if (aiming) return NpcClickAction.None;
            switch (mode)
            {
                case EyeCursorMode.Look: return NpcClickAction.Describe;
                case EyeCursorMode.Talk: return NpcClickAction.Talk;
                case EyeCursorMode.Pointer: return NpcClickAction.Pointer;
                default: return NpcClickAction.None;
            }
        }
    }

    /// <summary>
    /// obj_npc's left-click event (RECONSTRUCTION.md §4): reads the eye cursor's mode at click
    /// time and either types this NPC's <see cref="Description"/> where the cursor is (look),
    /// sends the NPC over to talk (talk), or raises <see cref="PointerClicked"/> for the hacking
    /// overlay (pointer, U.INC-T7). The description is per-NPC data, as in the original.
    /// </summary>
    [RequireComponent(typeof(NpcController))]
    [RequireComponent(typeof(Collider2D))]
    public sealed class NpcInteraction : MonoBehaviour
    {
        [SerializeField] private EyeCursor cursor;
        [SerializeField] private Camera viewCamera;

        [Tooltip("Typed at the click in look mode (the original's description; '#' became a newline).")]
        [TextArea(2, 6)]
        [SerializeField] private string description = string.Empty;

        [Tooltip("spr_desc_border, drawn half-transparent behind the description.")]
        [SerializeField] private Texture2D descriptionBorder;

        /// <summary>Clicked in pointer mode.</summary>
        public event Action<NpcInteraction> PointerClicked;

        /// <summary>Any NPC clicked in pointer mode; the hacking overlay (U.INC-T7,
        /// <see cref="Hacking.HackingOverlay"/>) subscribes.</summary>
        public static event Action<NpcInteraction> AnyPointerClicked;

        public string Description
        {
            get => description;
            set => description = value;
        }

        public EyeCursor Cursor
        {
            get => cursor;
            set => cursor = value;
        }

        private NpcController controller;
        private Collider2D hitArea;

        private void Awake()
        {
            controller = GetComponent<NpcController>();
            hitArea = GetComponent<Collider2D>();
        }

        private void Update()
        {
            if (!GameInput.Current.LeftClickPressed || cursor == null) return;
            // A click on the hacking overlay's icon or console panel is the GUI's, not the NPC's.
            if (Hacking.HackingOverlay.IsOverGui(GameInput.PointerScreenPosition)) return;

            var cam = viewCamera != null ? viewCamera : Camera.main;
            if (cam == null) return;

            Vector3 world = cam.ScreenToWorldPoint(GameInput.PointerScreenPosition);
            if (hitArea.OverlapPoint(world)) Interact(world);
        }

        /// <summary>A left click on this NPC at <paramref name="worldPoint"/>, acted on by the
        /// cursor's current mode.</summary>
        public void Interact(Vector2 worldPoint)
        {
            if (cursor == null) return;

            switch (NpcClick.ActionFor(cursor.Mode, cursor.Aiming))
            {
                case NpcClickAction.Describe:
                    if (!string.IsNullOrEmpty(description))
                    {
                        // type_text(mouse_x, mouse_y, description)
                        var cam = viewCamera != null ? viewCamera : Camera.main;
                        DescriptionTypewriter.Show(new Vector3(worldPoint.x, worldPoint.y, 0f), description, cam, descriptionBorder);
                    }
                    break;

                case NpcClickAction.Talk:
                    controller.StartTalking();
                    break;

                case NpcClickAction.Pointer:
                    PointerClicked?.Invoke(this);
                    AnyPointerClicked?.Invoke(this);
                    break;
            }
        }
    }
}
