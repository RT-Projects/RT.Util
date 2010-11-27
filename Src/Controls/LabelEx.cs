using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using RT.Util.ExtensionMethods;

namespace RT.Util.Controls
{
    /// <summary>
    /// Implements a label control that supports basic formatting of the displayed text.
    /// Text alignment and right-to-left text are not supported.
    /// </summary>
    public class LabelEx : Control
    {
        /// <summary>Contains values that specify the unit in which the value of the <see cref="HangingIndent"/> property is measured.</summary>
        public enum IndentUnit
        {
            /// <summary>The indent is a multiple of the size of a space character.</summary>
            Spaces,
            /// <summary>The indent is measured in pixels.</summary>
            Pixels
        }

        /// <summary>Occurs when a link within the label is activated (via mouse click, Space or Enter).</summary>
        public event LinkEventHandler LinkActivated;
        /// <summary>Occurs when a link within the label gets focussed by the keyboard.</summary>
        public event LinkEventHandler LinkGotFocus;
        /// <summary>Occurs when a link within the label loses the keyboard focus.</summary>
        public event LinkEventHandler LinkLostFocus;

        /// <summary>Constructor.</summary>
        public LabelEx()
        {
            _parsed = EggsML.Parse("");
            DoubleBuffered = true;
            SetStyle(ControlStyles.FixedHeight, false);
            SetStyle(ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);

            // Set default properties
            setTabStop(false);
            AutoSize = true;
            LinkColor = SystemColors.HotTrack;
            LinkActiveColor = Color.Red;
        }

        /// <summary>Text displayed in the label. See remarks for EggsML supported.</summary>
        /// <remarks>
        /// <list type="bullet">
        ///     <item><description><c>*</c> = bold.</description></item>
        ///     <item><description><c>/</c> = italics.</description></item>
        ///     <item><description><c>_</c> = underline.</description></item>
        ///     <item><description><c>+</c> = marks a section of text as nowrap.</description></item>
        ///     <item><description><c>&lt;XYZ&gt;{ ... }</c> = make the text enclosed in curlies a link. XYZ can be any string and will be passed in for the <see cref="LinkActivated"/> event.</description></item>
        ///     <item><description><c>&lt;XYZ&gt;= ... =</c> = colour the text enclosed in equals sign in the colour designated by XYZ (e.g. “Red”).</description></item>
        ///     <item><description><c>[ ... ]</c> = adds a bullet point to the beginning of a paragraph. (Enclose the entire paragraph without its trailing newline, otherwise behaviour is weird.)</description></item>
        /// </list>
        /// </remarks>
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

        /// <summary>Enable/disable wrapping long lines on word boundaries.</summary>
        [RefreshProperties(RefreshProperties.All)]
        [DefaultValue(false)]
        public virtual bool WordWrap
        {
            get { return _wordWrap; }
            set
            {
                if (_wordWrap == value)
                    return;
                _cachedPreferredSizes.Clear();
                _cachedRendering = null;
                _wordWrap = value;
                autosize();
                Invalidate();
            }
        }

        /// <summary>Specifies the line spacing between paragraph as a multiple of the line height.</summary>
        [RefreshProperties(RefreshProperties.All)]
        [DefaultValue(0d)]
        public virtual double ParagraphSpacing
        {
            get { return _paragraphSpacing; }
            set
            {
                if (_paragraphSpacing == value)
                    return;
                if (value < 0)
                    throw new ArgumentException("ParagraphSpacing cannot be negative.");
                _cachedPreferredSizes.Clear();
                _cachedRendering = null;
                _paragraphSpacing = value;
                autosize();
                Invalidate();
            }
        }

