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
        public event PaintEventHandler PaintBuffer;

        private Bitmap Buffer;

        public DoubleBufferedPanel()
        {
            this.SetStyle(
                ControlStyles.AllPaintingInWmPaint | 
                ControlStyles.UserPaint | 
                ControlStyles.DoubleBuffer, true
            );

            Buffer = new Bitmap(Width, Height);
            this.Paint += new PaintEventHandler(DoubleBufferedPanel_Paint);
            this.Resize += new EventHandler(DoubleBufferedPanel_Resize);
        }

        private void DoubleBufferedPanel_Resize(object sender, EventArgs e)
        {
            Refresh();
        }

        public override void Refresh()
        {
            if (Buffer.Width != Width || Buffer.Height != Height)
                Buffer = new Bitmap(Width, Height);

            if (PaintBuffer != null)
                PaintBuffer(this, new PaintEventArgs(
                    Graphics.FromImage(Buffer),
                    new Rectangle(0, 0, Width, Height)
                ));

            base.Refresh();
        }

        void DoubleBufferedPanel_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.DrawImage(Buffer, e.ClipRectangle, e.ClipRectangle, GraphicsUnit.Pixel);
        }
    }
}
