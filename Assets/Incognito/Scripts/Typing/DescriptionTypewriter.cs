using Incognito.UI;
using Incognito.World;
using UnityEngine;
using UnityEngine.UIElements;

namespace Incognito.Typing
{
    /// <summary>
    /// A floating, self-typing description (obj_typewriter via type_text.gml) anchored at a world
    /// point: the text types out a character every 2 steps, then fades and the object destroys
    /// itself. Drawn white with a black outline (draw_text_outline_alpha) over a half-transparent
    /// spr_desc_border box that starts 40px left of and 20px above the text, with a UI Toolkit label
    /// and background image (U.INC-T26 — was IMGUI, like the command prompt). Showing a text that is
    /// already on screen restarts it (type_text destroyed any typewriter printing identical text).
    /// </summary>
    public sealed class DescriptionTypewriter : MonoBehaviour
    {
        private const float BoxPadLeft = 40f;   // draw_sprite_ext(spr_desc_border, 0, x - 40, ...)
        private const float BoxPadTop = 20f;    // ..., y - 20, ...)
        private const float BoxAlpha = 0.5f;
        private const int FontSize = 24;        // fnt_large: High Tower Text 16pt

        public TypewriterState State { get; private set; }

        private Camera viewCamera;
        private VisualElement box;
        private Label textLabel;
        private bool measured;

        /// <summary>type_text(x, y, value).</summary>
        public static DescriptionTypewriter Show(Vector3 worldPosition, string text, Camera camera, Texture border)
        {
            foreach (var existing in FindObjectsByType<DescriptionTypewriter>(FindObjectsSortMode.None))
            {
                if (existing.State != null && existing.State.Text == text) Destroy(existing.gameObject);
            }

            var go = new GameObject("Description Typewriter");
            go.transform.position = worldPosition;
            var typewriter = go.AddComponent<DescriptionTypewriter>();
            typewriter.State = new TypewriterState(text);
            typewriter.viewCamera = camera;
            typewriter.BuildUi(text, border);
            return typewriter;
        }

        private void BuildUi(string text, Texture border)
        {
            HudRoot.EnsureInstalled();

            // Box and label are independent siblings (not parent/child) so their alpha fades
            // separately, as the original's two unrelated draw calls did (GUI.color per call).
            box = new VisualElement { pickingMode = PickingMode.Ignore };
            box.style.position = Position.Absolute;
            box.style.visibility = Visibility.Hidden; // hidden until sized from the full text
            box.style.backgroundSize = new BackgroundSize(Length.Percent(100f), Length.Percent(100f));
            if (border != null) box.style.backgroundImage = new StyleBackground((Texture2D)border);
            else box.style.backgroundColor = Color.black;
            HudRoot.WorldLabels.Add(box);

            textLabel = new Label(text) { pickingMode = PickingMode.Ignore };
            OutlinedText.Style(textLabel, FontSize);
            textLabel.style.position = Position.Absolute;
            textLabel.style.visibility = Visibility.Hidden;
            HudRoot.WorldLabels.Add(textLabel);

            // The box is sized once, from the full text, so it doesn't grow while typing.
            textLabel.RegisterCallback<GeometryChangedEvent>(OnFirstMeasure);
        }

        private void OnFirstMeasure(GeometryChangedEvent evt)
        {
            textLabel.UnregisterCallback<GeometryChangedEvent>(OnFirstMeasure);

            float w = evt.newRect.width;
            float h = evt.newRect.height;
            box.style.width = w + 2f * BoxPadLeft;
            box.style.height = h + 2f * BoxPadTop;
            textLabel.style.width = w;
            textLabel.style.height = h;
            textLabel.text = State.Shown;

            measured = true;
        }

        private void Update()
        {
            if (State == null) return;
            State.Step(Time.deltaTime);
            if (State.Done)
            {
                Destroy(gameObject);
                return;
            }

            if (!measured) return;
            textLabel.text = State.Shown;
            ApplyPosition();
        }

        private void OnDestroy()
        {
            box?.RemoveFromHierarchy();
            textLabel?.RemoveFromHierarchy();
        }

        private void ApplyPosition()
        {
            var cam = viewCamera != null ? viewCamera : Camera.main;
            if (cam == null || box.panel == null)
            {
                box.style.visibility = Visibility.Hidden;
                textLabel.style.visibility = Visibility.Hidden;
                return;
            }

            Vector3 screen = cam.WorldToScreenPoint(transform.position);
            Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(box.panel, new Vector2(screen.x, Screen.height - screen.y));

            box.style.left = panelPos.x - BoxPadLeft;
            box.style.top = panelPos.y - BoxPadTop;
            box.style.opacity = BoxAlpha * State.Alpha;
            box.style.visibility = Visibility.Visible;

            textLabel.style.left = panelPos.x;
            textLabel.style.top = panelPos.y;
            textLabel.style.opacity = State.Alpha;
            textLabel.style.visibility = Visibility.Visible;
        }
    }
}