        /// <summary>Specifies an amount by which all but the first line of each paragraph are indented.</summary>
        /// <seealso cref="HangingIndentUnit"/>
        [RefreshProperties(RefreshProperties.All)]
        [DefaultValue(0)]
        public virtual int HangingIndent
        {
            get { return _hangingIndent; }
            set
            {
                if (_hangingIndent == value)
                    return;
                _cachedPreferredSizes.Clear();
                _cachedRendering = null;
                _hangingIndent = value;
                autosize();
                Invalidate();
            }
        }

        /// <summary>Specifies the units in which <see cref="HangingIndent"/> is measured.</summary>
        [RefreshProperties(RefreshProperties.All)]
        [DefaultValue(IndentUnit.Spaces)]
        public virtual IndentUnit HangingIndentUnit
        {
            get { return _hangingIndentUnit; }
            set
            {
                if (_hangingIndentUnit == value)
                    return;
                _cachedPreferredSizes.Clear();
                _cachedRendering = null;
                _hangingIndentUnit = value;
                autosize();
                Invalidate();
            }
        }

        /// <summary>Color of text in a link (can be overridden using a <c>{ ... }</c> tag).</summary>
        [DefaultValue(typeof(Color), "HotTrack")]
        public virtual Color LinkColor { get; set; }

        /// <summary>Color of text when a link is in the “active” state, i.e. when the mouse button or Space is held down.</summary>
        [DefaultValue(typeof(Color), "Red")]
        public virtual Color LinkActiveColor { get; set; }

        /// <summary>Contains information about a parse error that describes in what way the current value of
        /// <see cref="Text"/> is invalid EggsML, or null if it is valid.</summary>
        public EggsMLParseException ParseError { get; private set; }

        private class renderingInfo
        {
            public string Text;
            public Font Font;
            public Rectangle Rectangle;
            public Color Color;
            public int? LinkNumber;
            public renderingInfo(string text, Font font, Rectangle location, Color color, int? linkNumber) { Text = text; Font = font; Rectangle = location; Color = color; LinkNumber = linkNumber; }
        }

        private class linkLocationInfo
        {
            public string LinkID;
            public List<Rectangle> Rectangles = new List<Rectangle>();
        }

        private const string BULLET = " • ";
        private static ColorConverter _colorConverter;
        private static Cursor _cursorHandCache;
        private static Cursor _cursorHand
        {
            get
            {
                if (_cursorHandCache == null)
                {
                    var handle = WinAPI.LoadCursor(IntPtr.Zero, 32649);
                    if (handle == IntPtr.Zero)
                        _cursorHandCache = Cursors.Hand;
                    else
                        try { _cursorHandCache = new Cursor(handle); }
                        catch { _cursorHandCache = Cursors.Hand; }
                }
                return _cursorHandCache;
            }
        }

        private Dictionary<int, Size> _cachedPreferredSizes = new Dictionary<int, Size>();
        private List<renderingInfo> _cachedRendering;
        private Color _cachedRenderingColor;
        private int _cachedRenderingWidth;
        private bool _callGotFocusAfterPaint;
        private bool _formJustDeactivated;
        private int _hangingIndent = 0;
        private IndentUnit _hangingIndentUnit = IndentUnit.Spaces;
        private int? _keyboardFocusOnLinkNumberPrivate;
        private bool _lastHadFocus;
        private List<linkLocationInfo> _linkLocations;
        private char _mnemonic;
        private bool _mouseIsDownOnLink;
        private int? _mouseOnLinkNumber;
        private double _paragraphSpacing = 0d;
        private List<Control> _parentChain = new List<Control>();
        private EggsNode _parsed;
        private bool _spaceIsDownOnLink;
        private bool _wordWrap = false;

