using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace RT.Util.Controls
{
    /// <summary>
    /// SplitContainerEx provides such "advanced" features as painting the bloody splitter.
    /// </summary>
    public partial class SplitContainerEx : SplitContainer
    {
        /// <summary>
        /// Initialises a <see cref="SplitContainerEx"/> instance.
        /// </summary>
        public SplitContainerEx()
        {
            InitializeComponent();

            Paint += new PaintEventHandler(SplitContainerEx_Paint);
            SplitterMoved += new SplitterEventHandler(SplitContainerEx_SplitterMoved);
        }

        private bool _paintSplitter = true;

        /// <summary>
        /// Specifies whether the splitter should be painted.
        /// </summary>
        public bool PaintSplitter
        {
            get { return _paintSplitter; }
            set
            {
                _paintSplitter = value;
                Invalidate();
            }
        }

        void SplitContainerEx_Paint(object sender, PaintEventArgs e)
        {
            if (!_paintSplitter)
                return;

            int mid = SplitterDistance + SplitterWidth / 2;

            var Highlight = new Pen(Color.FromKnownColor(KnownColor.ButtonHighlight), 1);
            var Face = new Pen(Color.FromKnownColor(KnownColor.ControlDark), 1);

            if (Orientation == Orientation.Horizontal)
            {
                // Horizontal
                e.Graphics.DrawLine(Highlight, 0, mid - 2, ClientRectangle.Width, mid - 2);
                e.Graphics.DrawLine(Face, 0, mid - 1, ClientRectangle.Width, mid - 1);
                e.Graphics.DrawLine(Highlight, 0, mid, ClientRectangle.Width, mid);
                e.Graphics.DrawLine(Face, 0, mid + 1, ClientRectangle.Width, mid + 1);
            }
            else
            {
                // Vertical
                e.Graphics.DrawLine(Highlight, mid - 2, 0, mid - 2, ClientRectangle.Height);
                e.Graphics.DrawLine(Face, mid - 1, 0, mid - 1, ClientRectangle.Height);
                e.Graphics.DrawLine(Highlight, mid, 0, mid, ClientRectangle.Height);
                e.Graphics.DrawLine(Face, mid + 1, 0, mid + 1, ClientRectangle.Height);
            }
        }

        void SplitContainerEx_SplitterMoved(object sender, SplitterEventArgs e)
        {
            if (_settings != null && Visible)
                _settings.PositionPercent = (double) SplitterDistance / Width;
        }

        /// <summary>Holds the settings of the <see cref="SplitContainerEx"/>.</summary>
        public class Settings
        {
            /// <summary>Holds the position of the splitter, or null if not stored yet.</summary>
            public double? PositionPercent;
        }

        private Settings _settings;

        /// <summary>
        /// Stores a reference to the specified settings class, or null to disable the saving of settings.
        /// Loads the settings from the specified instance and applies them to the control.
        /// Must be called in form's Load event or later to have an effect!
        /// </summary>
        public void SetSettings(Settings settings)
        {
            _settings = settings;
            if (_settings != null)
            {
                if (_settings.PositionPercent != null)
                    SplitterDistance = (int) (_settings.PositionPercent.Value * Width);
            }
        }
    }
}
