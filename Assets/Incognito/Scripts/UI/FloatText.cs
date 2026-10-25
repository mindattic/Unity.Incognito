using Incognito.Content;
using Incognito.Player;
using Incognito.Text;
using Incognito.World;
using UnityEngine;
using UnityEngine.UIElements;

namespace Incognito.UI
{
    /// <summary>
    /// obj_float_text, created by show_float_text(x, y, text) and show_float_text_color(x, y, text,
    /// colour, outline) (U.INC-T31): a line of fnt_basic text, centred on a point in the room, that
    /// floats up 1px and fades by 0.007 every 60Hz step (<see cref="FloatTextState"/>) and destroys
    /// itself once faded, drawn with draw_text_outline_color at a 1px outline as a UI Toolkit label
    /// on <see cref="HudRoot.WorldLabels"/>. The original's two scripts are its whole API; no live
    /// original object calls either, so nothing in the rebuild shows one yet either; it is here,
    /// ported, for the game's own use. Text is display text (<see cref="GmText.Expand"/> turns a GM
    /// string's '#' into a line break).
    /// </summary>
    public sealed class FloatText : MonoBehaviour
    {
        public FloatTextState State { get; private set; }

        private readonly StepClock clock = new StepClock();
        private Camera viewCamera;
        private Label label;

        /// <summary>show_float_text(x, y, text): white text over a black outline.</summary>
        public static FloatText Show(Vector3 worldPosition, string text, Camera camera = null)
        {
            return Show(worldPosition, text, Color.white, Color.black, camera);
        }

        /// <summary>show_float_text_color(x, y, text, colour, outline).</summary>
        public static FloatText Show(Vector3 worldPosition, string text, Color color, Color outline, Camera camera = null)
        {
            var go = new GameObject("Float Text");
            go.transform.position = worldPosition;
            var floatText = go.AddComponent<FloatText>();
            floatText.State = new FloatTextState(text);
            floatText.viewCamera = camera;
            floatText.BuildUi(color, outline);
            return floatText;
        }

        private void BuildUi(Color color, Color outline)
        {
            HudRoot.EnsureInstalled();
            label = new Label(State.Text) { pickingMode = PickingMode.Ignore };
            label.style.position = Position.Absolute;
            OutlinedText.OutlineColor(label, GameFonts.Basic, FloatTextState.OutlineWidth, color, outline);
            OutlinedText.CenterHorizontally(label);
            label.style.visibility = Visibility.Hidden; // until first placed
            HudRoot.WorldLabels.Add(label);
        }

        private void Update()
        {
            if (State == null) return;
            State.Step(clock.Advance(Time.deltaTime));
            if (State.Done)
            {
                Destroy(gameObject);
                return;
            }

            const float px = PlayerLocomotion.WorldUnitsPerOriginalPixel;
            // y -= floatspeed each step; drawn at y - 10 (original y grows down, world y up).
            Vector3 anchor = transform.position + new Vector3(0f, (State.Rise - FloatTextState.DrawOffsetY) * px, 0f);
            if (!HudRoot.TryWorldToPanel(viewCamera != null ? viewCamera : Camera.main, anchor, out Vector2 at))
            {
                label.style.visibility = Visibility.Hidden;
                return;
            }

            label.style.left = at.x;
            label.style.top = at.y;
            OutlinedText.SetAlpha(label, State.Alpha);
            label.style.visibility = Visibility.Visible;
        }

        private void OnDestroy()
        {
            label?.RemoveFromHierarchy();
        }
    }
}