        private int? _keyboardFocusOnLinkNumber
        {
            get { return _keyboardFocusOnLinkNumberPrivate; }
            set
            {
                if (_keyboardFocusOnLinkNumberPrivate == value)
                    return;
                if (LinkLostFocus != null && _keyboardFocusOnLinkNumberPrivate != null)
                    LinkLostFocus(this, new LinkEventArgs(_linkLocations[_keyboardFocusOnLinkNumberPrivate.Value].LinkID, _linkLocations[_keyboardFocusOnLinkNumberPrivate.Value].Rectangles));
                _keyboardFocusOnLinkNumberPrivate = value;
                Invalidate();
                if (LinkGotFocus != null && _keyboardFocusOnLinkNumberPrivate != null)
                    LinkGotFocus(this, new LinkEventArgs(_linkLocations[_keyboardFocusOnLinkNumberPrivate.Value].LinkID, _linkLocations[_keyboardFocusOnLinkNumberPrivate.Value].Rectangles));
            }
        }

        // TextRenderer.MeasureText() requires a useless size to be specified in order to specify format flags
        private static Size _dummySize = new Size(int.MaxValue, int.MaxValue);

        /// <summary>Override; see base.</summary>
        protected override void OnFontChanged(EventArgs e)
        {
            _cachedPreferredSizes.Clear();
            _cachedRendering = null;
            base.OnFontChanged(e);
            autosize();
            Invalidate();
        }

        /// <summary>Override; see base.</summary>
        protected override void OnEnabledChanged(EventArgs e)
        {
            _cachedRendering = null;
            Invalidate();
            base.OnEnabledChanged(e);
        }

        /// <summary>Override; see base.</summary>
        protected override void OnTextChanged(EventArgs e)
        {
            _cachedPreferredSizes.Clear();
            _cachedRendering = null;
            base.OnTextChanged(e);
            _mnemonic = '\0';
            setTabStop(false);
            var origText = base.Text;
            try
            {
                _parsed = EggsML.Parse(origText);
                ParseError = null;

                // We know there are no mnemonics or links in the exception message, so only do this if there was no parse error
                extractMnemonicEtc(_parsed);
            }
            catch (EggsMLParseException epe)
            {
                ParseError = epe;
                var msg = "";
                int ind = 0;
                if (epe.FirstIndex != null)
                {
                    ind = epe.FirstIndex.Value;
                    msg += EggsML.Escape(origText.Substring(0, ind));
                    msg += "<Red>={0}=".Fmt(EggsML.Escape(origText.Substring(ind, 1)));
                    ind++;
                }
                msg += EggsML.Escape(origText.Substring(ind, epe.Index - ind));
                ind = epe.Index;
                if (epe.Length > 0)
                {
                    msg += "<Red>={0}=".Fmt(EggsML.Escape(origText.Substring(ind, epe.Length)));
                    ind += epe.Length;
                }
                msg += "<Red>= ← (" + EggsML.Escape(epe.Message) + ")=";
                msg += EggsML.Escape(origText.Substring(ind));
                _parsed = EggsML.Parse(msg);
            }
            autosize();
            if (Focused && TabStop)
                _keyboardFocusOnLinkNumber = 0;
            else
                _keyboardFocusOnLinkNumber = null;
            Invalidate();
        }

        private void extractMnemonicEtc(EggsNode node)
        {
            // The only legal way to use & is as a tag containing a single character. For example:
            // &F&ile       (mnemonic is 'F')
            // O&p&en    (mnemonic is 'P')

            var tag = node as EggsTag;
            if (tag == null)
                return;
            if (tag.Tag == '&')
            {
                if (tag.Children.Count != 1 || !(tag.Children.First() is EggsText) || ((EggsText) tag.Children.First()).Text.Length != 1)
                    throw new ArgumentException("'&' mnemonic tag must not contain anything other than a single character.");
                _mnemonic = char.ToUpperInvariant(((EggsText) tag.Children.First()).Text[0]);
            }
            else if (tag.Tag == '{')
            {
                // Deliberately skip the inside of links: don’t wanna interpret their mnemonics as the main mnemonic
                setTabStop(true);
            }
            else
            {
                foreach (var child in tag.Children)
                {
                    if (TabStop && _mnemonic != '\0')
                        return;
                    extractMnemonicEtc(child);
                }
            }
        }

