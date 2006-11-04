using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Windows.Forms;

namespace RT.Util.Controls
{
    public class MenuRadioGroup<ValueType> : Component
        where ValueType : struct
    {
        private MenuRadioItem<ValueType>[] FMembers = null;

        public MenuRadioItem<ValueType>[] Members
        {
            get { return FMembers; }
            set { FMembers = value; }
        }

        public ValueType Value
        {
            get
            {
                if (FMembers != null)
                    foreach (MenuRadioItem<ValueType> i in FMembers)
                        if (i.Checked)
                            return i.Value;
                return default(ValueType);
            }
        }

        public event EventHandler ValueChanged;

        public void SetValue(ValueType Value)
        {
            if (FMembers != null)
                foreach (MenuRadioItem<ValueType> i in FMembers)
                    i.Checked = (i.Value.Equals(Value));
            if (ValueChanged != null)
                ValueChanged(this, new EventArgs());
        }
    }
}
