using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RT.Util.ExtensionMethods
{
    /// <summary>
    /// Provides extension methods for Windows Forms controls.
    /// </summary>
    public static class WindowsFormsExtensions
    {
        /// <summary>
        /// If this control is located within a <see cref="TabPage"/>, returns that TabPage
        /// by iterating recursively through this item's parents. Otherwise returns null.
        /// </summary>
        public static TabPage ParentTab(this Control control)
        {
            while (control != null)
            {
                if (control.Parent is TabPage)
                    return control.Parent as TabPage;
                control = control.Parent;
            }
            return null;
        }

        /// <summary>Same as Control.Invoke, except that the action is not invoked immediately
        /// if we are on the GUI thread. Instead, it is scheduled to be run the next time the GUI thread is idle.</summary>
        /// <param name="invokable">Control to invoke action on.</param>
        /// <param name="action">Action to invoke.</param>
        public static void InvokeLater(this Control invokable, Action action)
        {
            Task.Factory.StartNew(() => { invokable.Invoke(action); });
        }
    }
}
