using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Windows.Forms;

namespace RT.Util.Controls
{
    /// <summary>
    /// Keeps track of a group of menu items (specifically, <see cref="MenuRadioItem&lt;ValueType&gt;"/>)
    /// which are intended to act like a radio-button group, and where each menu item is associated with
    /// a specific value, usually from an enum type.
    /// </summary>
    /// <typeparam name="ValueType">The type of the value associated with each menu item.</typeparam>
    public class MenuRadioGroup<ValueType> : Component where ValueType : struct
    {
        private List<MenuRadioItem<ValueType>> FMembers = new List<MenuRadioItem<ValueType>>();

        /// <summary>Returns an array of all the menu items contained in this group.</summary>
        public MenuRadioItem<ValueType>[] Members
        {
            get { return FMembers.ToArray(); }
        }

        /// <summary>Returns the value associated with the currently-selected menu item.</summary>
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

        /// <summary>Triggers whenever a different menu item becomes the selected one.</summary>
        public event EventHandler ValueChanged;

        /// <summary>Causes the menu item with the specified value to become the selected one.
        /// If no menu item in the group is associated with the specified value, nothing happens.
        /// If more than one menu item in the group is associated with the same value, the
        /// behaviour is undefined.</summary>
        /// <param name="Value">The value whose associated menu item is to be selected.</param>
        public void SetValue(ValueType Value)
        {
            if (FMembers != null)
                foreach (MenuRadioItem<ValueType> i in FMembers)
                    i.Checked = (i.Value.Equals(Value));
            if (ValueChanged != null)
                ValueChanged(this, new EventArgs());
        }

        /// <summary>Adds a menu item to the group.</summary>
        /// <param name="Member">Menu item to be added to the group.</param>
        public void AddMember(MenuRadioItem<ValueType> Member)
        {
            if (!FMembers.Contains(Member))
            {
                FMembers.Add(Member);
                Member.ParentGroup = this;
            }
        }

        /// <summary>Returns the menu item associated with the specified value, or null
        /// if no menu item in the group is associated with the specified value.</summary>
        /// <param name="Value">The value for which to find the menu item.</param>
        public MenuRadioItem<ValueType> GetItemFromValue(ValueType Value)
        {
            if (FMembers != null)
                foreach (MenuRadioItem<ValueType> i in FMembers)
                    if (i.Value.Equals(Value))
                        return i;
            return null;
        }
    }
}
