namespace RT.Util.ExtensionMethods;

/// <summary>Provides extension methods for Windows Forms controls.</summary>
public static class WindowsFormsExtensions
{
    /// <summary>
    ///     If this control is located within a <see cref="TabPage"/>, returns the first TabPage found by iterating
    ///     recursively through its parents. Otherwise returns null.</summary>
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
}
