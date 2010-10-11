using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;

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

        /// <summary>Constructor.</summary>
        public LabelEx()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.Selectable | ControlStyles.FixedHeight, false);
            SetStyle(ControlStyles.ResizeRedraw, true);
            TabStop = false;
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
            _parsed = EggsML.Parse(base.Text);
            _cachedMeasuredWidth = 0;
            autosize();
            Invalidate();
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
            _paintGr = e.Graphics;
            _paintX = 0;
            _paintY = 0;
            _doMeasure = false;
            doPaintOrMeasure(_parsed, Font);
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
                    doPaintOrMeasure(_parsed, Font);
                }
            return new Size(_cachedMeasuredWidth, _cachedMeasuredHeight);
        }

        private Graphics _paintGr;
        private int _paintX, _paintY;
        private bool _doMeasure;
        private int _lineStartGlyphOverhang;

        private void doPaintOrMeasure(EggsNode node, Font font)
        {
            if (node is EggsText)
            {
                var dummy = new Size(int.MaxValue, int.MaxValue); // the API requires a size to be specified in order to specify format flags..... argh!
                var glyphOverhang = (TextRenderer.MeasureText(_paintGr, "mm", font).Width - TextRenderer.MeasureText(_paintGr, "mm", font, dummy, TextFormatFlags.NoPadding).Width + 1) / 2;

                var lines = ((EggsText) node).Text.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Replace("\r", "");
                    var size = TextRenderer.MeasureText(_paintGr, line, font, dummy, TextFormatFlags.NoPadding);
                    if (_doMeasure && _paintX == 0)
                        _lineStartGlyphOverhang = glyphOverhang;
                    if (!_doMeasure)
                        TextRenderer.DrawText(_paintGr, line, font, new Point(_paintX, _paintY), ForeColor);
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
                    foreach (var subnodes in n.Children)
                        foreach (var subnode in subnodes)
                            doPaintOrMeasure(subnode, subfont);
                }
            }
            else if (node is EggsGroup)
            {
                foreach (var subnode in ((EggsGroup) node).Children)
                    doPaintOrMeasure(subnode, font);
            }
        }
    }
}
