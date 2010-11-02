using System;
using System.Linq;
using System.Collections.Generic;
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
        private char _mnemonic;
        private Dictionary<int, Size> _cachedPreferredSizes = new Dictionary<int, Size>();
        private bool _wordWrap = false;

        /// <summary>Constructor.</summary>
        public LabelEx()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.Selectable | ControlStyles.FixedHeight, false);
            SetStyle(ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            TabStop = false;
            AutoSize = true;
        }

        /// <summary>Text displayed in the label. EggsML supported: * to bold, / to italicize, _ to underline, + for nowrap.</summary>
        [EditorAttribute("System.ComponentModel.Design.MultilineStringEditor, System.Design", "System.Drawing.Design.UITypeEditor")]
        [EditorBrowsable(EditorBrowsableState.Always)]
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public override string Text
        {
            get { return base.Text; }
            set { base.Text = value; }
        }

        /// <summary>Set to true to make the label size itself to fit all the text.</summary>
        [EditorBrowsable(EditorBrowsableState.Always)]
        [Browsable(true)]
        [RefreshProperties(RefreshProperties.All)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [DefaultValue(false)]
        public override bool AutoSize
        {
            get { return base.AutoSize; }
            set
            {
                base.AutoSize = value;
                autosize();
            }
        }

        /// <summary>Enable/disable wrapping long lines on word boundaries.</summary>
        [RefreshProperties(RefreshProperties.All)]
        [DefaultValue(false)]
        public virtual bool WordWrap
        {
            get { return _wordWrap; }
            set
            {
                _cachedPreferredSizes.Clear();
                _wordWrap = value;
                autosize();
                Invalidate();
            }
        }

        /// <summary>Override; see base.</summary>
        protected override void OnFontChanged(EventArgs e)
        {
            _cachedPreferredSizes.Clear();
            base.OnFontChanged(e);
            autosize();
            Invalidate();
        }

        /// <summary>Override; see base.</summary>
        protected override void OnTextChanged(EventArgs e)
        {
            _cachedPreferredSizes.Clear();
            base.OnTextChanged(e);
            _parsed = EggsML.Parse(base.Text);
            _mnemonic = char.ToUpperInvariant(parseMnemonic(_parsed));
            autosize();
            Invalidate();
        }

        private char parseMnemonic(EggsNode node)
        {
            // The only legal way to use & is as a tag containing a single character. For example:
            // &A&ssembly    (mnemonic is 'A')
            // Ob&f&uscate   (mnemonic is 'F')

            var tag = node as EggsTag;
            if (tag == null)
                return '\0';
            if (tag.Tag == '&')
            {
                if (tag.Children.Count != 1 || !(tag.Children.First() is EggsText) || ((EggsText) tag.Children.First()).Text.Length != 1)
                    throw new ArgumentException("'&' mnemonic tag must not contain anything other than a single character.");
                return ((EggsText) tag.Children.First()).Text[0];
            }
            else
                return tag.Children.Select(c => parseMnemonic(c)).FirstOrDefault(c => c != '\0');
        }

        private void autosize()
        {
            if (!AutoSize)
                return;
            Size = PreferredSize;
        }

        /// <summary>Override; see base.</summary>
        public override Size GetPreferredSize(Size constrainingSize)
        {
            if (!_cachedPreferredSizes.ContainsKey(constrainingSize.Width))
                using (var g = CreateGraphics())
                    _cachedPreferredSizes[constrainingSize.Width] = doPaintOrMeasure(g, _parsed, Font, ForeColor, false, constrainingSize.Width == 0 ? int.MaxValue : constrainingSize.Width);
            return _cachedPreferredSizes[constrainingSize.Width];
        }

        /// <summary>Override; see base.</summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            PaintLabel(e.Graphics, Enabled ? ForeColor : SystemColors.GrayText, Font);
        }

        /// <summary>Paints the formatted label text using the specified initial color and font for the text outside of any formatting tags.</summary>
        protected void PaintLabel(Graphics g, Color color, Font initialFont)
        {
            doPaintOrMeasure(g, _parsed, initialFont, color, true, ClientSize.Width);
        }

        // TextRenderer.MeasureText() requires a useless size to be specified in order to specify format flags
        private static Size _dummySize = new Size(int.MaxValue, int.MaxValue);

        private Size doPaintOrMeasure(Graphics g, EggsNode node, Font initialFont, Color foreColor, bool doPaint, int constrainingWidth)
        {
            int x = 0, y = 0, maxX = 0;
            var glyphOverhang = TextRenderer.MeasureText(g, "Wg", initialFont, _dummySize) - TextRenderer.MeasureText(g, "Wg", initialFont, _dummySize, TextFormatFlags.NoPadding);

            var spaceSizes = new Dictionary<FontStyle, Size>();
            Func<Font, Size> spaceSize = font =>
            {
                if (!spaceSizes.ContainsKey(font.Style))
                    spaceSizes[font.Style] = TextRenderer.MeasureText(g, " ", font, _dummySize, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                return spaceSizes[font.Style];
            };

            int wrapWidth = WordWrap ? Math.Max(1, constrainingWidth - glyphOverhang.Width) : int.MaxValue;
            int hangingIndent = 0;
            EggsML.WordWrap<Font>(node, initialFont, wrapWidth, hangingIndent,
                (text, font) => (text == " " ? spaceSize(font) : TextRenderer.MeasureText(g, text, font, _dummySize, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix)).Width,
                (text, font, width) =>
                {
                    if (doPaint && !string.IsNullOrWhiteSpace(text))
                        TextRenderer.DrawText(g, text, font, new Point(x + glyphOverhang.Width / 2, y + glyphOverhang.Height / 2), foreColor, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                    x += width;
                    maxX = Math.Max(maxX, x);
                },
                (font, isHanging) => { x = isHanging ? hangingIndent : 0; y += spaceSize(font).Height; },
                (font, tag) =>
                {
                    switch (tag)
                    {
                        case '/': return new Font(font, font.Style | FontStyle.Italic);
                        case '*': return new Font(font, font.Style | FontStyle.Bold);
                        case '_': return new Font(font, font.Style | FontStyle.Underline);
                        case '&':
                            if (ShowKeyboardCues)
                                return new Font(font, font.Style | FontStyle.Underline);
                            return font;
                    }
                    return font;
                });
            return new Size(maxX + glyphOverhang.Width, y + spaceSize(initialFont).Height + glyphOverhang.Height);
        }

        /// <summary>Override; see base.</summary>
        protected override bool ProcessMnemonic(char charCode)
        {
            if (Enabled && Visible && _mnemonic == char.ToUpperInvariant(charCode) && Parent != null)
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
