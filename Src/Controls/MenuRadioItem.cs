using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace RT.Util.Controls
{
    public class MenuRadioItem<ValueType> : ToolStripMenuItem
        where ValueType : struct
    {
        private ValueType FValue;
        private MenuRadioGroup<ValueType> FParentGroup;

        public ValueType Value {
            get { return FValue; }
            set { FValue = value; }
        }
        public MenuRadioGroup<ValueType> ParentGroup {
            get { return FParentGroup; }
            set { FParentGroup = value; }
        }

        public MenuRadioItem() : base()
        {
            this.Click += new EventHandler(MenuRadioItem_Click);
        }

        private void MenuRadioItem_Click(object sender, EventArgs e)
        {
            if (FParentGroup != null)
                FParentGroup.SetValue(FValue);
        }
    }
}
