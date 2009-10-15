using System.ComponentModel;
using System.Windows.Forms;
using System.Drawing;

namespace RT.Util.Controls
{
    /// <summary>
    /// This control is a label with a horizontal line running to the right of the label.
    /// The purpose is to visually separate blocks of controls, similar to GroupBox but
    /// without completely surrounding the controls in a box.
    /// </summary>
    public partial class Separator : UserControl
    {
        private GroupBox _groupBox;

        /// <summary>Constructor.</summary>
        public Separator()
        {
            SuspendLayout();
            _groupBox = new GroupBox()
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Location = new Point(-8, 1),
                Name = "_groupBox",
                Text = ""
            };
            Controls.Add(_groupBox);
            AutoScaleDimensions = new SizeF(6F, 13F);
            AutoScaleMode = AutoScaleMode.Font;
            Name = "Separator";
            ResumeLayout(false);
            SetStyle(ControlStyles.Selectable, false);
        }

        /// <summary>
        /// Gets/sets the text displayed in the control. Set to an empty string in order
        /// to get a horizontal line separator.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public override string Text
        {
            get { return _groupBox.Text; }
            set { _groupBox.Text = value; }
        }
    }
}
