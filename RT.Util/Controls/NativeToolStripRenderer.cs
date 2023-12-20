using System.Runtime.InteropServices;
using System.Windows.Forms.VisualStyles;

// MIT License. Original code from http://code.google.com/p/szotar/.

namespace RT.Util.Controls;

/// <summary>Defines values for several standard native toolbar themes.</summary>
public enum NativeToolbarTheme
{
    /// <summary>Standard toolbar theme - same as the native menu.</summary>
    Toolbar,
    /// <summary>A fancy colored theme (black on Win7).</summary>
    MediaToolbar,
    /// <summary>A fancy colored theme (blue on Win7).</summary>
    CommunicationsToolbar,
    /// <summary>A fancy colored theme.</summary>
    BrowserTabBar,
    /// <summary>A fancy colored theme (light on Win7).</summary>
    HelpBar
}

/// <summary>Renders a toolstrip using the UxTheme API via VisualStyleRenderer and a specific style.</summary>
public class NativeToolStripRenderer : ToolStripSystemRenderer
{
    private VisualStyleRenderer _renderer;

    /// <summary>Gets/sets the type of theme to use.</summary>
    public NativeToolbarTheme Theme { get; set; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MARGINS
    {
        public int cxLeftWidth;
        public int cxRightWidth;
        public int cyTopHeight;
        public int cyBottomHeight;
    }

    /// <summary>
    ///     It shouldn't be necessary to P/Invoke like this, however VisualStyleRenderer.GetMargins misses out a parameter in
    ///     its own P/Invoke.</summary>
    [DllImport("uxtheme.dll")]
    private extern static int GetThemeMargins(IntPtr hTheme, IntPtr hdc, int iPartId, int iStateId, int iPropId, IntPtr rect, out MARGINS pMargins);

    // See http://msdn2.microsoft.com/en-us/library/bb773210.aspx - "Parts and States"
    // Only menu-related parts/states are needed here, VisualStyleRenderer handles most of the rest.
    private enum MenuParts : int
    {
        ItemTMSchema = 1,
        DropDownTMSchema = 2,
        BarItemTMSchema = 3,
        BarDropDownTMSchema = 4,
        ChevronTMSchema = 5,
        SeparatorTMSchema = 6,
        BarBackground = 7,
        BarItem = 8,
        PopupBackground = 9,
        PopupBorders = 10,
        PopupCheck = 11,
        PopupCheckBackground = 12,
        PopupGutter = 13,
        PopupItem = 14,
        PopupSeparator = 15,
        PopupSubmenu = 16,
        SystemClose = 17,
        SystemMaximize = 18,
        SystemMinimize = 19,
        SystemRestore = 20
    }

    private enum MenuBarStates : int
    {
        Active = 1,
        Inactive = 2
    }

    private enum MenuBarItemStates : int
    {
        Normal = 1,
        Hover = 2,
        Pushed = 3,
        Disabled = 4,
        DisabledHover = 5,
        DisabledPushed = 6
    }

    private enum MenuPopupItemStates : int
    {
        Normal = 1,
        Hover = 2,
        Disabled = 3,
        DisabledHover = 4
    }

    private enum MenuPopupCheckStates : int
    {
        CheckmarkNormal = 1,
        CheckmarkDisabled = 2,
        BulletNormal = 3,
        BulletDisabled = 4
    }

    private enum MenuPopupCheckBackgroundStates : int
    {
        Disabled = 1,
        Normal = 2,
        Bitmap = 3
    }

    private enum MenuPopupSubMenuStates : int
    {
        Normal = 1,
        Disabled = 2
    }

    private enum MarginTypes : int
    {
        Sizing = 3601,
        Content = 3602,
        Caption = 3603
    }

    private static readonly int RebarBackground = 6;

    private Padding GetThemeMargins(IDeviceContext dc, MarginTypes marginType)
    {
        MARGINS margins;
        try
        {
            IntPtr hDC = dc.GetHdc();
            if (0 == GetThemeMargins(_renderer.Handle, hDC, _renderer.Part, _renderer.State, (int) marginType, IntPtr.Zero, out margins))
                return new Padding(margins.cxLeftWidth, margins.cyTopHeight, margins.cxRightWidth, margins.cyBottomHeight);
            return new Padding(0);
        }
        finally
        {
            dc.ReleaseHdc();
        }
    }

    private static int GetItemState(ToolStripItem item)
    {
        bool hot = item.Selected;

        if (item.IsOnDropDown)
        {
            if (item.Enabled)
                return hot ? (int) MenuPopupItemStates.Hover : (int) MenuPopupItemStates.Normal;
            return hot ? (int) MenuPopupItemStates.DisabledHover : (int) MenuPopupItemStates.Disabled;
        }
        else
        {
            if (item.Pressed)
                return item.Enabled ? (int) MenuBarItemStates.Pushed : (int) MenuBarItemStates.DisabledPushed;
            if (item.Enabled)
                return hot ? (int) MenuBarItemStates.Hover : (int) MenuBarItemStates.Normal;
            return hot ? (int) MenuBarItemStates.DisabledHover : (int) MenuBarItemStates.Disabled;
        }
    }

    private string RebarClass
    {
        get
        {
            return SubclassPrefix + "Rebar";
        }
    }

    private string ToolbarClass
    {
        get
        {
            return SubclassPrefix + "ToolBar";
        }
    }

    private string MenuClass
    {
        get
        {
            return SubclassPrefix + "Menu";
        }
    }

    private string SubclassPrefix
    {
        get
        {
            switch (Theme)
            {
                case NativeToolbarTheme.MediaToolbar: return "Media::";
                case NativeToolbarTheme.CommunicationsToolbar: return "Communications::";
                case NativeToolbarTheme.BrowserTabBar: return "BrowserTabBar::";
                case NativeToolbarTheme.HelpBar: return "Help::";
                default: return string.Empty;
            }
        }
    }

    private VisualStyleElement Subclass(VisualStyleElement element)
    {
        return VisualStyleElement.CreateElement(SubclassPrefix + element.ClassName,
                element.Part, element.State);
    }

    private bool EnsureRenderer()
    {
        if (!IsSupported)
            return false;

        if (_renderer == null)
            _renderer = new VisualStyleRenderer(VisualStyleElement.Button.PushButton.Normal);

        return true;
    }

    /// <summary>Gives parented ToolStrips a transparent background.</summary>
    protected override void Initialize(ToolStrip toolStrip)
    {
        if (toolStrip.Parent is ToolStripPanel)
            toolStrip.BackColor = Color.Transparent;

        base.Initialize(toolStrip);
    }

    /// <summary>
    ///     Using just ToolStripManager.Renderer without setting the Renderer individually per ToolStrip means that the
    ///     ToolStrip is not passed to the Initialize method. ToolStripPanels, however, are. So we can simply initialize it
    ///     here too, and this should guarantee that the ToolStrip is initialized at least once. Hopefully it isn't any more
    ///     complicated than this.</summary>
    protected override void InitializePanel(ToolStripPanel toolStripPanel)
    {
        foreach (Control control in toolStripPanel.Controls)
            if (control is ToolStrip)
                Initialize((ToolStrip) control);

        base.InitializePanel(toolStripPanel);
    }

    /// <summary>Override - see base.</summary>
    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        if (EnsureRenderer())
        {
            _renderer.SetParameters(MenuClass, (int) MenuParts.PopupBorders, 0);
            if (e.ToolStrip.IsDropDown)
            {
                Region oldClip = e.Graphics.Clip;

                // Tool strip borders are rendered *after* the content, for some reason.
                // So we have to exclude the inside of the popup otherwise we'll draw over it.
                Rectangle insideRect = e.ToolStrip.ClientRectangle;
                insideRect.Inflate(-1, -1);
                e.Graphics.ExcludeClip(insideRect);

                _renderer.DrawBackground(e.Graphics, e.ToolStrip.ClientRectangle, e.AffectedBounds);

                // Restore the old clip in case the Graphics is used again (does that ever happen?)
                e.Graphics.Clip = oldClip;
            }
        }
        else
        {
            base.OnRenderToolStripBorder(e);
        }
    }

