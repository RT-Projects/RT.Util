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
    public class ScrollableLabelEx : ScrollableControl
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
            Label.PreviewKeyDown += keyDown;
        }

        private void keyDown(object sender, PreviewKeyDownEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Up:
                    scrollTo(-2 * VerticalScroll.SmallChange - DisplayRectangle.Y);
                    e.IsInputKey = false;
                    break;
                case Keys.Down:
                    scrollTo(2 * VerticalScroll.SmallChange - DisplayRectangle.Y);
                    e.IsInputKey = false;
                    break;
                case Keys.PageUp:
                    scrollTo(-VerticalScroll.LargeChange - DisplayRectangle.Y);
                    e.IsInputKey = false;
                    break;
                case Keys.PageDown:
                    scrollTo(VerticalScroll.LargeChange - DisplayRectangle.Y);
                    e.IsInputKey = false;
                    break;
                case Keys.Home:
                    scrollTo(0);
                    e.IsInputKey = false;
                    break;
                case Keys.End:
                    scrollTo(Label.Height);
                    e.IsInputKey = false;
                    break;
            }
        }

        private void linkGotFocus(object sender, LinkEventArgs e)
        {
            var y = -DisplayRectangle.Y;
            for (int i = e.LinkLocation.Length - 1; i >= 0; i--)
                if (e.LinkLocation[i].Bottom > y + ClientSize.Height)
                    y = e.LinkLocation[i].Bottom - ClientSize.Height * 9 / 10;
            for (int i = 0; i < e.LinkLocation.Length; i++)
                if (e.LinkLocation[i].Top < y)
                    y = e.LinkLocation[i].Top - ClientSize.Height / 10;
            scrollTo(y);
        }

        private void scrollTo(int y)
        {
            y = Math.Min(Math.Max(y, 0), Label.Height - ClientSize.Height);
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
            return DisplayRectangle.Location;
        }
    }
}
