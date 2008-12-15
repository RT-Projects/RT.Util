using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Windows.Forms;

namespace RT.Util.Controls
{
    /// <summary>
    /// Keeps track of a group of menu items (specifically, <see cref="MenuRadioItem&lt;T&gt;"/>)
    /// which are intended to act like a radio-button group, and where each menu item is associated with
    /// a specific value, usually from an enum type.
    /// </summary>
    /// <typeparam name="T">The type of the value associated with each menu item.</typeparam>
    public class MenuRadioGroup<T> : Component where T : struct
    {
        private List<MenuRadioItem<T>> _members = new List<MenuRadioItem<T>>();

        /// <summary>Returns an array of all the menu items contained in this group.</summary>
        public MenuRadioItem<T>[] Members
        {
            get { return _members.ToArray(); }
        }

        /// <summary>Returns the value associated with the currently-selected menu item.</summary>
        public T Value
        {
            get
            {
                if (_members != null)
                    foreach (MenuRadioItem<T> i in _members)
                        if (i.Checked)
                            return i.Value;
                return default(T);
            }
        }

        /// <summary>Triggers whenever a different menu item becomes the selected one.</summary>
        public event EventHandler ValueChanged;

        /// <summary>Causes the menu item with the specified value to become the selected one.
        /// If no menu item in the group is associated with the specified value, nothing happens.
        /// If more than one menu item in the group is associated with the same value, the
        /// behaviour is undefined.</summary>
        /// <param name="value">The value whose associated menu item is to be selected.</param>
        public void SetValue(T value)
        {
            if (_members != null)
                foreach (MenuRadioItem<T> i in _members)
                    i.Checked = (i.Value.Equals(value));
            if (ValueChanged != null)
                ValueChanged(this, new EventArgs());
        }

        /// <summary>Adds a menu item to the group.</summary>
        /// <param name="member">Menu item to be added to the group.</param>
        public void AddMember(MenuRadioItem<T> member)
        {
            if (!_members.Contains(member))
            {
                _members.Add(member);
                member.ParentGroup = this;
            }
        }

        /// <summary>Returns the menu item associated with the specified value, or null
        /// if no menu item in the group is associated with the specified value.</summary>
        /// <param name="value">The value for which to find the menu item.</param>
        public MenuRadioItem<T> GetItemFromValue(T value)
        {
            if (_members != null)
                foreach (MenuRadioItem<T> i in _members)
                    if (i.Value.Equals(value))
                        return i;
            return null;
        }
    }
}
