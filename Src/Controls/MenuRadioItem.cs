using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace RT.Util.Controls
{
    /// <summary>
    /// Encapsulates a menu item that is associated with a value from a specified type, usually an enum.
    /// These menu items are intended to be grouped using a <see cref="MenuRadioGroup&lt;T&gt;"/>.
    /// </summary>
    /// <typeparam name="T">The type of the value associated with this menu item.</typeparam>
    public sealed class MenuRadioItem<T> : ToolStripMenuItem where T : struct
    {
        private T _value;
        private MenuRadioGroup<T> _parentGroup;

        /// <summary>Returns the value associated with this menu item.</summary>
        public T Value
        {
            get { return _value; }
            set { _value = value; }
        }

        /// <summary>Returns the group to which this menu item belongs,
        /// or moves it to a different group.</summary>
        public MenuRadioGroup<T> ParentGroup
        {
            get { return _parentGroup; }
            set
            {
                if (_parentGroup != value)
                {
                    _parentGroup = value;
                    _parentGroup.AddMember(this);
                }
            }
        }

        /// <summary>Initialises a <see cref="MenuRadioItem&lt;T&gt;"/> instance.</summary>
        public MenuRadioItem()
            : base()
        {
            this.Click += new EventHandler(MenuRadioItem_Click);
        }

        private void MenuRadioItem_Click(object sender, EventArgs e)
        {
            if (_parentGroup != null)
                _parentGroup.SetValue(_value);
        }
    }
}
