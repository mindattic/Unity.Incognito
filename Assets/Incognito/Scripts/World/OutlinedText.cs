using UnityEngine;
using UnityEngine.UIElements;

namespace Incognito.World
{
    /// <summary>
    /// Shared layout and style for the room name, door labels and other UI Toolkit text laid out on
    /// the original's 1920x1080 reference view (U.INC-T26 — was an IMGUI eight-direction outline
    /// hack). <see cref="HudRoot"/>'s panel is matched to that reference resolution, so these values
    /// are plain original pixels; the panel scales them to the real screen.
    /// </summary>
    public static class OutlinedText
    {
        public const float ReferenceHeight = 1080f;

        /// <summary>fnt_large (High Tower Text 16pt), as DescriptionTypewriter sizes it.</summary>
        public const int LargeFontSize = 24;

        /// <summary>Styles <paramref name="label"/> as the original's draw_text_outline /
        /// draw_text_outline_alpha: white text over a black outline, using the engine's own
        /// text-outline style instead of drawing eight offset copies by hand.</summary>
        public static void Style(Label label, int fontSize = LargeFontSize)
        {
            label.style.color = Color.white;
            label.style.unityTextOutlineColor = Color.black;
            label.style.unityTextOutlineWidth = 1f;
            label.style.fontSize = fontSize;
            label.style.whiteSpace = WhiteSpace.NoWrap;
        }
    }
}
