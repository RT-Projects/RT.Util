using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace RT.Util.Controls
{
    /// <summary>
    /// Encapsulates a menu item that is associated with a value from a specified type, usually an enum.
    /// These menu items are intended to be grouped using a <see cref="MenuRadioGroup&lt;ValueType&gt;"/>.
    /// </summary>
    /// <typeparam name="ValueType">The type of the value associated with this menu item.</typeparam>
    public class MenuRadioItem<ValueType> : ToolStripMenuItem where ValueType : struct
    {
        private ValueType FValue;
        private MenuRadioGroup<ValueType> FParentGroup;

        /// <summary>Returns the value associated with this menu item.</summary>
        public ValueType Value
        {
            get { return FValue; }
            set { FValue = value; }
        }

        /// <summary>Returns the group to which this menu item belongs,
        /// or moves it to a different group.</summary>
        public MenuRadioGroup<ValueType> ParentGroup
        {
            get { return FParentGroup; }
            set
            {
                if (FParentGroup != value)
                {
                    FParentGroup = value;
                    FParentGroup.AddMember(this);
                }
            }
        }

        /// <summary>Initialises a <see cref="MenuRadioItem&lt;ValueType&gt;"/> instance.</summary>
        public MenuRadioItem()
            : base()
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