    Rectangle GetBackgroundRectangle(ToolStripItem item)
    {
        if (!item.IsOnDropDown)
            return new Rectangle(new Point(), item.Bounds.Size);

        // For a drop-down menu item, the background rectangles of the items should be touching vertically.
        // This ensures that's the case.
        Rectangle rect = item.Bounds;

        // The background rectangle should be inset two pixels horizontally (on both sides), but we have 
        // to take into account the border.
        rect.X = item.ContentRectangle.X + 1;
        rect.Width = item.ContentRectangle.Width - 1;

        // Make sure we're using all of the vertical space, so that the edges touch.
        rect.Y = 0;
        return rect;
    }

    /// <summary>Override - see base.</summary>
    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        if (EnsureRenderer())
        {
            int partID = e.Item.IsOnDropDown ? (int) MenuParts.PopupItem : (int) MenuParts.BarItem;
            _renderer.SetParameters(MenuClass, partID, GetItemState(e.Item));

            Rectangle bgRect = GetBackgroundRectangle(e.Item);
            _renderer.DrawBackground(e.Graphics, bgRect, bgRect);
        }
        else
        {
            base.OnRenderMenuItemBackground(e);
        }
    }

    /// <summary>Override - see base.</summary>
    protected override void OnRenderToolStripPanelBackground(ToolStripPanelRenderEventArgs e)
    {
        if (EnsureRenderer())
        {
            // Draw the background using Rebar & RP_BACKGROUND (or, if that is not available, fall back to
            // Rebar.Band.Normal)
            if (VisualStyleRenderer.IsElementDefined(VisualStyleElement.CreateElement(RebarClass, RebarBackground, 0)))
            {
                _renderer.SetParameters(RebarClass, RebarBackground, 0);
            }
            else
            {
                _renderer.SetParameters(RebarClass, 0, 0);
            }

            if (_renderer.IsBackgroundPartiallyTransparent())
                _renderer.DrawParentBackground(e.Graphics, e.ToolStripPanel.ClientRectangle, e.ToolStripPanel);

            _renderer.DrawBackground(e.Graphics, e.ToolStripPanel.ClientRectangle);

            e.Handled = true;
        }
        else
        {
            base.OnRenderToolStripPanelBackground(e);
        }
    }

    /// <summary>Render the background of an actual menu bar, dropdown menu or toolbar.</summary>
    protected override void OnRenderToolStripBackground(System.Windows.Forms.ToolStripRenderEventArgs e)
    {
        if (EnsureRenderer())
        {
            if (e.ToolStrip.IsDropDown)
            {
                _renderer.SetParameters(MenuClass, (int) MenuParts.PopupBackground, 0);
            }
            else
            {
                // It's a MenuStrip or a ToolStrip. If it's contained inside a larger panel, it should have a
                // transparent background, showing the panel's background.

                if (e.ToolStrip.Parent is ToolStripPanel)
                {
                    // The background should be transparent, because the ToolStripPanel's background will be visible.
                    // (Of course, we assume the ToolStripPanel is drawn using the same theme, but it's not my fault
                    // if someone does that.)
                    return;
                }
                else
                {
                    // A lone toolbar/menubar should act like it's inside a toolbox, I guess.
                    // Maybe I should use the MenuClass in the case of a MenuStrip, although that would break
                    // the other themes...
                    if (VisualStyleRenderer.IsElementDefined(VisualStyleElement.CreateElement(RebarClass, RebarBackground, 0)))
                        _renderer.SetParameters(RebarClass, RebarBackground, 0);
                    else
                        _renderer.SetParameters(RebarClass, 0, 0);
                }
            }

            if (_renderer.IsBackgroundPartiallyTransparent())
                _renderer.DrawParentBackground(e.Graphics, e.ToolStrip.ClientRectangle, e.ToolStrip);

            _renderer.DrawBackground(e.Graphics, e.ToolStrip.ClientRectangle, e.AffectedBounds);
        }
        else
        {
            base.OnRenderToolStripBackground(e);
        }
    }

    /// <summary>
    ///     The only purpose of this override is to change the arrow colour. It's OK to just draw over the default arrow since
    ///     we also pass down arrow drawing to the system renderer.</summary>
    protected override void OnRenderSplitButtonBackground(ToolStripItemRenderEventArgs e)
    {
        if (EnsureRenderer())
        {
            ToolStripSplitButton sb = (ToolStripSplitButton) e.Item;
            base.OnRenderSplitButtonBackground(e);

            // It doesn't matter what colour of arrow we tell it to draw. OnRenderArrow will compute it from the item anyway.
            OnRenderArrow(new ToolStripArrowRenderEventArgs(e.Graphics, sb, sb.DropDownButtonBounds, Color.Red, ArrowDirection.Down));
        }
        else
        {
            base.OnRenderSplitButtonBackground(e);
        }
    }

    Color GetItemTextColor(ToolStripItem item)
    {
        int partId = item.IsOnDropDown ? (int) MenuParts.PopupItem : (int) MenuParts.BarItem;
        _renderer.SetParameters(MenuClass, partId, GetItemState(item));
        return _renderer.GetColor(ColorProperty.TextColor);
    }

    /// <summary>Override - see base.</summary>
    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        if (EnsureRenderer())
            e.TextColor = GetItemTextColor(e.Item);

        base.OnRenderItemText(e);
    }

    /// <summary>Override - see base.</summary>
    protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
    {
        if (EnsureRenderer())
        {
            if (e.ToolStrip.IsDropDown)
            {
                _renderer.SetParameters(MenuClass, (int) MenuParts.PopupGutter, 0);
                // The AffectedBounds is usually too small, way too small to look right. Instead of using that,
                // use the AffectedBounds but with the right width. Then narrow the rectangle to the correct edge
                // based on whether or not it's RTL. (It doesn't need to be narrowed to an edge in LTR mode, but let's
                // do that anyway.)
                // Using the DisplayRectangle gets roughly the right size so that the separator is closer to the text.
                Padding margins = GetThemeMargins(e.Graphics, MarginTypes.Sizing);
                int extraWidth = (e.ToolStrip.Width - e.ToolStrip.DisplayRectangle.Width - margins.Left - margins.Right - 1) - e.AffectedBounds.Width;
                Rectangle rect = e.AffectedBounds;
                rect.Y += 2;
                rect.Height -= 4;
                int sepWidth = _renderer.GetPartSize(e.Graphics, ThemeSizeType.True).Width;
                if (e.ToolStrip.RightToLeft == RightToLeft.Yes)
                {
                    rect = new Rectangle(rect.X - extraWidth, rect.Y, sepWidth, rect.Height);
                    rect.X += sepWidth;
                }
                else
                {
                    rect = new Rectangle(rect.Width + extraWidth - sepWidth, rect.Y, sepWidth, rect.Height);
                }
                _renderer.DrawBackground(e.Graphics, rect);
            }
        }
        else
        {
            base.OnRenderImageMargin(e);
        }
    }

    /// <summary>Override - see base.</summary>
    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        if (e.ToolStrip.IsDropDown && EnsureRenderer())
        {
            _renderer.SetParameters(MenuClass, (int) MenuParts.PopupSeparator, 0);
            Rectangle rect = new Rectangle(e.ToolStrip.DisplayRectangle.Left, 0, e.ToolStrip.DisplayRectangle.Width, e.Item.Height);
            _renderer.DrawBackground(e.Graphics, rect, rect);
        }
        else
        {
            base.OnRenderSeparator(e);
        }
    }

    /// <summary>Override - see base.</summary>
    protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
    {
        if (EnsureRenderer())
        {
            Rectangle bgRect = GetBackgroundRectangle(e.Item);
            bgRect.Width = bgRect.Height;

            // Now, mirror its position if the menu item is RTL.
            if (e.Item.RightToLeft == RightToLeft.Yes)
                bgRect = new Rectangle(e.ToolStrip.ClientSize.Width - bgRect.X - bgRect.Width, bgRect.Y, bgRect.Width, bgRect.Height);

            _renderer.SetParameters(MenuClass, (int) MenuParts.PopupCheckBackground, e.Item.Enabled ? (int) MenuPopupCheckBackgroundStates.Normal : (int) MenuPopupCheckBackgroundStates.Disabled);
            _renderer.DrawBackground(e.Graphics, bgRect);

            Rectangle checkRect = e.ImageRectangle;
            checkRect.X = bgRect.X + bgRect.Width / 2 - checkRect.Width / 2;
            checkRect.Y = bgRect.Y + bgRect.Height / 2 - checkRect.Height / 2;

            // I don't think ToolStrip even supports radio box items, so no need to render them.
            _renderer.SetParameters(MenuClass, (int) MenuParts.PopupCheck, e.Item.Enabled ? (int) MenuPopupCheckStates.CheckmarkNormal : (int) MenuPopupCheckStates.CheckmarkDisabled);

            _renderer.DrawBackground(e.Graphics, checkRect);
        }
        else
        {
            base.OnRenderItemCheck(e);
        }
    }

    /// <summary>Override - see base.</summary>
    protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
    {
        // The default renderer will draw an arrow for us (the UXTheme API seems not to have one for all directions),
        // but it will get the colour wrong in many cases. The text colour is probably the best colour to use.
        if (EnsureRenderer())
            e.ArrowColor = GetItemTextColor(e.Item);
        base.OnRenderArrow(e);
    }

    /// <summary>Override - see base.</summary>
    protected override void OnRenderOverflowButtonBackground(ToolStripItemRenderEventArgs e)
    {
        if (EnsureRenderer())
        {
            // BrowserTabBar::Rebar draws the chevron using the default background. Odd.
            string rebarClass = RebarClass;
            if (Theme == NativeToolbarTheme.BrowserTabBar)
                rebarClass = "Rebar";

            int state = VisualStyleElement.Rebar.Chevron.Normal.State;
            if (e.Item.Pressed)
                state = VisualStyleElement.Rebar.Chevron.Pressed.State;
            else if (e.Item.Selected)
                state = VisualStyleElement.Rebar.Chevron.Hot.State;

            _renderer.SetParameters(rebarClass, VisualStyleElement.Rebar.Chevron.Normal.Part, state);
            _renderer.DrawBackground(e.Graphics, new Rectangle(Point.Empty, e.Item.Size));
        }
        else
        {
            base.OnRenderOverflowButtonBackground(e);
        }
    }

    /// <summary>Gets a value indicating whether a native theme can be used.</summary>
    public bool IsSupported
    {
        get
        {
            if (!VisualStyleRenderer.IsSupported)
                return false;

            // Needs a more robust check. It seems mono supports very different style sets.
            return
                    VisualStyleRenderer.IsElementDefined(
                            VisualStyleElement.CreateElement("Menu",
                                    (int) MenuParts.BarBackground,
                                    (int) MenuBarStates.Active));
        }
    }
}
