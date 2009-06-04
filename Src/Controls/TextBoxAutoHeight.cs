using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace RT.Util.Controls
{
    /// <summary>
    /// Encapsulates a textbox which, when in multi-line and wordwrap mode, will automatically set its height to be precisely as necessary to accommodate the contained text.
    /// </summary>
    public class TextBoxAutoHeight : TextBox
    {
        /// <summary>Sets the specified bounds of the System.Windows.Forms.TextBoxBase control.</summary>
        protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified)
        {
            if (Multiline && WordWrap)
            {
                Size s = SizeFromClientSize(TextRenderer.MeasureText(Text.Length > 0 ? Text : "Wg", Font, new Size(width, height), TextFormatFlags.WordBreak));
                base.SetBoundsCore(x, y, width, s.Height + Margin.Vertical, BoundsSpecified.All);
            }
            else
                base.SetBoundsCore(x, y, width, height, specified);
        }

        /// <summary></summary>
        /// <param name="e"></param>
        protected override void OnTextChanged(EventArgs e)
        {
            if (Multiline && WordWrap)
                SetBoundsCore(Left, Top, Width, Height, BoundsSpecified.Size);
            base.OnTextChanged(e);
        }

        /// <summary>Raises the <see cref="System.Windows.Forms.Control.SizeChanged"/> event.</summary>
        /// <param name="e"></param>
        protected override void OnSizeChanged(EventArgs e)
        {
            if (Multiline && WordWrap)
                SetBoundsCore(Left, Top, Width, Height, BoundsSpecified.Size);
            base.OnSizeChanged(e);
        }
    }
}
