using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace RT.Controls
{
    /// <summary>
    /// SplitContainerEx provides such "advanced" features as painting the bloody splitter.
    /// </summary>
    public partial class SplitContainerEx : SplitContainer
    {
        public SplitContainerEx()
        {
            InitializeComponent();

            Paint += new PaintEventHandler(SplitContainerEx_Paint);
        }

        private bool FPaintSplitter = true;

        public bool PaintSplitter
        {
            get { return FPaintSplitter; }
            set
            {
                FPaintSplitter = value;
                Invalidate();
            }
        }

        void SplitContainerEx_Paint(object sender, PaintEventArgs e)
        {
            if (!FPaintSplitter)
                return;

            int mid = SplitterDistance + SplitterWidth / 2;

            if (Orientation == Orientation.Horizontal)
            {
                // Horizontal
                e.Graphics.DrawLine(Pens.White, 0, mid-2, ClientRectangle.Width, mid-2);
                e.Graphics.DrawLine(Pens.Gray,  0, mid-1, ClientRectangle.Width, mid-1);
                e.Graphics.DrawLine(Pens.White, 0, mid,   ClientRectangle.Width, mid);
                e.Graphics.DrawLine(Pens.Gray,  0, mid+1, ClientRectangle.Width, mid+1);
            }
            else
            {
                // Vertical
                e.Graphics.DrawLine(Pens.White, mid-2, 0, mid-2, ClientRectangle.Height);
                e.Graphics.DrawLine(Pens.Gray,  mid-1, 0, mid-1, ClientRectangle.Height);
                e.Graphics.DrawLine(Pens.White, mid,   0, mid,   ClientRectangle.Height);
                e.Graphics.DrawLine(Pens.Gray,  mid+1, 0, mid+1, ClientRectangle.Height);
            }
        }
    }
}
