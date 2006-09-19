using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO.Compression;
using System.Drawing.Drawing2D;

namespace RT.Controls
{
    public class DoubleBufferedPanel : Panel
    {
        public DoubleBufferedPanel()
        {
            this.SetStyle(
                ControlStyles.AllPaintingInWmPaint | 
                ControlStyles.UserPaint | 
                ControlStyles.DoubleBuffer, true
            );

            DoPaint();
            this.Paint += new PaintEventHandler(DoubleBufferedPanel_Paint);
        }

        void DoubleBufferedPanel_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.DrawImage(Buffer, e.ClipRectangle, e.ClipRectangle, GraphicsUnit.Pixel);
        }

        private Bitmap Buffer;

        /// <summary>
        /// Resizes the buffer bitmap if necessary, then invokes the PaintBuffer callback to
        /// let the user paint the buffer.
        /// </summary>
        private void DoPaint()
        {
            if ((Buffer != null) && ((Buffer.Width != Width) || (Buffer.Height != Height)))
                Buffer = new Bitmap(Width, Height);

            PaintEventArgs pea = new PaintEventArgs(
                Graphics.FromImage(Buffer),
                new Rectangle(0, 0, Width, Height));

            if (PaintBuffer != null)
                PaintBuffer(this, pea);
        }

        public event PaintEventHandler PaintBuffer;

        public void ResizeAndRepaint()
        {
            DoPaint();
            Invalidate();
        }
    }
}