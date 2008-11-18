using System.ComponentModel;
using System.Windows.Forms;

namespace RT.Util.Controls
{
    /// <summary>
    /// This control is a label with a horizontal line running to the right of the label.
    /// The purpose is to visually separate blocks of controls, similar to GroupBox but
    /// without completely surrounding the controls in a box.
    /// </summary>
    public partial class Separator : UserControl
    {
        /// <summary></summary>
        public Separator()
        {
            InitializeComponent();
            this.SetStyle(ControlStyles.Selectable, false);
            GB.Text = ""; // because the designer will only store a value for the Text
            // property if it is not "", i.e. the Text property must default to "".
        }

        /// <summary>
        /// Gets/sets the text displayed in the control. Set to an empty string in order
        /// to get a horizontal line separator.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public override string Text
        {
            get { return GB.Text; }
            set { GB.Text = value; }
        }
    }
}
