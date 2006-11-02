using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace RT.Util.Controls
{
    public partial class Separator : UserControl
    {
        public Separator()
        {
            InitializeComponent();
        }

        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public override string Text
        {
            get { return GB.Text; }
            set { GB.Text = value; }
        }
    }
}
