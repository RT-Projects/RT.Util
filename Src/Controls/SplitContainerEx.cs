using System;
using System.Drawing;
using System.Windows.Forms;

namespace RT.Util.Controls
{
    /// <summary>
    /// SplitContainerEx provides such "advanced" features as painting the bloody splitter.
    /// </summary>
    public sealed partial class SplitContainerEx : SplitContainer
    {
        /// <summary>
        /// Initialises a <see cref="SplitContainerEx"/> instance.
        /// </summary>
        public SplitContainerEx()
        {
            base.SplitterMoved += splitterMoved;
        }

        /// <summary>Occurs when the splitter control is moved.</summary>
        public event SplitterEventHandler SplitterMoved;

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
        private bool _paintSplitter = true;

        protected override void OnPaint(PaintEventArgs e)
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

        private void splitterMoved(object sender, SplitterEventArgs e)
        {
            if (_settings != null && Visible)
                _settings.PositionPercent = (double) SplitterDistance / Width;
            if (SplitterMoved != null)
                SplitterMoved(sender, e);
        }

        /// <summary>Holds the settings of the <see cref="SplitContainerEx"/>.</summary>
        public sealed class Settings
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
            updateSplitterDistance();
        }

        /// <summary>Override; see base.</summary>
        protected override void OnResize(EventArgs e)
        {
            updateSplitterDistance();
        }

        private void updateSplitterDistance()
        {
            if (_settings != null && Visible && _settings.PositionPercent != null)
                SplitterDistance = (int) (_settings.PositionPercent.Value * Width);
        }

        /// <summary>Override; see base.</summary>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == (Orientation == Orientation.Vertical ? Keys.Left : Keys.Up))
            {
                SplitterDistance -= 10;
                e.Handled = true;
            }
            if (e.Control && e.KeyCode == (Orientation == Orientation.Vertical ? Keys.Right : Keys.Down))
            {
                SplitterDistance += 10;
                e.Handled = true;
            }

            base.OnKeyDown(e);
        }
    }
}
