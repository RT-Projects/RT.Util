using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace RT.Util.Controls
{
    /// <summary>Provides a ListBox with added functionality. See remarks.</summary>
    /// <remarks>
    /// <para>The extra methods <see cref="RefreshItem"/> and <see cref="RefreshItems"/> just call base methods which are inexplicably protected.</para>
    /// <para>When setting <c>SelectionMode</c> to <c>SelectionMode.MultiExtended</c>, this listbox supports Ctrl+Arrow keys and Ctrl+Space properly, unlike the standard ListBox where these don’t work.</para>
    /// </remarks>
    public class ListBoxEx : ListBox
    {
        /// <summary>Refreshes all ListBox items and retrieves new strings for them.</summary>
        public new void RefreshItems() { base.RefreshItems(); }

        /// <summary>Refreshes the item contained at the specified index.</summary>
        /// <param name="index">The zero-based index of the element to refresh.</param>
        public new void RefreshItem(int index) { base.RefreshItem(index); }

        /// <summary>Override; see base.</summary>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyData == (Keys.Control | Keys.A))
            {
                if (SelectionMode == SelectionMode.MultiExtended || SelectionMode == SelectionMode.MultiSimple)
                    for (int i = 0; i < Items.Count; i++)
                        SetSelected(i, true);
                e.Handled = true;
                _ignoreOneKeyPress = true;
                return;
            }

            if (SelectionMode != SelectionMode.MultiExtended)
            {
                base.OnKeyDown(e);
                return;
            }

            if (e.KeyData == (Keys.Control | Keys.Up))
            {
                var index = OutlineIndex;
                if (index > 0)
                    OutlineIndex = index - 1;
                e.Handled = true;
            }
            else if (e.KeyData == (Keys.Control | Keys.Down))
            {
                var index = OutlineIndex;
                if (index < Items.Count - 1)
                    OutlineIndex = index + 1;
                e.Handled = true;
            }
            else if (e.KeyData == (Keys.Control | Keys.Space))
            {
                var index = OutlineIndex;
                SetSelected(index, !GetSelected(index));
                e.Handled = true;
            }
            else
                base.OnKeyDown(e);
        }

        private bool _ignoreOneKeyPress = false;

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            if (_ignoreOneKeyPress)
            {
                e.Handled = true;
                _ignoreOneKeyPress = false;
            }
            else
                base.OnKeyPress(e);
        }

        /// <summary>Gets or sets the zero-based index of the list item that has the dotted outline.</summary>
        /// <remarks>
        /// <para>The list item may be selected or not.</para>
        /// <para>Changing this property does not fire a SelectedIndexChanged event.</para>
        /// </remarks>
        [DefaultValue(0)]
        public int OutlineIndex
        {
            get
            {
                return (int) WinAPI.SendMessage(Handle, WinAPI.LB_GETCARETINDEX, 0, 0);
            }
            set
            {
                if (value < 0 || value >= Items.Count)
                    throw new ArgumentException("OutlineIndex cannot be negative or greater than the size of the collection.", "value");
                WinAPI.SendMessage(Handle, WinAPI.LB_SETCARETINDEX, (uint) value, 0);
            }
        }
    }
}