        private void autosize()
        {
            if (!AutoSize)
                return;
            Size = PreferredSize;
        }

        private void setTabStop(bool tabStop)
        {
            if (tabStop == TabStop)
                return;
            TabStop = tabStop;
            SetStyle(ControlStyles.Selectable, tabStop);
        }

        /// <summary>Override; see base.</summary>
        public override Size GetPreferredSize(Size constrainingSize)
        {
            if (!_cachedPreferredSizes.ContainsKey(constrainingSize.Width))
                using (var g = CreateGraphics())
                    _cachedPreferredSizes[constrainingSize.Width] = doPaintOrMeasure(g, _parsed, Font, ForeColor, constrainingSize.Width == 0 ? int.MaxValue : constrainingSize.Width);
            return _cachedPreferredSizes[constrainingSize.Width];
        }

        /// <summary>Override; see base.</summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            PaintLabel(e.Graphics, Enabled ? ForeColor : SystemColors.GrayText, Font);
            if (_callGotFocusAfterPaint)
            {
                OnGotFocus(e);
                _callGotFocusAfterPaint = false;
            }
        }

        /// <summary>Paints the formatted label text using the specified initial color and font for the text outside of any formatting tags.</summary>
        protected void PaintLabel(Graphics g, Color initialColor, Font initialFont)
        {
            if (_cachedRendering == null || _cachedRenderingWidth != ClientSize.Width || _cachedRenderingColor != initialColor)
            {
                _cachedRendering = new List<renderingInfo>();
                _cachedRenderingWidth = ClientSize.Width;
                _cachedRenderingColor = initialColor;
                _linkLocations = new List<linkLocationInfo>();
                doPaintOrMeasure(g, _parsed, initialFont, initialColor, _cachedRenderingWidth, _cachedRendering, _linkLocations);
                if (_keyboardFocusOnLinkNumber != null && _keyboardFocusOnLinkNumber.Value >= _linkLocations.Count)
                    _keyboardFocusOnLinkNumber = _linkLocations.Count == 0 ? (int?) null : _linkLocations.Count - 1;
            }
            foreach (var item in _cachedRendering)
            {
                TextRenderer.DrawText(g, item.Text,
                    _mouseOnLinkNumber != null && _mouseOnLinkNumber == item.LinkNumber ? new Font(item.Font, item.Font.Style | FontStyle.Underline) : item.Font,
                    item.Rectangle.Location,
                    (_mouseIsDownOnLink && _mouseOnLinkNumber == item.LinkNumber) ||
                    (_spaceIsDownOnLink && _keyboardFocusOnLinkNumber == item.LinkNumber)
                        ? LinkActiveColor : item.Color,
                    TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
            }
            if (_keyboardFocusOnLinkNumber != null)
                foreach (var rectangle in _linkLocations[_keyboardFocusOnLinkNumber.Value].Rectangles)
                    ControlPaint.DrawFocusRectangle(g, rectangle);
        }

        private class renderState
        {
            public Font Font { get; private set; }
            public Color Color { get; private set; }
            public int BlockIndent { get; private set; }
            public int? LinkNumber { get; private set; }
            public renderState(Font initialFont, Color initialColor) { Font = initialFont; Color = initialColor; BlockIndent = 0; LinkNumber = null; }
            private renderState(Font font, Color color, int blockIndent, int? linkNumber) { Font = font; Color = color; BlockIndent = blockIndent; LinkNumber = linkNumber; }
            public renderState ChangeFont(Font newFont) { return new renderState(newFont, Color, BlockIndent, LinkNumber); }
            public renderState ChangeColor(Color newColor) { return new renderState(Font, newColor, BlockIndent, LinkNumber); }
            public renderState ChangeBlockIndent(int newIndent) { return new renderState(Font, Color, newIndent, LinkNumber); }
            public renderState ChangeLinkNumberAndColor(int? newLinkNumber, Color newColor) { return new renderState(Font, newColor, BlockIndent, newLinkNumber); }
        }

        private Size doPaintOrMeasure(Graphics g, EggsNode node, Font initialFont, Color initialForeColor, int constrainingWidth,
            List<renderingInfo> renderings = null, List<linkLocationInfo> linkRenderings = null)
        {
            var glyphOverhang = TextRenderer.MeasureText(g, "Wg", initialFont, _dummySize) - TextRenderer.MeasureText(g, "Wg", initialFont, _dummySize, TextFormatFlags.NoPadding);

            var spaceSizes = new Dictionary<FontStyle, Size>();
            Func<Font, Size> spaceSize = font =>
            {
                if (!spaceSizes.ContainsKey(font.Style))
                    spaceSizes[font.Style] = TextRenderer.MeasureText(g, " ", font, _dummySize, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                return spaceSizes[font.Style];
            };

            Dictionary<FontStyle, int> bulletSizes = null;
            Func<Font, int> bulletSize = font =>
            {
                if (bulletSizes == null)
                    bulletSizes = new Dictionary<FontStyle, int>();
                if (!bulletSizes.ContainsKey(font.Style))
                    bulletSizes[font.Style] = TextRenderer.MeasureText(g, BULLET, font, _dummySize, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix).Width;
                return bulletSizes[font.Style];
            };

            int x = glyphOverhang.Width / 2, y = glyphOverhang.Height / 2;
            int wrapWidth = WordWrap ? Math.Max(1, constrainingWidth - glyphOverhang.Width) : int.MaxValue;
            int hangingIndent = _hangingIndent * (_hangingIndentUnit == IndentUnit.Spaces ? spaceSize(initialFont).Width : 1);
            bool atBeginningOfLine = false;

            int actualWidth = EggsML.WordWrap(node, new renderState(initialFont, initialForeColor), wrapWidth,
                (state, text) => (text == " " ? spaceSize(state.Font) : TextRenderer.MeasureText(g, text, state.Font, _dummySize, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix)).Width,
                (state, text, width) =>
                {
                    if (renderings != null && !string.IsNullOrEmpty(text))
                    {
                        renderingInfo info;
                        if (!atBeginningOfLine && renderings.Count > 0 && (info = renderings[renderings.Count - 1]).Color == state.Color && info.Font == state.Font && info.LinkNumber == state.LinkNumber)
                        {
                            info.Text += text;
                            var rect = info.Rectangle;
                            rect.Width += width;
                            info.Rectangle = rect;
                        }
                        else
                        {
                            info = new renderingInfo(text, state.Font, new Rectangle(x, y, width, spaceSize(state.Font).Height), state.Color, state.LinkNumber);
                            renderings.Add(info);
                        }
                        if (state.LinkNumber != null)
                        {
                            var list = linkRenderings[state.LinkNumber.Value].Rectangles;
                            if (list.Count == 0 || list[list.Count - 1].Y != info.Rectangle.Y)
                                list.Add(info.Rectangle);
                            else
                            {
                                var rect = list[list.Count - 1];
                                rect.Width += width;
                                list[list.Count - 1] = rect;
                            }
                        }
                    }
                    atBeginningOfLine = false;
                    x += width;
                },
                (state, newParagraph, indent) =>
                {
                    atBeginningOfLine = true;
                    var sh = spaceSize(state.Font).Height;
                    y += sh;
                    if (newParagraph && _paragraphSpacing > 0)
                        y += (int) (_paragraphSpacing * sh);
                    var newIndent = state.BlockIndent + indent;
                    if (!newParagraph)
                        newIndent += hangingIndent;
                    x = newIndent + glyphOverhang.Width / 2;
                    return newIndent;
                },
                (state, tag, parameter) =>
                {
                    var font = state.Font;
                    switch (tag)
                    {
                        // ITALICS
                        case '/': return Tuple.Create(state.ChangeFont(new Font(font, font.Style | FontStyle.Italic)), 0);

                        // BOLD
                        case '*': return Tuple.Create(state.ChangeFont(new Font(font, font.Style | FontStyle.Bold)), 0);

                        // UNDERLINE
                        case '_': return Tuple.Create(state.ChangeFont(new Font(font, font.Style | FontStyle.Underline)), 0);

                        // MAIN MNEMONIC
                        case '&':
                            if (ShowKeyboardCues)
                                return Tuple.Create(state.ChangeFont(new Font(font, font.Style | FontStyle.Underline)), 0);
                            break;

                        // BULLET POINT
                        case '[':
                            var advance = bulletSize(font);
                            if (renderings != null)
                                renderings.Add(new renderingInfo(BULLET, font, new Rectangle(x, y, advance, spaceSize(font).Height), initialForeColor, null));
                            x += advance;
                            return Tuple.Create(state.ChangeBlockIndent(state.BlockIndent + advance), advance);

                        // LINK (e.g. <link target>{link text}, link target may be omitted)
                        case '{':
                            if (linkRenderings == null)
                                break;
                            linkRenderings.Add(new linkLocationInfo { LinkID = parameter });
                            return Tuple.Create(state.ChangeLinkNumberAndColor(linkRenderings.Count - 1, Enabled ? LinkColor : SystemColors.GrayText), 0);

                        // COLOUR (e.g. <colour>=coloured text=, revert to default colour if no <colour> specified)
                        case '=':
                            var color = parameter == null ? initialForeColor : (Color) (_colorConverter ?? (_colorConverter = new ColorConverter())).ConvertFromString(parameter);
                            return Tuple.Create(state.ChangeColor(color), 0);
                    }
                    return Tuple.Create(state, 0);
                });
            return new Size(actualWidth + glyphOverhang.Width, y + spaceSize(initialFont).Height + glyphOverhang.Height);
        }

        /// <summary>Override; see base.</summary>
        protected override void OnChangeUICues(UICuesEventArgs e)
        {
            if (e.ChangeKeyboard && (_mnemonic != '\0' || TabStop))
            {
                _cachedRendering = null;
                Invalidate();
            }
        }

        /// <summary>Override; see base.</summary>
        protected override bool ProcessMnemonic(char charCode)
        {
            if (!Enabled || !Visible)
                return false;

            // Main mnemonic, which takes focus to the next control in the form
            if (_mnemonic == char.ToUpperInvariant(charCode) && Parent != null)
            {
                OnMnemonic();
                return true;
            }

            // Mnemonics for links within the label, which trigger the link
            Stack<int> links = new Stack<int>();
            int linkNumber = -1;
            Func<EggsNode, bool> recurse = null;
            recurse = node =>
            {
                EggsTag tag;
                if ((tag = node as EggsTag) != null)
                {
                    if (tag.Tag == '<')
                        return false;
                    else if (tag.Tag == '{')
                    {
                        linkNumber++;
                        links.Push(linkNumber);
                    }
                    else if (tag.Tag == '&' && links.Count > 0 && tag.Children.Count == 1 && tag.Children.First() is EggsText &&
                        char.ToUpperInvariant(tag.Children.First().ToString(true)[0]) == char.ToUpperInvariant(charCode))
                    {
                        var pop = links.Pop();
                        _keyboardFocusOnLinkNumber = pop;
                        Focus();
                        if (LinkActivated != null)
                            LinkActivated(this, new LinkEventArgs(_linkLocations[pop].LinkID, _linkLocations[pop].Rectangles));
                        return true;
                    }
                    foreach (var child in tag.Children)
                        if (recurse(child))
                            return true;
                    if (tag.Tag == '{')
                        links.Pop();
                }
                return false;
            };

            return recurse(_parsed);
        }

        /// <summary>This method is called when the control responds to a mnemonic being pressed.</summary>
        protected virtual void OnMnemonic()
        {
            if (Parent != null)
                if (Parent.SelectNextControl(this, true, false, true, false) && !Parent.ContainsFocus)
                    Parent.Focus();
        }

        /// <summary>Override; see base.</summary>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_cachedRendering != null && e.Button == MouseButtons.None)
            {
                for (int i = 0; i < _linkLocations.Count; i++)
                    foreach (var rectangle in _linkLocations[i].Rectangles)
                        if (rectangle.Contains(e.Location))
                        {
                            if (_mouseOnLinkNumber != i)
                            {
                                Cursor = _cursorHand;
                                _mouseOnLinkNumber = i;
                                Invalidate();
                            }
                            goto Out;
                        }
                if (_mouseOnLinkNumber != null)
                {
                    Cursor = Cursors.Default;
                    _mouseOnLinkNumber = null;
                    Invalidate();
                }
            }
            Out:
            base.OnMouseMove(e);
        }

        /// <summary>Override; see base.</summary>
        protected override void OnMouseLeave(EventArgs e)
        {
            if (_cachedRendering != null && TabStop)
            {
                Cursor = Cursors.Default;
                _mouseOnLinkNumber = null;
                Invalidate();
            }
            base.OnMouseLeave(e);
        }

        /// <summary>Override; see base.</summary>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (_mouseOnLinkNumber != null)
            {
                _mouseIsDownOnLink = true;
                _keyboardFocusOnLinkNumber = _mouseOnLinkNumber;
                Focus();
                Invalidate();
            }
            base.OnMouseDown(e);
        }

