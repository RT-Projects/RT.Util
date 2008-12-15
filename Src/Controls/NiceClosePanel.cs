using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace RT.Util.Controls
{
    /// <summary>Provides a narrow panel that somewhat resembles a
    /// tooltip window's title bar with a close button.</summary>
    public class NiceClosePanel : Panel
    {
        private Button _closeButton;

        /// <summary>Triggers when the close button is clicked.</summary>
        public event EventHandler CloseClicked;

        /// <summary>Initialises a new <see cref="NiceClosePanel"/> instance.</summary>
        public NiceClosePanel()
            : base()
        {
            initializeComponent();
            this.Resize += new EventHandler(resize);
            this.Paint += new PaintEventHandler(paint);
            this._closeButton.Click += new EventHandler(closeButton_Click);
        }

        private void closeButton_Click(object sender, EventArgs e)
        {
            if (CloseClicked != null)
                CloseClicked(this, e);
        }

        private void paint(object sender, PaintEventArgs e)
        {
            for (int i = 2; i < ClientSize.Height - 2; i++)
                e.Graphics.DrawLine(
                    new Pen(Color.FromKnownColor(
                        i % 2 == 1 ? KnownColor.ControlDark : KnownColor.ControlLightLight
                    )),
                    0, i, ClientSize.Width - _closeButton.Width - 3, i
                );
        }

        private void resize(object sender, EventArgs e)
        {
            _closeButton.Size = new Size(ClientSize.Height, ClientSize.Height);
            _closeButton.Location = new Point(ClientSize.Width - _closeButton.Width, 0);
            Refresh();
        }

        private void initializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NiceClosePanel));
            this._closeButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // FCloseButton
            // 
            this._closeButton.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this._closeButton.Image = ((System.Drawing.Image) (resources.GetObject("FCloseButton.Image")));
            this._closeButton.Location = new System.Drawing.Point(0, 0);
            this._closeButton.Margin = new System.Windows.Forms.Padding(0);
            this._closeButton.Name = "FCloseButton";
            this._closeButton.Padding = new System.Windows.Forms.Padding(0, 0, 2, 2);
            this._closeButton.Size = new System.Drawing.Size(8, 8);
            this._closeButton.TabIndex = 0;
            this._closeButton.TabStop = false;
            // 
            // NiceClosePanel
            // 
            this.Controls.Add(this._closeButton);
            this.ResumeLayout(false);
        }
    }
}
