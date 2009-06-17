using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace RT.Util.Controls
{
    /// <summary>
    /// Encapsulates a textbox which, when in multi-line and wordwrap mode, will automatically set its height to be precisely as necessary to accommodate the contained text.
    /// </summary>
    public class TextBoxAutoHeight : TextBox
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        static extern IntPtr SendMessage(IntPtr hWnd, Int32 Msg, IntPtr wParam, IntPtr lParam);

        /// <summary>Sets the specified bounds of the System.Windows.Forms.TextBoxBase control.</summary>
        protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified)
        {
            if (Multiline && WordWrap)
            {
                Size s = SizeFromClientSize(TextRenderer.MeasureText(Text.Length > 0 ? Text : "Wg", Font, new Size(width - Margin.Horizontal - Padding.Horizontal, height), TextFormatFlags.WordBreak));
                base.SetBoundsCore(x, y, width, s.Height + Margin.Vertical, BoundsSpecified.All);
            }
            else
                base.SetBoundsCore(x, y, width, height, specified);
        }

        protected override void WndProc(ref Message m)
        {
            // You won't need to scroll the textbox if its height is automatically set to accommodate all the text.
            // Therefore, catch mouse-scroll-wheel messages and passes them on to the parent.
            if (m.Msg == 0x20a)
                SendMessage(Parent.Handle, m.Msg, m.WParam, m.LParam);
            else
                base.WndProc(ref m);
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
