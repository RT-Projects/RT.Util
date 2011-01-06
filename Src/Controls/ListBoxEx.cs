using System.Windows.Forms;

namespace RT.Util.Controls
{
    /// <summary>Provides a ListBox that has extra methods to refresh items.</summary>
    /// <remarks>The extra methods just call base methods which are inexplicably protected.</remarks>
    public class ListBoxEx : ListBox
    {
        /// <summary>Refreshes all ListBox items and retrieves new strings for them.</summary>
        public new void RefreshItems() { base.RefreshItems(); }

        /// <summary>Refreshes the item contained at the specified index.</summary>
        /// <param name="index">The zero-based index of the element to refresh.</param>
        public new void RefreshItem(int index) { base.RefreshItem(index); }
    }
}
