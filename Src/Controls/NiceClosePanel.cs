using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

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
            _closeButton = new System.Windows.Forms.Button();
            _closeButton.Name = "_closeButton";
            _closeButton.Size = new System.Drawing.Size(8, 8);
            _closeButton.Text = "X";
            _closeButton.Tag = "notranslate";
            _closeButton.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            // This is a small 8×8 PNG that contains a black X on a transparent background
            _closeButton.Image = new Bitmap(new MemoryStream(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 13, 73, 72, 68, 82, 0, 0, 0, 8, 0, 0, 0, 8, 8, 6, 0, 0, 0, 196, 15, 190, 139, 0, 0, 0, 89, 73, 68, 65, 84, 40, 83, 99, 96, 128, 0, 126, 40, 141, 76, 113, 194, 56, 32, 198, 109, 32, 246, 64, 146, 53, 0, 178, 239, 1, 177, 16, 76, 204, 24, 200, 120, 2, 85, 4, 146, 124, 142, 166, 1, 172, 14, 164, 232, 37, 16, 191, 195, 38, 9, 82, 0, 210, 249, 6, 138, 145, 173, 3, 235, 70, 54, 22, 217, 58, 176, 36, 27, 16, 95, 69, 51, 22, 164, 8, 197, 145, 112, 47, 33, 249, 4, 44, 6, 0, 149, 131, 14, 219, 10, 117, 71, 99, 0, 0, 0, 0, 73, 69, 78, 68, 174, 66, 96, 130 }));
            _closeButton.Margin = new System.Windows.Forms.Padding(0);
            _closeButton.Padding = new System.Windows.Forms.Padding(0, 0, 2, 2);
            _closeButton.TabIndex = 0;
            _closeButton.TabStop = false;

            Controls.Add(_closeButton);
            Resize += new EventHandler(resize);
            Paint += new PaintEventHandler(paint);
            _closeButton.Click += new EventHandler(fireCloseClicked);

            // This is a workaround which appears to be necessary in Mono. Otherwise the button does not appear until the first time the level list is resized.
            Timer t = new Timer { Interval = 100 };
            t.Tick += (s, e) => { resize(s, e); t.Enabled = false; };
            t.Enabled = true;
        }

        private void fireCloseClicked(object sender, EventArgs e)
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
    }
}
