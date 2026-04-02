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
#pragma warning disable IDE1006 // Naming Styles

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
    private static extern int GetThemeMargins(IntPtr hTheme, IntPtr hdc, int iPartId, int iStateId, int iPropId, IntPtr rect, out MARGINS pMargins);

    // See http://msdn2.microsoft.com/en-us/library/bb773210.aspx - "Parts and States"
    // Only menu-related parts/states are needed here, VisualStyleRenderer handles most of the rest.
    private enum menuParts : int
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

    private enum menuBarStates : int
    {
        Active = 1,
        Inactive = 2
    }

    private enum menuBarItemStates : int
    {
        Normal = 1,
        Hover = 2,
        Pushed = 3,
        Disabled = 4,
        DisabledHover = 5,
        DisabledPushed = 6
    }

    private enum menuPopupItemStates : int
    {
        Normal = 1,
        Hover = 2,
        Disabled = 3,
        DisabledHover = 4
    }

    private enum menuPopupCheckStates : int
    {
        CheckmarkNormal = 1,
        CheckmarkDisabled = 2,
        BulletNormal = 3,
        BulletDisabled = 4
    }

    private enum menuPopupCheckBackgroundStates : int
    {
        Disabled = 1,
        Normal = 2,
        Bitmap = 3
    }

    private enum menuPopupSubMenuStates : int
    {
        Normal = 1,
        Disabled = 2
    }

    private enum marginTypes : int
    {
        Sizing = 3601,
        Content = 3602,
        Caption = 3603
    }

    private const int RebarBackground = 6;

    private Padding getThemeMargins(Graphics dc, marginTypes marginType)
    {
        try
        {
            var hDC = dc.GetHdc();
            return 0 == GetThemeMargins(_renderer.Handle, hDC, _renderer.Part, _renderer.State, (int) marginType, IntPtr.Zero, out var margins)
                ? new Padding(margins.cxLeftWidth, margins.cyTopHeight, margins.cxRightWidth, margins.cyBottomHeight)
                : new Padding(0);
        }
        finally
        {
            dc.ReleaseHdc();
        }
    }

    private static int getItemState(ToolStripItem item)
    {
        var hot = item.Selected;

        if (item.IsOnDropDown)
        {
            return item.Enabled
                ? hot ? (int) menuPopupItemStates.Hover : (int) menuPopupItemStates.Normal
                : hot ? (int) menuPopupItemStates.DisabledHover : (int) menuPopupItemStates.Disabled;
        }
        else
        {
            return item.Pressed
                ? item.Enabled ? (int) menuBarItemStates.Pushed : (int) menuBarItemStates.DisabledPushed
                : item.Enabled
                ? hot ? (int) menuBarItemStates.Hover : (int) menuBarItemStates.Normal
                : hot ? (int) menuBarItemStates.DisabledHover : (int) menuBarItemStates.Disabled;
        }
    }

    private string rebarClass => subclassPrefix + "Rebar";

    private string menuClass => subclassPrefix + "Menu";

    private string subclassPrefix => Theme switch
    {
        NativeToolbarTheme.MediaToolbar => "Media::",
        NativeToolbarTheme.CommunicationsToolbar => "Communications::",
        NativeToolbarTheme.BrowserTabBar => "BrowserTabBar::",
        NativeToolbarTheme.HelpBar => "Help::",
        _ => string.Empty,
    };

    private bool ensureRenderer()
    {
        if (!IsSupported)
            return false;

        _renderer ??= new VisualStyleRenderer(VisualStyleElement.Button.PushButton.Normal);

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
            if (control is ToolStrip strip)
                Initialize(strip);

        base.InitializePanel(toolStripPanel);
    }

    /// <summary>Override - see base.</summary>
    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        if (ensureRenderer())
        {
            _renderer.SetParameters(menuClass, (int) menuParts.PopupBorders, 0);
            if (e.ToolStrip.IsDropDown)
            {
                var oldClip = e.Graphics.Clip;

                // Tool strip borders are rendered *after* the content, for some reason.
                // So we have to exclude the inside of the popup otherwise we'll draw over it.
                var insideRect = e.ToolStrip.ClientRectangle;
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

    private static Rectangle getBackgroundRectangle(ToolStripItem item)
    {
        if (!item.IsOnDropDown)
            return new Rectangle(new Point(), item.Bounds.Size);

        // For a drop-down menu item, the background rectangles of the items should be touching vertically.
        // This ensures that's the case.
        var rect = item.Bounds;

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
        if (ensureRenderer())
        {
            var partID = e.Item.IsOnDropDown ? (int) menuParts.PopupItem : (int) menuParts.BarItem;
            _renderer.SetParameters(menuClass, partID, getItemState(e.Item));

            var bgRect = getBackgroundRectangle(e.Item);
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
        if (ensureRenderer())
        {
            // Draw the background using Rebar & RP_BACKGROUND (or, if that is not available, fall back to
            // Rebar.Band.Normal)
            if (VisualStyleRenderer.IsElementDefined(VisualStyleElement.CreateElement(rebarClass, RebarBackground, 0)))
            {
                _renderer.SetParameters(rebarClass, RebarBackground, 0);
            }
            else
            {
                _renderer.SetParameters(rebarClass, 0, 0);
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
        if (ensureRenderer())
        {
            if (e.ToolStrip.IsDropDown)
            {
                _renderer.SetParameters(menuClass, (int) menuParts.PopupBackground, 0);
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
                    if (VisualStyleRenderer.IsElementDefined(VisualStyleElement.CreateElement(rebarClass, RebarBackground, 0)))
                        _renderer.SetParameters(rebarClass, RebarBackground, 0);
                    else
                        _renderer.SetParameters(rebarClass, 0, 0);
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
        if (ensureRenderer())
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

    private Color getItemTextColor(ToolStripItem item)
    {
        var partId = item.IsOnDropDown ? (int) menuParts.PopupItem : (int) menuParts.BarItem;
        _renderer.SetParameters(menuClass, partId, getItemState(item));
        return _renderer.GetColor(ColorProperty.TextColor);
    }

    /// <summary>Override - see base.</summary>
    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        if (ensureRenderer())
            e.TextColor = getItemTextColor(e.Item);

        base.OnRenderItemText(e);
    }

    /// <summary>Override - see base.</summary>
    protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
    {
        if (ensureRenderer())
        {
            if (e.ToolStrip.IsDropDown)
            {
                _renderer.SetParameters(menuClass, (int) menuParts.PopupGutter, 0);
                // The AffectedBounds is usually too small, way too small to look right. Instead of using that,
                // use the AffectedBounds but with the right width. Then narrow the rectangle to the correct edge
                // based on whether or not it's RTL. (It doesn't need to be narrowed to an edge in LTR mode, but let's
                // do that anyway.)
                // Using the DisplayRectangle gets roughly the right size so that the separator is closer to the text.
                var margins = getThemeMargins(e.Graphics, marginTypes.Sizing);
                var extraWidth = (e.ToolStrip.Width - e.ToolStrip.DisplayRectangle.Width - margins.Left - margins.Right - 1) - e.AffectedBounds.Width;
                var rect = e.AffectedBounds;
                rect.Y += 2;
                rect.Height -= 4;
                var sepWidth = _renderer.GetPartSize(e.Graphics, ThemeSizeType.True).Width;
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
        if (e.ToolStrip.IsDropDown && ensureRenderer())
        {
            _renderer.SetParameters(menuClass, (int) menuParts.PopupSeparator, 0);
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
        if (ensureRenderer())
        {
            var bgRect = getBackgroundRectangle(e.Item);
            bgRect.Width = bgRect.Height;

            // Now, mirror its position if the menu item is RTL.
            if (e.Item.RightToLeft == RightToLeft.Yes)
                bgRect = new Rectangle(e.ToolStrip.ClientSize.Width - bgRect.X - bgRect.Width, bgRect.Y, bgRect.Width, bgRect.Height);

            _renderer.SetParameters(menuClass, (int) menuParts.PopupCheckBackground, e.Item.Enabled ? (int) menuPopupCheckBackgroundStates.Normal : (int) menuPopupCheckBackgroundStates.Disabled);
            _renderer.DrawBackground(e.Graphics, bgRect);

            var checkRect = e.ImageRectangle;
            checkRect.X = bgRect.X + bgRect.Width / 2 - checkRect.Width / 2;
            checkRect.Y = bgRect.Y + bgRect.Height / 2 - checkRect.Height / 2;

            // I don't think ToolStrip even supports radio box items, so no need to render them.
            _renderer.SetParameters(menuClass, (int) menuParts.PopupCheck, e.Item.Enabled ? (int) menuPopupCheckStates.CheckmarkNormal : (int) menuPopupCheckStates.CheckmarkDisabled);

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
        if (ensureRenderer())
            e.ArrowColor = getItemTextColor(e.Item);
        base.OnRenderArrow(e);
    }

    /// <summary>Override - see base.</summary>
    protected override void OnRenderOverflowButtonBackground(ToolStripItemRenderEventArgs e)
    {
        if (ensureRenderer())
        {
            // BrowserTabBar::Rebar draws the chevron using the default background. Odd.
            var rebarClass = this.rebarClass;
            if (Theme == NativeToolbarTheme.BrowserTabBar)
                rebarClass = "Rebar";

            var state = VisualStyleElement.Rebar.Chevron.Normal.State;
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
    public static bool IsSupported
    {
        get
        {
            if (!VisualStyleRenderer.IsSupported)
                return false;

            // Needs a more robust check. It seems mono supports very different style sets.
            return
                    VisualStyleRenderer.IsElementDefined(
                            VisualStyleElement.CreateElement("Menu",
                                    (int) menuParts.BarBackground,
                                    (int) menuBarStates.Active));
        }
    }
#pragma warning restore IDE1006 // Naming Styles
}
