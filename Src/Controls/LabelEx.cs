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
    public class LabelEx : Control, IButtonControl
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
            TabStop = false;
            AutoSize = true;
            LinkColor = SystemColors.HotTrack;
            LinkActiveColor = Color.Red;
        }

        /// <summary>Text displayed in the label. See remarks for EggsML supported. Setting this property also changes
        /// the value of the <see cref="TabStop"/> property depending on whether the text contains any links.
        /// To achieve deviant behaviour, set TabStop immediately after setting Text.</summary>
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

        /// <summary>Gets or sets a value indicating whether the user can give the focus to this control using the TAB key.
        /// This property’s value automatically changes every time Text changes; it is set to true if there is a link in the text, false otherwise.
        /// To achieve deviant behaviour, set TabStop immediately after setting Text.</summary>
        [DefaultValue(typeof(bool), "false")]
        public new bool TabStop
        {
            get { return base.TabStop; }
            set
            {
                if (value == base.TabStop)
                    return;
                base.TabStop = value;
                SetStyle(ControlStyles.Selectable, value);
            }
        }

        /// <summary>Contains information about a parse error that describes in what way the current value of
        /// <see cref="Text"/> is invalid EggsML, or null if it is valid.</summary>
        public EggsMLParseException ParseError { get; private set; }

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
        private static Dictionary<Font, Dictionary<string, Size>> _measureCache = new Dictionary<Font, Dictionary<string, Size>>();

        private Dictionary<int, Size> _cachedPreferredSizes = new Dictionary<int, Size>();
        private List<renderingInfo> _cachedRendering;
        private Color _cachedRenderingColor;
        private int _cachedRenderingWidth;
        private bool _formJustDeactivated;
        private int _hangingIndent = 0;
        private IndentUnit _hangingIndentUnit = IndentUnit.Spaces;
        private linkLocationInfo _keyboardFocusOnLinkPrivate;
        private bool _lastHadFocus;
        private List<locationInfo> _specialLocations = new List<locationInfo>();
        private char _mnemonic;
        private bool _mouseIsDownOnLink;
        private linkLocationInfo _mouseOnLink;
        private double _paragraphSpacing = 0d;
        private List<Control> _parentChain = new List<Control>();
        private EggsNode _parsed;
        private bool _spaceIsDownOnLink;
        private bool _wordWrap = false;
        private ToolTip _tooltip = new ToolTip { StripAmpersands = false, Active = true };
        private string _tooltipText;

        private linkLocationInfo _keyboardFocusOnLink
        {
            get { return _keyboardFocusOnLinkPrivate; }
            set
            {
                if (_keyboardFocusOnLinkPrivate == value)
                    return;

                // The previous focused link loses focus
                if (LinkLostFocus != null && _keyboardFocusOnLinkPrivate != null)
                    LinkLostFocus(this, new LinkEventArgs(_keyboardFocusOnLink.LinkID, _keyboardFocusOnLink.Rectangles));

                _keyboardFocusOnLinkPrivate = value;
                Invalidate();

                // The new link gains focus
                if (LinkGotFocus != null && _keyboardFocusOnLinkPrivate != null)
                    LinkGotFocus(this, new LinkEventArgs(_keyboardFocusOnLink.LinkID, _keyboardFocusOnLink.Rectangles));
            }
        }

        // TextRenderer.MeasureText() requires a useless size to be specified in order to specify format flags
        private static Size _dummySize = new Size(int.MaxValue, int.MaxValue);

        private sealed class renderState
        {
            public Font Font { get; private set; }
            public Color Color { get; private set; }
            public int BlockIndent { get; private set; }
            public IEnumerable<locationInfo> ActiveLocations { get; private set; }
            public bool Mnemonic { get; private set; }

            public renderState(Font initialFont, Color initialColor) { Font = initialFont; Color = initialColor; BlockIndent = 0; ActiveLocations = Enumerable.Empty<locationInfo>(); Mnemonic = false; }
            private renderState(Font font, Color color, int blockIndent, IEnumerable<locationInfo> activeLocations, bool mnemonic) { Font = font; Color = color; BlockIndent = blockIndent; ActiveLocations = activeLocations; Mnemonic = mnemonic; }
            public renderState ChangeFont(Font newFont) { return new renderState(newFont, Color, BlockIndent, ActiveLocations, Mnemonic); }
            public renderState ChangeColor(Color newColor) { return new renderState(Font, newColor, BlockIndent, ActiveLocations, Mnemonic); }
            public renderState ChangeBlockIndent(int newIndent) { return new renderState(Font, Color, newIndent, ActiveLocations, Mnemonic); }
            public renderState SetMnemonic() { return new renderState(Font, Color, BlockIndent, ActiveLocations, true); }
            public renderState AddActiveLocation(locationInfo location) { return new renderState(Font, Color, BlockIndent, location.Concat(ActiveLocations), Mnemonic); }
        }

        private sealed class renderingInfo
        {
            public string Text;
            public Rectangle Rectangle;
            public renderState State;
            public renderingInfo(string text, Rectangle location, renderState state) { Text = text; Rectangle = location; State = state; }
        }

        private abstract class locationInfo
        {
            public List<Rectangle> Rectangles = new List<Rectangle>();
        }

        private sealed class linkLocationInfo : locationInfo
        {
            public string LinkID;
            public char? Mnemonic;
        }

        private sealed class tooltipLocationInfo : locationInfo
        {
            public string Tooltip;
        }

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
            checkForLinksAndTooltips(PointToClient(Control.MousePosition));
        }

        /// <summary>Override; see base.</summary>
        protected override void OnTextChanged(EventArgs e)
        {
            // If a link has focus, trigger the LinkLostFocus event.
            // OnPaint will trigger the LinkGotFocus event as appropriate
            _keyboardFocusOnLink = null;

            _cachedPreferredSizes.Clear();
            _cachedRendering = null;
            base.OnTextChanged(e);
            _mnemonic = '\0';
            TabStop = false;
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
                TabStop = true;
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
            if (Size != PreferredSize)
                Size = PreferredSize;
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
            Color initialColor = Enabled ? ForeColor : SystemColors.GrayText;

            if (_cachedRendering == null || _cachedRenderingWidth != ClientSize.Width || _cachedRenderingColor != initialColor)
            {
                _cachedRendering = new List<renderingInfo>();
                _cachedRenderingWidth = ClientSize.Width;
                _cachedRenderingColor = initialColor;
                _specialLocations.Clear();
                doPaintOrMeasure(e.Graphics, _parsed, Font, initialColor, _cachedRenderingWidth, _cachedRendering, _specialLocations);

                // If this control has focus and it has a link in it, focus the first link. (This triggers the LinkGotFocus event.)
                if (!_lastHadFocus)
                    _keyboardFocusOnLink = null;
                else if (_keyboardFocusOnLink == null)
                    _keyboardFocusOnLink = _specialLocations.OfType<linkLocationInfo>().FirstOrDefault();

                checkForLinksAndTooltips(PointToClient(Control.MousePosition));
            }

            foreach (var item in _cachedRendering)
            {
                if (item.Rectangle.Bottom < e.ClipRectangle.Top || item.Rectangle.Right < e.ClipRectangle.Left || item.Rectangle.Left > e.ClipRectangle.Right)
                    continue;
                if (item.Rectangle.Top > e.ClipRectangle.Bottom)
                    break;
                var font = item.State.Font;
                if ((item.State.Mnemonic && ShowKeyboardCues) || (_mouseOnLink != null && item.State.ActiveLocations.Contains(_mouseOnLink)))
                    font = new Font(font, font.Style | FontStyle.Underline);
                TextRenderer.DrawText(e.Graphics, item.Text,
                    font,
                    item.Rectangle.Location,
                    (_mouseIsDownOnLink && item.State.ActiveLocations.Contains(_mouseOnLink)) ||
                    (_spaceIsDownOnLink && item.State.ActiveLocations.Contains(_keyboardFocusOnLink))
                        ? LinkActiveColor : item.State.Color,
                    TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
            }

            if (_keyboardFocusOnLink != null)
            {
                if (!_specialLocations.Contains(_keyboardFocusOnLink))
                    _keyboardFocusOnLinkPrivate = null;   // set the private one so that no event is triggered
                else
                    foreach (var rectangle in _keyboardFocusOnLink.Rectangles)
                        ControlPaint.DrawFocusRectangle(e.Graphics, rectangle);
            }
        }

        private Size measure(Font font, string text, Graphics g)
        {
            Dictionary<string, Size> dic;
            Size result;

            if (!_measureCache.TryGetValue(font, out dic))
                _measureCache[font] = dic = new Dictionary<string, Size>();
            if (!dic.TryGetValue(text, out result))
                dic[text] = result = TextRenderer.MeasureText(g, text, font, _dummySize, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
            return result;
        }

        private Size doPaintOrMeasure(Graphics g, EggsNode node, Font initialFont, Color initialForeColor, int constrainingWidth,
            List<renderingInfo> renderings = null, List<locationInfo> locations = null)
        {
            var glyphOverhang = TextRenderer.MeasureText(g, "Wg", initialFont, _dummySize) - TextRenderer.MeasureText(g, "Wg", initialFont, _dummySize, TextFormatFlags.NoPadding);
            int x = glyphOverhang.Width / 2, y = glyphOverhang.Height / 2;
            int wrapWidth = WordWrap ? Math.Max(1, constrainingWidth - glyphOverhang.Width) : int.MaxValue;
            int hangingIndent = _hangingIndent * (_hangingIndentUnit == IndentUnit.Spaces ? measure(initialFont, " ", g).Width : 1);
            bool atBeginningOfLine = false;

            int actualWidth = EggsML.WordWrap(node, new renderState(initialFont, initialForeColor), wrapWidth,
                (state, text) => measure(state.Font, text, g).Width,
                (state, text, width) =>
                {
                    if (state.Mnemonic && !string.IsNullOrWhiteSpace(text))
                        state.ActiveLocations.OfType<linkLocationInfo>().FirstOrDefault().NullOr(link => { link.Mnemonic = char.ToLowerInvariant(text.Trim()[0]); return link; });

                    if (renderings != null && !string.IsNullOrEmpty(text))
                    {
                        renderingInfo info;
                        if (!atBeginningOfLine && renderings.Count > 0 && (info = renderings[renderings.Count - 1]).State == state)
                        {
                            info.Text += text;
                            var rect = info.Rectangle;
                            rect.Width += width;
                            info.Rectangle = rect;
                        }
                        else
                        {
                            info = new renderingInfo(text, new Rectangle(x, y, width, measure(state.Font, " ", g).Height), state);
                            renderings.Add(info);
                        }
                        foreach (var location in state.ActiveLocations)
                        {
                            if (location.Rectangles.Count == 0 || location.Rectangles[location.Rectangles.Count - 1].Y != info.Rectangle.Y)
                                location.Rectangles.Add(info.Rectangle);
                            else
                            {
                                var rect = location.Rectangles[location.Rectangles.Count - 1];
                                rect.Width += width;
                                location.Rectangles[location.Rectangles.Count - 1] = rect;
                            }
                        }
                    }
                    atBeginningOfLine = false;
                    x += width;
                },
                (state, newParagraph, indent) =>
                {
                    atBeginningOfLine = true;
                    var sh = measure(state.Font, " ", g).Height;
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

                        // MNEMONICS
                        case '&': return Tuple.Create(state.SetMnemonic(), 0);

                        // BULLET POINT
                        case '[':
                            var bulletSize = measure(font, BULLET, g);
                            var advance = bulletSize.Width;
                            if (renderings != null)
                                renderings.Add(new renderingInfo(BULLET, new Rectangle(x, y, advance, bulletSize.Height), new renderState(font, state.Color)));
                            x += advance;
                            return Tuple.Create(state.ChangeBlockIndent(state.BlockIndent + advance), advance);

                        // LINK (e.g. <link target>{link text}, link target may be omitted)
                        case '{':
                            if (locations == null)
                                break;
                            var linkLocation = new linkLocationInfo { LinkID = parameter };
                            locations.Add(linkLocation);
                            return Tuple.Create(state.ChangeColor(Enabled ? LinkColor : SystemColors.GrayText).AddActiveLocation(linkLocation), 0);

                        // TOOLTIP (e.g. <tooltip text>#main text#)
                        case '#':
                            if (string.IsNullOrWhiteSpace(parameter) || locations == null)
                                break;
                            var tooltipLocation = new tooltipLocationInfo { Tooltip = parameter };
                            locations.Add(tooltipLocation);
                            return Tuple.Create(state.AddActiveLocation(tooltipLocation), 0);

                        // COLOUR (e.g. <colour>=coloured text=, revert to default colour if no <colour> specified)
                        case '=':
                            var color = parameter == null ? initialForeColor : (Color) (_colorConverter ?? (_colorConverter = new ColorConverter())).ConvertFromString(parameter);
                            return Tuple.Create(state.ChangeColor(color), 0);
                    }
                    return Tuple.Create(state, 0);
                });
            return new Size(actualWidth + glyphOverhang.Width, y + measure(initialFont, " ", g).Height + glyphOverhang.Height);
        }

        /// <summary>Override; see base.</summary>
        protected override void OnChangeUICues(UICuesEventArgs e)
        {
            if (e.ChangeKeyboard && (_mnemonic != '\0' || TabStop))
                Invalidate();
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
            var applicableLinks = _specialLocations.OfType<linkLocationInfo>().Where(link => link.Mnemonic == charCode).ToArray();
            if (applicableLinks.Length == 0)
                return false;
            else if (applicableLinks.Length == 1)
            {
                // One applicable link: activate it
                _keyboardFocusOnLink = applicableLinks[0];
                Focus();
                if (LinkActivated != null)
                    LinkActivated(this, new LinkEventArgs(applicableLinks[0].LinkID, applicableLinks[0].Rectangles));
            }
            else
            {
                // More than one applicable link: cycle between between them without activating them (must press Enter or Space to activate them)
                _keyboardFocusOnLink = _keyboardFocusOnLink.NullOr(kf => applicableLinks.SkipWhile(loc => loc != kf).Skip(1).FirstOrDefault()) ?? applicableLinks.FirstOrDefault();
                Focus();
            }
            return true;
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
                checkForLinksAndTooltips(e.Location);
            base.OnMouseMove(e);
        }

        private void checkForLinksAndTooltips(Point p)
        {
            var anyLink = false;
            var anyTooltip = false;

            if (Enabled)
            {
                foreach (var location in _specialLocations)
                    foreach (var rectangle in location.Rectangles)
                        if (rectangle.Contains(p))
                        {
                            if (location is linkLocationInfo)
                            {
                                if (_mouseOnLink != location)
                                {
                                    Cursor = _cursorHand;
                                    _mouseOnLink = (linkLocationInfo) location;
                                    Invalidate();
                                }
                                anyLink = true;
                            }
                            else
                            {
                                // tooltip
                                var tooltipText = ((tooltipLocationInfo) location).Tooltip;
                                if (_tooltipText != tooltipText)
                                {
                                    _tooltip.Show(tooltipText, this, rectangle.Left, rectangle.Bottom + 5);
                                    _tooltipText = tooltipText;
                                }
                                anyTooltip = true;
                            }
                        }
            }

            if (_mouseOnLink != null && !anyLink)
            {
                Cursor = Cursors.Default;
                _mouseOnLink = null;
                Invalidate();
            }

            if (!anyTooltip)
            {
                _tooltip.Hide(this);
                _tooltipText = null;
            }
        }

        /// <summary>Override; see base.</summary>
        protected override void OnMouseLeave(EventArgs e)
        {
            if (_cachedRendering != null && TabStop)
            {
                Cursor = Cursors.Default;
                _mouseOnLink = null;
                Invalidate();
            }
            _tooltip.Hide(this);
            _tooltipText = null;
            base.OnMouseLeave(e);
        }

        /// <summary>Override; see base.</summary>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (_mouseOnLink != null)
            {
                _mouseIsDownOnLink = true;
                _keyboardFocusOnLink = _mouseOnLink;
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
                    foreach (var rectangle in _mouseOnLink.Rectangles)
                        if (rectangle.Contains(e.Location))
                        {
                            still = true;
                            break;
                        }
                    if (still)
                        LinkActivated(this, new LinkEventArgs(_mouseOnLink.LinkID, _mouseOnLink.Rectangles));
                }
            }
            base.OnMouseUp(e);
        }

        /// <summary>Override; see base.</summary>
        protected override void OnGotFocus(EventArgs e)
        {
            _lastHadFocus = true;
            if (_keyboardFocusOnLink == null)
            {
                var links = _specialLocations.OfType<linkLocationInfo>();
                _keyboardFocusOnLink = Control.ModifierKeys.HasFlag(Keys.Shift) ? links.LastOrDefault() : links.FirstOrDefault();
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
                _keyboardFocusOnLink = null;
            base.OnLostFocus(e);
        }

        /// <summary>Override; see base.</summary>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            // This handles Enter and Space.
            // The tab key is handled in ProcessDialogKey() instead.
            if (e.Modifiers == Keys.None)
            {
                if (e.KeyCode == Keys.Enter)
                {
                    PerformClick();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Space)
                {
                    _spaceIsDownOnLink = true;
                    Invalidate();
                    e.Handled = true;
                    return;
                }
            }

            base.OnKeyDown(e);
        }

        /// <summary>Override; see base.</summary>
        protected override bool ProcessDialogKey(Keys keyData)
        {
            // This handles Tab and Shift-Tab.
            // The Enter and Space keys are handled in OnKeyDown() instead.

            if (keyData != Keys.Tab && keyData != (Keys.Tab | Keys.Shift))
                return base.ProcessDialogKey(keyData);

            var shift = keyData == (Keys.Tab | Keys.Shift);
            var links = _specialLocations.OfType<linkLocationInfo>();

            _keyboardFocusOnLink = shift
                ? links.TakeWhile(l => l != _keyboardFocusOnLink).LastOrDefault()
                : links.SkipWhile(l => l != _keyboardFocusOnLink).Skip(1).FirstOrDefault();

            return _keyboardFocusOnLink == null
                ? base.ProcessDialogKey(keyData)
                : true;
        }

        /// <summary>Override; see base.</summary>
        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space)
            {
                _spaceIsDownOnLink = false;
                Invalidate();
                PerformClick();
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

        /// <summary>Override; see base.</summary>
        protected override void OnSystemColorsChanged(EventArgs e)
        {
            _cachedRendering = null;
            Invalidate();
            base.OnSystemColorsChanged(e);
        }

        private void someParentChangedSomewhere(object sender, EventArgs e)
        {
            Form form;
            foreach (var parent in _parentChain)
            {
                parent.ParentChanged -= someParentChangedSomewhere;
                if ((form = parent as Form) != null)
                    form.Deactivate -= formDeactivated;
            }
            _parentChain.Clear();
            var control = Parent;
            while (control != null)
            {
                _parentChain.Add(control);
                control.ParentChanged += someParentChangedSomewhere;
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

        DialogResult IButtonControl.DialogResult
        {
            get { return DialogResult.None; }
            set { }
        }

        void IButtonControl.NotifyDefault(bool value) { }

        /// <summary>Pretends as if the user pressed Enter. Has no effect if there is no link that has keyboard focus.</summary>
        public void PerformClick()
        {
            if (_keyboardFocusOnLink != null && LinkActivated != null)
                LinkActivated(this, new LinkEventArgs(_keyboardFocusOnLink.LinkID, _keyboardFocusOnLink.Rectangles));
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