        /// <summary>Override; see base.</summary>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (_mouseIsDownOnLink)
            {
                _mouseIsDownOnLink = false;
                Invalidate();

                if (LinkActivated != null)
                {
                    // Determine whether the mouse is still on the link; if so, trigger it
                    bool still = false;
                    foreach (var rectangle in _linkLocations[_mouseOnLinkNumber.Value].Rectangles)
                        if (rectangle.Contains(e.Location))
                        {
                            still = true;
                            break;
                        }
                    if (still)
                        LinkActivated(this, new LinkEventArgs(_linkLocations[_mouseOnLinkNumber.Value].LinkID, _linkLocations[_mouseOnLinkNumber.Value].Rectangles));
                }
            }
            base.OnMouseUp(e);
        }

        /// <summary>Override; see base.</summary>
        protected override void OnGotFocus(EventArgs e)
        {
            _lastHadFocus = true;
            if (_keyboardFocusOnLinkNumber == null)
            {
                if (_linkLocations == null)
                {
                    // Annoying: OnGotFocus can be called before the label has ever been painted and thus before _linkLocations has ever been populated.
                    // Use a kludgy workaround to get it to be called again later — hopefully then the label has been painted.
                    _callGotFocusAfterPaint = true;
                }
                else
                    _keyboardFocusOnLinkNumber = _linkLocations.Count == 0 ? (int?) null : Control.ModifierKeys.HasFlag(Keys.Shift) ? _linkLocations.Count - 1 : 0;
            }

            // Only call the base if this is not the late invocation from the paint event
            if (!(e is PaintEventArgs))
                base.OnGotFocus(e);
        }

        /// <summary>Override; see base.</summary>
        protected override void OnLostFocus(EventArgs e)
        {
            _lastHadFocus = false;
            if (_formJustDeactivated)
                _formJustDeactivated = false;
            else
                _keyboardFocusOnLinkNumber = null;
            base.OnLostFocus(e);
        }

        /// <summary>Override; see base.</summary>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Modifiers == Keys.None)
            {
                if (e.KeyCode == Keys.Enter)
                {
                    if (_keyboardFocusOnLinkNumber != null && LinkActivated != null)
                        LinkActivated(this, new LinkEventArgs(_linkLocations[_keyboardFocusOnLinkNumber.Value].LinkID, _linkLocations[_keyboardFocusOnLinkNumber.Value].Rectangles));
                    e.Handled = true;
                    return;
                }
                else if (e.KeyCode == Keys.Space)
                {
                    _spaceIsDownOnLink = true;
                    Invalidate();
                    e.Handled = true;
                    return;
                }
                else if (e.KeyCode == Keys.Tab && (_keyboardFocusOnLinkNumber == null || _keyboardFocusOnLinkNumber.Value < _linkLocations.Count - 1))
                {
                    _keyboardFocusOnLinkNumber = (_keyboardFocusOnLinkNumber ?? -1) + 1;
                    e.Handled = true;
                    return;
                }
            }
            base.OnKeyDown(e);
        }

        /// <summary>Override; see base.</summary>
        protected override bool ProcessDialogKey(Keys keyData)
        {
            if (keyData == Keys.Tab && (_keyboardFocusOnLinkNumber == null || _keyboardFocusOnLinkNumber < _linkLocations.Count - 1))
            {
                _keyboardFocusOnLinkNumber = (_keyboardFocusOnLinkNumber ?? -1) + 1;
                return true;
            }

            if (keyData == (Keys.Tab | Keys.Shift) && (_keyboardFocusOnLinkNumber == null || _keyboardFocusOnLinkNumber > 0))
            {
                _keyboardFocusOnLinkNumber = (_keyboardFocusOnLinkNumber ?? _linkLocations.Count) - 1;
                return true;
            }

            return base.ProcessDialogKey(keyData);
        }

        /// <summary>Override; see base.</summary>
        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space)
            {
                _spaceIsDownOnLink = false;
                Invalidate();
                if (_keyboardFocusOnLinkNumber != null && LinkActivated != null)
                    LinkActivated(this, new LinkEventArgs(_linkLocations[_keyboardFocusOnLinkNumber.Value].LinkID, _linkLocations[_keyboardFocusOnLinkNumber.Value].Rectangles));
                e.Handled = true;
                return;
            }
            base.OnKeyUp(e);
        }

        /// <summary>Override; see base.</summary>
        protected override void OnParentChanged(EventArgs e)
        {
            someParentChangedSomewhere(null, null);
            base.OnParentChanged(e);
        }

        private void someParentChangedSomewhere(object sender, EventArgs e)
        {
            foreach (var parent in _parentChain)
                parent.ParentChanged -= someParentChangedSomewhere;
            _parentChain.Clear();
            var control = Parent;
            while (control != null)
            {
                control.ParentChanged += someParentChangedSomewhere;
                Form form;
                if ((form = control as Form) != null)
                    form.Deactivate += formDeactivated;
                control = control.Parent;
            }
        }

        private void formDeactivated(object sender, EventArgs e)
        {
            if (_lastHadFocus)
                _formJustDeactivated = true;
        }
    }

    /// <summary>Provides data for the <see cref="LabelEx.LinkActivated"/> event.</summary>
    public class LinkEventArgs : EventArgs
    {
        /// <summary>Gets the user-specified Link ID associated with the activated link.</summary>
        public string LinkID { get; private set; }
        /// <summary>Gets the location of the link as a series of rectangles relative to the control’s client co-ordinates.</summary>
        public Rectangle[] LinkLocation { get; private set; }
        /// <summary>Constructor.</summary>
        public LinkEventArgs(string linkId, IEnumerable<Rectangle> linkLocation) { LinkID = linkId; LinkLocation = linkLocation.ToArray(); }
    }

    /// <summary>Represents the method that will handle the <see cref="LabelEx.LinkActivated"/> event.</summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">A <see cref="LinkEventArgs"/> that contains the event data.</param>
    public delegate void LinkEventHandler(object sender, LinkEventArgs e);
}
