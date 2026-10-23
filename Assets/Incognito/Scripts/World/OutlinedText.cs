using UnityEngine;

namespace Incognito.World
{
    /// <summary>
    /// IMGUI stand-in for the original's draw_text_outline / draw_text_outline_alpha: white text
    /// over a 1px black outline, laid out on the 1920x1080 reference view and scaled to the screen.
    /// </summary>
    public static class OutlinedText
    {
        public const float ReferenceHeight = 1080f;

        /// <summary>fnt_large (High Tower Text 16pt), as DescriptionTypewriter sizes it.</summary>
        public const int LargeFontSize = 24;

        private static GUIStyle style;

        public static float Scale => Screen.height > 0 ? Screen.height / ReferenceHeight : 1f;

        public static GUIStyle Style(TextAnchor alignment)
        {
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label) { wordWrap = false, richText = false };
                style.normal.textColor = Color.white;
            }
            style.fontSize = Mathf.Max(1, Mathf.RoundToInt(LargeFontSize * Scale));
            style.alignment = alignment;
            return style;
        }

        /// <summary>Draws <paramref name="text"/> in <paramref name="rect"/> (screen pixels, GUI
        /// space) with an outline, at <paramref name="alpha"/>.</summary>
        public static void Draw(Rect rect, string text, TextAnchor alignment, float alpha)
        {
            if (string.IsNullOrEmpty(text) || alpha <= 0f) return;

            GUIStyle s = Style(alignment);
            var content = new GUIContent(text);
            float outline = Mathf.Max(1f, Scale);
            Color previous = GUI.color;

            GUI.color = new Color(0f, 0f, 0f, alpha);
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    GUI.Label(new Rect(rect.x + dx * outline, rect.y + dy * outline, rect.width, rect.height), content, s);
                }
            }

            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.Label(rect, content, s);
            GUI.color = previous;
        }
    }
}
