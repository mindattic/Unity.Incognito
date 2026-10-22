using UnityEngine;

namespace Incognito.Typing
{
    /// <summary>
    /// A floating, self-typing description (obj_typewriter via type_text.gml) anchored at a world
    /// point: the text types out a character every 2 steps, then fades and the object destroys
    /// itself. Drawn white with a black outline (draw_text_outline_alpha) over a half-transparent
    /// spr_desc_border box that starts 40px left of and 20px above the text. Showing a text that
    /// is already on screen restarts it (type_text destroyed any typewriter printing identical
    /// text). Drawn with IMGUI, like the command prompt, until the art pass (U.INC-T11).
    /// </summary>
    public sealed class DescriptionTypewriter : MonoBehaviour
    {
        // Original layout, in original pixels on the 1920x1080 view.
        private const float ReferenceHeight = 1080f;
        private const float BoxPadLeft = 40f;   // draw_sprite_ext(spr_desc_border, 0, x - 40, ...)
        private const float BoxPadTop = 20f;    // ..., y - 20, ...)
        private const float BoxAlpha = 0.5f;
        private const int FontSize = 24;        // fnt_large: High Tower Text 16pt

        public TypewriterState State { get; private set; }

        private Camera viewCamera;
        private Texture border;
        private Texture2D fallbackBorder;
        private GUIStyle style;

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
            typewriter.border = border;
            return typewriter;
        }

        private void Update()
        {
            if (State == null) return;
            State.Step(Time.deltaTime);
            if (State.Done) Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (fallbackBorder != null) Destroy(fallbackBorder);
        }

        private void OnGUI()
        {
            if (State == null || State.Done) return;
            var cam = viewCamera != null ? viewCamera : Camera.main;
            if (cam == null) return;

            float scale = Screen.height / ReferenceHeight;
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label) { wordWrap = false, richText = false };
                style.normal.textColor = Color.white;
            }
            style.fontSize = Mathf.Max(1, Mathf.RoundToInt(FontSize * scale));

            Vector3 screen = cam.WorldToScreenPoint(transform.position);
            var origin = new Vector2(screen.x, Screen.height - screen.y); // GUI y runs downward

            // The box is sized once for the whole text, so it doesn't grow while typing.
            Vector2 full = style.CalcSize(new GUIContent(State.Text));
            var box = new Rect(
                origin.x - BoxPadLeft * scale,
                origin.y - BoxPadTop * scale,
                full.x + 2f * BoxPadLeft * scale,
                full.y + 2f * BoxPadTop * scale);

            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, BoxAlpha * State.Alpha);
            GUI.DrawTexture(box, BorderTexture(), ScaleMode.StretchToFill);

            var textRect = new Rect(origin.x, origin.y, full.x + 1f, full.y + 1f);
            var content = new GUIContent(State.Shown);
            float outline = Mathf.Max(1f, scale);

            GUI.color = new Color(0f, 0f, 0f, State.Alpha);
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    GUI.Label(new Rect(textRect.x + dx * outline, textRect.y + dy * outline, textRect.width, textRect.height), content, style);
                }
            }

            GUI.color = new Color(1f, 1f, 1f, State.Alpha);
            GUI.Label(textRect, content, style);
            GUI.color = previous;
        }

        private Texture BorderTexture()
        {
            if (border != null) return border;
            if (fallbackBorder == null)
            {
                fallbackBorder = new Texture2D(1, 1);
                fallbackBorder.SetPixel(0, 0, Color.black);
                fallbackBorder.Apply();
            }
            return fallbackBorder;
        }
    }
}
