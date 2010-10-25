using System;
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
        private Dictionary<Size, Size> _cachedPreferredSizes = new Dictionary<Size, Size>();

        /// <summary>Constructor.</summary>
        public LabelEx()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.Selectable | ControlStyles.FixedHeight, false);
            SetStyle(ControlStyles.ResizeRedraw, true);
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
            _parsed = EggsML.Parse(parseMnemonic(base.Text));
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
            if (!_cachedPreferredSizes.ContainsKey(constrainingSize))
                using (var g = CreateGraphics())
                    _cachedPreferredSizes[constrainingSize] = doPaintOrMeasure(g, _parsed, Font, ForeColor, false, constrainingSize == new Size() ? _dummySize : constrainingSize);
            return _cachedPreferredSizes[constrainingSize];
        }

        /// <summary>Override; see base.</summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            PaintLabel(e.Graphics, Enabled ? ForeColor : SystemColors.GrayText, Font);
        }

        /// <summary>Paints the formatted label text using the specified initial color and font for the text outside of any formatting tags.</summary>
        protected void PaintLabel(Graphics g, Color initialColor, Font initialFont)
        {
            doPaintOrMeasure(g, _parsed, initialFont, initialColor, true, ClientSize);
        }

        private class eggWalkData
        {
            public bool AtStartOfLine;
            public int X, Y;
            public List<string> WordPieces;
            public List<Font> WordPiecesFonts;
            public List<int> WordPiecesWidths;
            public int WordPiecesWidthsSum;
            public bool DoPaint;
            public Graphics Graphics;
            public Size ConstrainingSize, MeasuredSize, GlyphOverhang;
            public Dictionary<FontStyle, Size> SpaceSizes;
        }

        // TextRenderer.MeasureText() requires a useless size to be specified in order to specify format flags
        private Size _dummySize = new Size(int.MaxValue, int.MaxValue);

        private Size doPaintOrMeasure(Graphics g, EggsNode node, Font font, Color initialColor, bool doPaint, Size constrainingSize)
        {
            var glyphOverhang = TextRenderer.MeasureText(g, "Wg", font, _dummySize) - TextRenderer.MeasureText(g, "Wg", font, _dummySize, TextFormatFlags.NoPadding);
            var data = new eggWalkData
            {
                AtStartOfLine = true,
                X = glyphOverhang.Width,
                Y = glyphOverhang.Height,
                WordPieces = new List<string>(),
                WordPiecesFonts = new List<Font>(),
                WordPiecesWidths = new List<int>(),
                WordPiecesWidthsSum = 0,
                DoPaint = doPaint,
                Graphics = g,
                ConstrainingSize = constrainingSize,
                MeasuredSize = new Size(),
                GlyphOverhang = glyphOverhang,
                SpaceSizes = new Dictionary<FontStyle, Size>()
            };
            eggWalkWordWrap(node, 0, data, font, initialColor, false);

            if (data.WordPieces.Count > 0)
            {
                if (!data.AtStartOfLine)
                    data.X += TextRenderer.MeasureText(g, " ", font, _dummySize, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix).Width;
                renderText(data);
            }

            return new Size(
                data.MeasuredSize.Width + glyphOverhang.Width,
                data.MeasuredSize.Height + glyphOverhang.Height + data.SpaceSizes[font.Style].Height
            );
        }

        private void eggWalkWordWrap(EggsNode node, int hangingIndent, eggWalkData data, Font curFont, Color curColor, bool curNowrap)
        {
            if (!data.SpaceSizes.ContainsKey(curFont.Style))
                data.SpaceSizes[curFont.Style] = TextRenderer.MeasureText(data.Graphics, " ", curFont, _dummySize, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
            var tag = node as EggsTag;
            if (tag != null)
            {
                switch (tag.Tag)
                {
                    case '/': curFont = new Font(curFont, curFont.Style | FontStyle.Italic); break;
                    case '*': curFont = new Font(curFont, curFont.Style | FontStyle.Bold); break;
                    case '_':
                    case '&': curFont = new Font(curFont, curFont.Style | FontStyle.Underline); break;
                    case '+': curNowrap = true; break;
                }
                foreach (var child in tag.Children)
                    eggWalkWordWrap(child, hangingIndent, data, curFont, curColor, curNowrap);
            }
            else if (node is EggsText)
            {
                var txt = ((EggsText) node).Text;
                for (int i = 0; i < txt.Length; i++)
                {
                    int lengthOfWord = 0;
                    while (lengthOfWord + i < txt.Length && (curNowrap || !char.IsWhiteSpace(txt, lengthOfWord + i)) && txt[lengthOfWord + i] != '\n')
                        lengthOfWord++;

                    if (lengthOfWord > 0)
                    {
                    retry1:
                        string fragment = txt.Substring(i, lengthOfWord);
                        var fragmentWidth = TextRenderer.MeasureText(data.Graphics, fragment, curFont, _dummySize, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix).Width;
                    retry2:

                        if (data.AtStartOfLine && data.X + data.WordPiecesWidthsSum + fragmentWidth > data.ConstrainingSize.Width - data.GlyphOverhang.Width)
                        {
                            if (lengthOfWord > 1)
                            {
                                lengthOfWord /= 2;
                                goto retry1;
                            }
                            renderText(data);
                            data.WordPieces.Clear();
                            data.WordPiecesFonts.Clear();
                            data.WordPiecesWidths.Clear();
                            data.WordPiecesWidthsSum = 0;
                            data.X = hangingIndent + data.GlyphOverhang.Width;
                            data.Y += data.SpaceSizes[curFont.Style].Height;
                        }
                        else if (!data.AtStartOfLine && data.X + data.SpaceSizes[curFont.Style].Width + data.WordPiecesWidthsSum + fragmentWidth > data.ConstrainingSize.Width - data.GlyphOverhang.Width)
                        {
                            data.X = hangingIndent + data.GlyphOverhang.Width;
                            data.Y += data.SpaceSizes[curFont.Style].Height;
                            data.AtStartOfLine = true;
                            goto retry2;
                        }

                        if (data.WordPieces.Count == 0 || data.WordPiecesFonts[data.WordPiecesFonts.Count - 1] != curFont)
                        {
                            data.WordPieces.Add(fragment);
                            data.WordPiecesFonts.Add(curFont);
                            data.WordPiecesWidths.Add(fragmentWidth);
                        }
                        else
                        {
                            data.WordPieces[data.WordPieces.Count - 1] += fragment;
                            data.WordPiecesWidths[data.WordPiecesWidths.Count - 1] += fragmentWidth;
                        }
                        data.WordPiecesWidthsSum += fragmentWidth;
                        i += lengthOfWord - 1;
                        continue;
                    }

                    if (data.WordPieces.Count > 0)
                    {
                        if (!data.AtStartOfLine)
                            data.X += data.SpaceSizes[curFont.Style].Width;
                        renderText(data);
                        data.AtStartOfLine = false;
                    }
                    data.WordPieces.Clear();
                    data.WordPiecesFonts.Clear();
                    data.WordPiecesWidths.Clear();
                    data.WordPiecesWidthsSum = 0;
                    if (txt[i] == '\n')
                    {
                        data.X = data.GlyphOverhang.Width;
                        data.Y += data.SpaceSizes[curFont.Style].Height;
                        data.AtStartOfLine = true;
                    }
                }
            }
        }

        private void renderText(eggWalkData data)
        {
            for (int i = 0; i < data.WordPieces.Count; i++)
            {
                if (data.DoPaint)
                    TextRenderer.DrawText(data.Graphics, data.WordPieces[i], data.WordPiecesFonts[i], new Point(data.X, data.Y), ForeColor, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                data.X += data.WordPiecesWidths[i];
            }
            data.MeasuredSize = new Size(Math.Max(data.MeasuredSize.Width, data.X), data.Y);
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
