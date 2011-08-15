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
    public sealed class TextBoxAutoHeight : TextBox
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        static extern IntPtr SendMessage(IntPtr hWnd, Int32 Msg, IntPtr wParam, IntPtr lParam);

        private int _lastWidth;
        private int _lastWidthResult;

        /// <summary>Sets the specified bounds of the System.Windows.Forms.TextBoxBase control.</summary>
        protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified)
        {
            if (x != Left || y != Top || width != Width || height != Height)
            {
                if (Multiline && WordWrap)
                {
                    int newHeight;
                    if (_lastWidth != 0 && width == _lastWidth)
                        newHeight = _lastWidthResult;
                    else
                    {
                        newHeight = SizeFromClientSize(TextRenderer.MeasureText(Text.Length > 0 ? Text : "Wg", Font, new Size(width - Margin.Horizontal - Padding.Horizontal, height), TextFormatFlags.WordBreak)).Height;
                        _lastWidthResult = newHeight;
                        _lastWidth = width;
                    }
                    base.SetBoundsCore(x, y, width, newHeight + Margin.Vertical, BoundsSpecified.All);
                }
                else
                    base.SetBoundsCore(x, y, width, height, specified);
            }
        }

        /// <summary>Overrides the base class's WndProc message to capture mouse-wheel messages and pass them on to the GUI parent instead.</summary>
        /// <param name="m">A Windows Message object.</param>
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

        /// <summary>Gets or sets the current text in the <see cref="TextBoxAutoHeight"/>.</summary>
        public override string Text
        {
            get { return base.Text; }
            set
            {
                _lastWidth = 0;
                base.Text = value;
            }
        }

        /// <summary>Gets or sets the font of the text displayed by the control.</summary>
        public override Font Font
        {
            get { return base.Font; }
            set
            {
                _lastWidth = 0;
                base.Font = value;
            }
        }

        /// <summary>Override; see base.</summary>
        protected override void OnMarginChanged(EventArgs e)
        {
            _lastWidth = 0;
            base.OnMarginChanged(e);
        }

        /// <summary>Override; see base.</summary>
        protected override void OnPaddingChanged(EventArgs e)
        {
            _lastWidth = 0;
            base.OnPaddingChanged(e);
        }
    }
}
