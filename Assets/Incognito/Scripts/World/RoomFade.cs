using UnityEngine;
using UnityEngine.SceneManagement;

namespace Incognito.World
{
    /// <summary>
    /// obj_fade: draws <see cref="RoomFadeState"/> as a black overlay over everything (the
    /// original's depth -100 GUI draw) and loads <see cref="RoomTransition.NextRoom"/> once a
    /// fade-in is opaque. Each room scene has one; it starts opaque and fades the room in.
    /// </summary>
    public sealed class RoomFade : MonoBehaviour
    {
        public RoomFadeState State { get; } = new RoomFadeState();

        private readonly StepClock clock = new StepClock();
        private Texture2D black;
        private bool loading;

        /// <summary>Fade to black, then load the pending room.</summary>
        public void FadeIn() => State.FadeIn();

        private void Update()
        {
            State.Step(clock.Advance(Time.deltaTime));

            if (State.ReadyToLoad && !loading && !string.IsNullOrEmpty(RoomTransition.NextRoom))
            {
                loading = true;
                SceneManager.LoadScene(RoomTransition.NextRoom);
            }
        }

        private void OnDestroy()
        {
            if (black != null) Destroy(black);
        }

        private void OnGUI()
        {
            if (State.Alpha <= 0f) return;
            if (black == null)
            {
                black = new Texture2D(1, 1);
                black.SetPixel(0, 0, Color.black);
                black.Apply();
            }

            GUI.depth = -100;
            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, State.Alpha);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), black);
            GUI.color = previous;
        }
    }
}
