using Incognito.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Incognito.World
{
    /// <summary>
    /// obj_fade: draws <see cref="RoomFadeState"/> as a black overlay over everything — the
    /// original's depth -100 GUI draw, now <see cref="HudRoot.Fade"/>, frontmost of every HUD layer
    /// (U.INC-T26 — was IMGUI) — and loads <see cref="RoomTransition.NextRoom"/> once a fade-in is
    /// opaque. Each room scene has one; it starts opaque and fades the room in.
    /// </summary>
    public sealed class RoomFade : MonoBehaviour
    {
        public RoomFadeState State { get; } = new RoomFadeState();

        private readonly StepClock clock = new StepClock();
        private VisualElement overlay;
        private bool loading;

        /// <summary>Fade to black, then load the pending room.</summary>
        public void FadeIn() => State.FadeIn();

        private void OnEnable()
        {
            HudRoot.EnsureInstalled();
            overlay = new VisualElement { pickingMode = PickingMode.Ignore };
            overlay.style.position = Position.Absolute;
            overlay.style.left = 0f;
            overlay.style.top = 0f;
            overlay.style.right = 0f;
            overlay.style.bottom = 0f;
            overlay.style.backgroundColor = Color.black;
            HudRoot.Fade.Add(overlay);
            Apply();
        }

        private void OnDisable()
        {
            overlay?.RemoveFromHierarchy();
        }

        private void Update()
        {
            State.Step(clock.Advance(Time.deltaTime));
            Apply();

            if (State.ReadyToLoad && !loading && !string.IsNullOrEmpty(RoomTransition.NextRoom))
            {
                loading = true;
                SceneManager.LoadScene(RoomTransition.NextRoom);
            }
        }

        private void Apply()
        {
            if (overlay == null) return;
            overlay.style.display = State.Alpha > 0f ? DisplayStyle.Flex : DisplayStyle.None;
            overlay.style.opacity = State.Alpha;
        }
    }
}
