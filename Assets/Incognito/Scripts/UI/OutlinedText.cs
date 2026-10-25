using Incognito.Content;
using Incognito.Settings;
using UnityEngine;
using UnityEngine.UIElements;

namespace Incognito.UI
{
    /// <summary>
    /// The original's text drawing, as UI Toolkit styles (U.INC-T31; U.INC-T26 replaced the IMGUI
    /// eight-copy outline with a Label's native text outline). Every label the HUD draws picks one
    /// of the original's three fonts (<see cref="GameFonts"/>, sized from the validated
    /// <see cref="GameContent.Fonts"/> catalog) and one of its four draw calls:
    /// <list type="bullet">
    /// <item><see cref="Plain"/>: draw_text in the draw colour (obj_console's readout and output).</item>
    /// <item><see cref="Outline"/>: draw_text_outline(x, y, width, text), white over a black outline.</item>
    /// <item><see cref="OutlineAlpha"/>: draw_text_outline_alpha(x, y, width, text, alpha), the draw
    /// colour (white at every call) over black, the whole thing at <c>alpha</c>.</item>
    /// <item><see cref="OutlineColor"/>: draw_text_outline_color(x, y, width, text, colour, outline).</item>
    /// </list>
    /// The original drew the outline as (2w+1)² black copies of the text offset by up to
    /// <c>width</c> px in x and y, then the text on top; the engine's text outline is that same
    /// <c>width</c>-px black edge around the glyphs. All three fonts render as Arial (the HUD
    /// panel's built-in LegacyRuntime.ttf, see <see cref="GameFontDefinition"/>) at the original's
    /// pixel size. Values are plain original pixels on the 1920x1080 reference view that
    /// <see cref="HudRoot"/>'s panel scales to the screen. Text is display text: GM8 '#' line breaks
    /// were already expanded when the content loaded (<see cref="Incognito.Text.GmText"/>).
    /// </summary>
    public static class OutlinedText
    {
        public const float ReferenceHeight = 1080f;

        /// <summary>The font's size in reference pixels, from the content catalog; the original's
        /// own point size at its atlas DPI if the catalog lacks it (a hand-built test fixture).</summary>
        public static float SizeOf(string fontId)
        {
            GameFontDefinition font = GameSettings.Content.FindFont(fontId);
            if (font != null) return font.PixelSize;
            return new GameFontDefinition { PointSize = FallbackPointSize(fontId) }.PixelSize;
        }

        private static float FallbackPointSize(string fontId)
        {
            switch (fontId)
            {
                case GameFonts.Basic: return 12f;
                case GameFonts.System: return 8f;
                default: return 16f; // fnt_large
            }
        }

        /// <summary>draw_set_font: the label's size; the face is the panel's Arial.</summary>
        public static void SetFont(VisualElement element, string fontId)
        {
            element.style.fontSize = SizeOf(fontId);
        }

        /// <summary>draw_text: unoutlined text in <paramref name="color"/> (the draw colour; white
        /// at every live call). Wraps nothing: only explicit line breaks break a line.</summary>
        public static void Plain(Label label, string fontId, Color color)
        {
            SetFont(label, fontId);
            label.style.color = color;
            label.style.unityTextOutlineWidth = 0f;
            label.style.whiteSpace = WhiteSpace.Pre;
        }

        /// <summary>draw_text_outline(x, y, <paramref name="width"/>, text): white over black.</summary>
        public static void Outline(Label label, string fontId, int width)
        {
            OutlineColor(label, fontId, width, Color.white, Color.black);
        }

        /// <summary>draw_text_outline_alpha(x, y, <paramref name="width"/>, text, alpha): the draw
        /// colour (<paramref name="color"/>, white at every live call) over black; set the alpha each
        /// frame with <see cref="SetAlpha"/>.</summary>
        public static void OutlineAlpha(Label label, string fontId, int width, Color color)
        {
            OutlineColor(label, fontId, width, color, Color.black);
        }

        /// <summary>draw_text_outline_color(x, y, <paramref name="width"/>, text, colour, outline).</summary>
        public static void OutlineColor(Label label, string fontId, int width, Color color, Color outline)
        {
            SetFont(label, fontId);
            label.style.color = color;
            label.style.unityTextOutlineColor = outline;
            label.style.unityTextOutlineWidth = width;
            label.style.whiteSpace = WhiteSpace.Pre;
        }

        /// <summary>draw_set_alpha for the whole label, text and outline alike.</summary>
        public static void SetAlpha(VisualElement element, float alpha)
        {
            element.style.opacity = Mathf.Clamp01(alpha);
        }

        /// <summary>fa_center: the label's x is its centre, as draw_set_halign(fa_center) made it.</summary>
        public static void CenterHorizontally(VisualElement element)
        {
            element.style.translate = new Translate(Length.Percent(-50f), 0f);
            element.style.unityTextAlign = TextAnchor.UpperCenter;
        }
    }
}
