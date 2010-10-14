using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace RT.Util.Controls
{
    /// <summary>
    /// Implements a label control that supports basic formatting of the displayed text.
    /// Text alignment and right-to-left text are not supported.
    /// </summary>
    public class LabelEx : Control
    {
        private EggsNode _parsed;
        private int _cachedMeasuredWidth, _cachedMeasuredHeight;
        private char _mnemonic;

        /// <summary>Constructor.</summary>
        public LabelEx()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.Selectable | ControlStyles.FixedHeight, false);
            SetStyle(ControlStyles.ResizeRedraw, true);
            TabStop = false;
            AutoSize = true;
        }

        /// <summary>Text displayed in the label. EggsML supported: * to bold, / to italicize, _ to underline.</summary>
        [EditorAttribute("System.ComponentModel.Design.MultilineStringEditor, System.Design", "System.Drawing.Design.UITypeEditor")]
        [EditorBrowsable(EditorBrowsableState.Always)]
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public override string Text
        {
            get { return base.Text; }
            set { base.Text = value; }
        }

        /// <summary>Set to true to make the label size itself to fit all the text. Does not support wrapping of long lines and does not play well with Anchor/Dock.</summary>
        [EditorBrowsable(EditorBrowsableState.Always)]
        [Browsable(true)]
        [RefreshProperties(RefreshProperties.All)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [DefaultValue(true)]
        public override bool AutoSize
        {
            get { return base.AutoSize; }
            set
            {
                base.AutoSize = value;
                autosize();
            }
        }

        /// <summary>Override; see base.</summary>
        protected override void OnFontChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            _cachedMeasuredWidth = 0;
            autosize();
            Invalidate();
        }

        /// <summary>Override; see base.</summary>
        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            _parsed = EggsML.Parse(parseMnemonic(base.Text));
            _cachedMeasuredWidth = 0;
            autosize();
            Invalidate();
        }

        private string parseMnemonic(string text)
        {
            // The only legal ways to use & are:
            // &<char>, only once ever, where <char> can't be a space (to catch the common error of "This & that")
            // && any number of times
            // Anything else throws

            _mnemonic = '\0';
            int i = 0;
            while (i < text.Length)
            {
                if (text[i] != '&')
                    i++;
                else
                {
                    if (i + 1 >= text.Length)
                        throw new ArgumentException("LabelEx text cannot end in an unescaped \"&\" character.");
                    else
                    {
                        if (text[i + 1] == '&')
                            i += 2;
                        else
                        {
                            if (_mnemonic != '\0')
                                throw new ArgumentException("LabelEx text cannot have more than one mnemonic sequence (like \"&a\").");
                            if (text[i + 1] == ' ')
                                throw new ArgumentException("LabelEx text mnemonic cannot be a space character.");
                            _mnemonic = char.ToUpper(text[i + 1]);
                            text = text.Substring(0, i) + "`&" + text[i + 1] + "&`" + text.Substring(i + 2);
                            i += 2 /* start at i+2 in original text */ - 1 /* the ampersand removed */ + 4 /* the characters inserted */;
                        }
                    }
                }
            }
            return text;
        }

        private void autosize()
        {
            if (!AutoSize)
                return;
            Size = PreferredSize;
        }

        /// <summary>Override; see base.</summary>
        protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified)
        {
            if (AutoSize)
            {
                var preferredSize = PreferredSize;
                width = preferredSize.Width;
                height = preferredSize.Height;
            }
            base.SetBoundsCore(x, y, width, height, specified);
        }

        /// <summary>Override; see base.</summary>
        public override Size GetPreferredSize(Size constrainingSize)
        {
            return measure();
        }

        /// <summary>Override; see base.</summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            PaintLabel(e.Graphics, Enabled ? ForeColor : SystemColors.GrayText, Font);
        }

        /// <summary>Paints the formatted label text using the specified initial color and font for the text outside of any formatting tags.</summary>
        protected void PaintLabel(Graphics g, Color initialColor, Font initialFont)
        {
            _paintGr = g;
            _paintX = 0;
            _paintY = 0;
            _paintColor = initialColor;
            _doMeasure = false;
            doPaintOrMeasure(_parsed, initialFont, false);
        }

        private Size measure()
        {
            if (_cachedMeasuredWidth == 0)
                using (_paintGr = CreateGraphics())
                {
                    _paintX = 0;
                    _paintY = 0;
                    _doMeasure = true;
                    _cachedMeasuredWidth = 0;
                    _cachedMeasuredHeight = 0;
                    doPaintOrMeasure(_parsed, Font, false);
                }
            return new Size(_cachedMeasuredWidth, _cachedMeasuredHeight);
        }

        private Graphics _paintGr;
        private int _paintX, _paintY;
        private Color _paintColor;
        private bool _doMeasure;
        private int _lineStartGlyphOverhang;

        private void doPaintOrMeasure(EggsNode node, Font font, bool mnemonic)
        {
            if (node is EggsText)
            {
                var dummy = new Size(int.MaxValue, int.MaxValue); // the API requires a size to be specified in order to specify format flags..... argh!
                var glyphOverhang = (TextRenderer.MeasureText(_paintGr, "mm", font).Width - TextRenderer.MeasureText(_paintGr, "mm", font, dummy, TextFormatFlags.NoPadding).Width + 1) / 2;

                var lines = ((EggsText) node).Text.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Replace("\r", "");
                    var size = TextRenderer.MeasureText(_paintGr, line, font, dummy, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                    if (_doMeasure && _paintX == 0)
                        _lineStartGlyphOverhang = glyphOverhang;
                    if (!_doMeasure)
                    {
                        if (mnemonic)
                            TextRenderer.DrawText(_paintGr, "&" + line, font, new Point(_paintX, _paintY), _paintColor);
                        else
                            TextRenderer.DrawText(_paintGr, line, font, new Point(_paintX, _paintY), _paintColor, TextFormatFlags.NoPrefix);
                    }
                    _paintX += size.Width;
                    if (_doMeasure)
                        _cachedMeasuredWidth = Math.Max(_cachedMeasuredWidth, _lineStartGlyphOverhang + _paintX + glyphOverhang);
                    if (i + 1 < lines.Length)
                    {
                        _paintX = 0;
                        _paintY += size.Height;
                    }
                    if (_doMeasure)
                        _cachedMeasuredHeight = _paintY + size.Height;
                }
            }
            else if (node is EggsTag)
            {
                var n = (EggsTag) node;
                var substyle =
                    n.Tag == '*' ? font.Style | FontStyle.Bold :
                    n.Tag == '/' ? font.Style | FontStyle.Italic :
                    n.Tag == '_' ? font.Style | FontStyle.Underline : font.Style;

                using (var subfont = new Font(font, substyle))
                {
                    foreach (var subnode in n.Children)
                        doPaintOrMeasure(subnode, subfont, n.Tag == '&' && ShowKeyboardCues);
                }
            }
            else if (node is EggsGroup)
            {
                foreach (var subnode in ((EggsGroup) node).Children)
                    doPaintOrMeasure(subnode, font, mnemonic);
            }
        }

        /// <summary>Override; see base.</summary>
        protected override bool ProcessMnemonic(char charCode)
        {
            if (Enabled && Visible && _mnemonic != '\0' && _mnemonic == char.ToUpper(charCode) && Parent != null)
            {
                OnMnemonic();
                return true;
            }
            return false;
        }

        /// <summary>This method is called when the control responds to a mnemonic being pressed.</summary>
        protected virtual void OnMnemonic()
        {
            if (Parent != null)
                if (Parent.SelectNextControl(this, true, false, true, false) && !Parent.ContainsFocus)
                    Parent.Focus();
        }
    }

    /// <summary>
    /// Implements a link label control that supports basic formatting of the displayed text. Additionally, the text isn't
    /// underlined by default (allowing the mnemonic to be seen); a non-ugly hand cursor is used; and the font rendering
    /// isn't all screwed up like it sometimes is in LinkLabel.
    /// </summary>
    public class LinkLabelEx : LabelEx, IButtonControl
    {
        private Font _hoverFont;
        private bool _isHover;
        private bool _isDown;

        private static Cursor _cursorHand;

        /// <summary>Constructor</summary>
        public LinkLabelEx()
        {
            SetStyle(ControlStyles.Selectable, true);
            TabStop = true;
            ForeColor = SystemColors.HotTrack;
            ActiveColor = Color.Red;
            _hoverFont = new Font(Font, Font.Style | FontStyle.Underline);
            Cursor = _cursorHand;
        }

        static LinkLabelEx()
        {
            var handle = WinAPI.LoadCursor(IntPtr.Zero, 32649);
            if (handle == IntPtr.Zero)
                _cursorHand = Cursors.Hand;
            else
                try { _cursorHand = new Cursor(handle); }
                catch { _cursorHand = Cursors.Hand; }
        }

        /// <summary>Color of the text when the link is in normal state.</summary>
        [DefaultValue(typeof(Color), "HotTrack")]
        public override Color ForeColor
        {
            get { return base.ForeColor; }
            set { base.ForeColor = value; }
        }

        /// <summary>Color of the text when the link is in the "active" state - i.e. when the mouse button or Space is held down.</summary>
        [DefaultValue(typeof(Color), "Red")]
        public virtual Color ActiveColor { get; set; }

        /// <summary>Override; see base.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public override Cursor Cursor
        {
            get { return base.Cursor; }
            set { base.Cursor = value; }
        }

        /// <summary>Override; see base.</summary>
        protected override void OnFontChanged(EventArgs e)
        {
            _hoverFont = new Font(Font, Font.Style | FontStyle.Underline);
            base.OnFontChanged(e);
        }

        /// <summary>Override; see base.</summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            var color = !Enabled ? SystemColors.GrayText : _isDown ? ActiveColor : ForeColor;
            var font = _isHover ? _hoverFont : Font;
            PaintLabel(e.Graphics, color, font);
            if (Focused)
                ControlPaint.DrawFocusRectangle(e.Graphics, ClientRectangle);
        }

        /// <summary>Override; see base.</summary>
        protected override void OnGotFocus(EventArgs e)
        {
            Invalidate();
            base.OnGotFocus(e);
        }

        /// <summary>Override; see base.</summary>
        protected override void OnLostFocus(EventArgs e)
        {
            Invalidate();
            base.OnLostFocus(e);
        }

        /// <summary>Override; see base.</summary>
        protected override void OnEnabledChanged(EventArgs e)
        {
            Invalidate();
            base.OnEnabledChanged(e);
        }

        /// <summary>Override; see base.</summary>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            _isDown = true;
            Invalidate();
            Focus();
            base.OnMouseDown(e);
        }

        /// <summary>Override; see base.</summary>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            _isDown = false;
            Invalidate();
            base.OnMouseUp(e);
        }

        /// <summary>Override; see base.</summary>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Modifiers == Keys.None)
            {
                if (e.KeyCode == Keys.Enter)
                {
                    PerformClick();
                    e.Handled = true;
                    return;
                }
                else if (e.KeyCode == Keys.Space)
                {
                    _isDown = true;
                    Invalidate();
                    e.Handled = true;
                    return;
                }
            }
            base.OnKeyDown(e);
        }

        /// <summary>Override; see base.</summary>
        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space)
            {
                _isDown = false;
                Invalidate();
                PerformClick();
                e.Handled = true;
                return;
            }
            base.OnKeyUp(e);
        }

        /// <summary>Override; see base.</summary>
        protected override void OnMouseEnter(EventArgs e)
        {
            _isHover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        /// <summary>Override; see base.</summary>
        protected override void OnMouseLeave(EventArgs e)
        {
            _isHover = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        /// <summary>Override; see base.</summary>
        protected override void OnMnemonic()
        {
            PerformClick();
        }

        /// <summary>Gets or sets the value returned to the parent form when the button is clicked.</summary>
        public DialogResult DialogResult { get; set; }

        void IButtonControl.NotifyDefault(bool value)
        {
            // Do nothing if this label is a default Enter or Esc control.
        }

        /// <summary>Simulates the user clicking this link.</summary>
        public void PerformClick()
        {
            OnClick(EventArgs.Empty);
        }

        /// <summary>Override; see base.</summary>
        protected override void OnClick(EventArgs e)
        {
            var form = FindForm();
            if (form != null)
                form.DialogResult = DialogResult;
            base.OnClick(e);
        }
    }
}
