namespace Incognito.Text
{
    /// <summary>
    /// Pure simulation of obj_float_text (scripts/show_float_text.gml and show_float_text_color.gml,
    /// U.INC-T31): a line of text that floats up from where it was shown and fades out. Its create
    /// event sets alpha = 1, fadespeed = 0.007 and floatspeed = 1; each 60Hz step, while alpha &gt; 0
    /// it fades by fadespeed and rises floatspeed px, and the first step that finds alpha at or
    /// below 0 destroys it. It is drawn centred (fa_center) in fnt_basic with a 1px outline, 10px
    /// above its own y (<see cref="DrawOffsetY"/>), white over black unless show_float_text_color
    /// gave other colours.
    /// </summary>
    public sealed class FloatTextState
    {
        /// <summary>alpha -= fadespeed per step.</summary>
        public const float FadeSpeed = 0.007f;

        /// <summary>y -= floatspeed per step, in original px.</summary>
        public const float FloatSpeed = 1f;

        /// <summary>draw_text_outline_color(x, y - 10, ...): drawn 10px above its y.</summary>
        public const float DrawOffsetY = -10f;

        /// <summary>The outline radius it is drawn with (draw_text_outline_color's argument2).</summary>
        public const int OutlineWidth = 1;

        public string Text { get; }

        /// <summary>The draw alpha, never below 0 (GM let it dip to -0.004 for one step; a negative
        /// draw alpha draws nothing, as 0 does).</summary>
        public float Alpha => alpha > 0f ? alpha : 0f;

        /// <summary>How far it has floated up from where it was shown, in original px (positive is up).</summary>
        public float Rise { get; private set; }

        /// <summary>True once it has destroyed itself.</summary>
        public bool Done { get; private set; }

        private float alpha = 1f;

        public FloatTextState(string text)
        {
            Text = text ?? string.Empty;
        }

        /// <summary>Runs <paramref name="steps"/> of obj_float_text's step event.</summary>
        public void Step(int steps)
        {
            for (int i = 0; i < steps && !Done; i++)
            {
                if (alpha > 0f)
                {
                    alpha -= FadeSpeed;
                    Rise += FloatSpeed;
                }
                else
                {
                    alpha = 0f;
                    Done = true;
                }
            }
        }
    }
}
