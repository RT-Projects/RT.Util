using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;

namespace RT.Util.Controls
{
    /// <summary>Provides a <see cref="LabelEx"/> wrapped in a ScrollableControl that behaves properly.</summary>
    public class ScrollableLabelEx : Panel
    {
        /// <summary>Constructor.</summary>
        public ScrollableLabelEx()
        {
            Label = new LabelEx
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                WordWrap = true
            };
            Controls.Add(Label);
            AutoScroll = true;
            TabStop = false;

            Label.LinkGotFocus += linkGotFocus;

            // For some reason KeyDown doesn’t receive the cursor keys, so use PreviewKeyDown
            Label.PreviewKeyDown += keyDown;
        }

        private void keyDown(object sender, PreviewKeyDownEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Up:
                    ScrollTo(-2 * VerticalScroll.SmallChange - DisplayRectangle.Y);
                    e.IsInputKey = false;
                    break;
                case Keys.Down:
                    ScrollTo(2 * VerticalScroll.SmallChange - DisplayRectangle.Y);
                    e.IsInputKey = false;
                    break;
                case Keys.PageUp:
                    ScrollTo(-VerticalScroll.LargeChange - DisplayRectangle.Y);
                    e.IsInputKey = false;
                    break;
                case Keys.PageDown:
                    ScrollTo(VerticalScroll.LargeChange - DisplayRectangle.Y);
                    e.IsInputKey = false;
                    break;
                case Keys.Home:
                    ScrollTo(0);
                    e.IsInputKey = false;
                    break;
                case Keys.End:
                    ScrollTo(Label.Height);
                    e.IsInputKey = false;
                    break;
            }
        }

        private void linkGotFocus(object sender, LinkEventArgs e)
        {
            // Scroll to the link that got focus if the link is partly outside the viewport,
            // but leave a margin of 1/10th the height of the client area
            var y = -DisplayRectangle.Y;
            for (int i = e.LinkLocation.Length - 1; i >= 0; i--)
                if (e.LinkLocation[i].Bottom > y + ClientSize.Height)
                    y = e.LinkLocation[i].Bottom - ClientSize.Height * 9 / 10;
            for (int i = 0; i < e.LinkLocation.Length; i++)
                if (e.LinkLocation[i].Top < y)
                    y = e.LinkLocation[i].Top - ClientSize.Height / 10;
            ScrollTo(y);
        }

        public void ScrollTo(int y)
        {
            y = Math.Max(Math.Min(y, Label.Height - ClientSize.Height), 0);
            SetDisplayRectLocation(0, -y);
            VerticalScroll.Value = y;
        }

        /// <summary>Override; see base.</summary>
        [DefaultValue(true)]
        public override bool AutoScroll
        {
            get { return base.AutoScroll; }
            set { base.AutoScroll = value; }
        }

        /// <summary>Gets the inner label.</summary>
        public LabelEx Label { get; private set; }

        /// <summary>Override; see base.</summary>
        protected override Point ScrollToControl(Control activeControl)
        {
            // For some strange reason, ScrollableControl feels the need occasionally to scroll to the 
            // currently focused control (e.g. when the layout engine tells it to perform layout, even
            // if the location and size of controls don’t actually chage). That would cause it to scroll
            // to the top unwantedly. This prevents it.
            return DisplayRectangle.Location;
        }
    }
}
