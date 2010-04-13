using System;
using System.Drawing;
using System.Windows.Forms;

namespace RT.Util.Controls
{
    /// <summary>
    /// Provides a double-buffered drawing surface with an off-screen buffer.
    /// All painting is done into the buffer, which is then blitted onto the
    /// screen as required. Repainting of the off-screen buffer is only done
    /// on size changes or explicit calls to <see cref="DoubleBufferedPanel.Refresh"/>.
    /// </summary>
    public sealed class DoubleBufferedPanel : Panel
    {
        /// <summary>
        /// Occurs when the off-screen buffer needs to be painted.
        /// </summary>
        public event PaintEventHandler PaintBuffer;

        /// <summary>
        /// Is used for detecting that the paint buffer must be repainted due to the
        /// paint event handler changing.
        /// </summary>
        private event PaintEventHandler _previousPaintBuffer;

        /// <summary>
        /// Holds the off-screen image. Initialised only when the first refresh occurs.
        /// </summary>
        protected Bitmap Buffer;

        /// <summary>
        /// Gets or sets a value indicating whether the panel should automatically refresh
        /// its contents (i.e. call the PaintBuffer event) every time it is resized.
        /// </summary>
        public bool RefreshOnResize { get; set; }

        /// <summary>Constructor.</summary>
        public DoubleBufferedPanel()
        {
            this.SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.DoubleBuffer, true
            );

            RefreshOnResize = true;
            this.Paint += new PaintEventHandler(DoubleBufferedPanel_Paint);
            this.Resize += new EventHandler(DoubleBufferedPanel_Resize);
        }

        private void DoubleBufferedPanel_Resize(object sender, EventArgs e)
        {
            if (RefreshOnResize)
                Refresh();
        }

        /// <summary>
        /// Forces an update of the off-screen buffer, by invoking the
        /// <see cref="PaintBuffer"/> event. Then forces a normal refresh
        /// of the underlying panel, which causes the off-screen buffer to
        /// be repainted over the whole panel.
        /// </summary>
        public override void Refresh()
        {
            _previousPaintBuffer = PaintBuffer;
            if (Width > 0 && Height > 0)
            {
                if (Buffer == null || Buffer.Width != Width || Buffer.Height != Height)
                    Buffer = new Bitmap(Width, Height);

                if (PaintBuffer != null)
                    PaintBuffer(this, new PaintEventArgs(
                        Graphics.FromImage(Buffer),
                        new Rectangle(0, 0, Width, Height)
                    ));
            }
            base.Refresh();
        }

        private void DoubleBufferedPanel_Paint(object sender, PaintEventArgs e)
        {
            if (Buffer == null || PaintBuffer != _previousPaintBuffer)
                Refresh();
            if (Buffer != null)
                e.Graphics.DrawImage(Buffer, e.ClipRectangle, e.ClipRectangle, GraphicsUnit.Pixel);
        }
    }
}
