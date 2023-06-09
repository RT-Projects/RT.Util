using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;
using RT.Util.ExtensionMethods;

namespace RT.Util.Forms
{
    /// <summary>
    ///     A form which has all the proper minimize/restore methods, and which remembers its position and size between
    ///     instances of the application.</summary>
    public class ManagedForm : Form
    {
        private FormWindowState _prevWindowState;
        private bool _stateMaximized;
        private bool _stateMinimized;
        private int _normalWidth, _normalHeight;
        private int _normalLeft, _normalTop;
        private Settings _settings;
        private string _lastScreenResolution;

        // We need a default constructor for the form designer to work, but we don't want it to be used at runtime, so make it private
        private ManagedForm() { }

        /// <summary>
        ///     Initialises a new managed form.</summary>
        /// <param name="settings">
        ///     An object of type <see cref="ManagedForm.Settings"/> from which the position and size of the form are
        ///     retrieved, and in which they will be stored.</param>
        public ManagedForm(Settings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));
            _settings = settings;

            var vs = SystemInformation.VirtualScreen;
            _lastScreenResolution = vs.Width + "x" + vs.Height;

            // Since the base constructor is executed before InitializeComponent(), and InitializeComponent() potentially sets ClientSize, which reverts our changes,
            // we need to apply the settings later. Use the Load event for this
            Load += formLoad;
        }

        private void formLoad(object sender, EventArgs e)
        {
            // Just leaving the form font at the default uses the wrong font - the one
            // returned by SystemFonts.DefaultFont, which is not one configured through the Desktop Properties dialog.
            // Fix this.
            Font = SystemFonts.MessageBoxFont;

            try
            {
                // This call also sets _lastScreenResolution
                setDimensionsForCurrentScreenResolution(true);
            }
            catch
            { }

            // SizeChanged event: keeps track of minimize/maximize and normal size
            SizeChanged += processResize;
            // Move event: keeps track of normal dimensions
            Move += processMove;
            // Close event: save the settings
            FormClosed += saveSettings;
            // Restore position and size properly when the screen resolution changes
            SystemEvents.DisplaySettingsChanged += displaySettingsChanged;

            _prevWindowState = WindowState;

            switch (WindowState)
            {
                case FormWindowState.Minimized:
                    _stateMinimized = true;
                    _stateMaximized = false; // (guessing?)
                    break;
                case FormWindowState.Maximized:
                    _stateMinimized = false;
                    _stateMaximized = true;
                    break;
                case FormWindowState.Normal:
                    _stateMinimized = false;
                    _stateMaximized = false;
                    break;
            }
        }

        private void setDimensionsForCurrentScreenResolution(bool firstShow)
        {
            var vs = SystemInformation.VirtualScreen;
            _lastScreenResolution = vs.Width + "x" + vs.Height;
            if (!_settings.DimensionsByRes.ContainsKey(_lastScreenResolution))
            {
                var primaryScreenWA = Screen.PrimaryScreen.WorkingArea;
                Left = _normalLeft = primaryScreenWA.Left + primaryScreenWA.Width / 2 - Width / 2;
                Top = _normalTop = primaryScreenWA.Top + primaryScreenWA.Height / 2 - Height / 2;
                _normalWidth = Width;
                _normalHeight = Height;
            }
            else
            {
                var dimensions = _settings.DimensionsByRes[_lastScreenResolution];
                bool visible = false;
                foreach (var screen in Screen.AllScreens)
                    visible |= dimensions.Left >= screen.WorkingArea.Left + 10 - dimensions.Width && dimensions.Left <= screen.WorkingArea.Right - 10 &&
                        dimensions.Top >= screen.WorkingArea.Top + 10 - dimensions.Height && dimensions.Top <= screen.WorkingArea.Bottom - 10;
                if (!visible)
                {
                    var primaryScreenWA = Screen.PrimaryScreen.WorkingArea;
                    dimensions.Left = primaryScreenWA.Left + primaryScreenWA.Width / 2 - dimensions.Width / 2;
                    dimensions.Top = primaryScreenWA.Top + primaryScreenWA.Height / 2 - dimensions.Height / 2;
                }
                Left = _normalLeft = dimensions.Left;
                Top = _normalTop = dimensions.Top;
                Width = _normalWidth = dimensions.Width;
                Height = _normalHeight = dimensions.Height;
                Maximized = dimensions.Maximized;
            }
            ResizeAndReposition(firstShow);
            processMove();
            processResize();
            updateDimensionsForLastScreenResolution();
        }

        /// <summary>
        ///     Override to alter the window size, maximize state and/or position on first show and screen resolution changes.
        ///     At the time of call, the current position/state/size are already set to the preferred ones, so it is possible
        ///     to change them with respect to the "saved" values. If this method does nothing then the last saved values will
        ///     be in effect.</summary>
        /// <param name="firstShow">
        ///     True if this is the first time the form is displayed; false if it's a screen resolution change.</param>
        protected virtual void ResizeAndReposition(bool firstShow) { }

        private void displaySettingsChanged(object sender, EventArgs e)
        {
            // Save the settings for the old resolution
            updateDimensionsForLastScreenResolution();

            // Set the dimensions for the new resolution. This call also updates _lastScreenResolution.
            // If no dimensions for the new resolution are stored, this does nothing and relies on the OS to position the window meaningfully.
            setDimensionsForCurrentScreenResolution(false);
        }

        private void processResize(object sender = null, EventArgs e = null)
        {
            // Update normal size
            if (WindowState == FormWindowState.Normal)
            {
                _normalWidth = Width;
                _normalHeight = Height;
                _normalLeft = Left;
                _normalTop = Top;
            }

            if (WindowState != _prevWindowState)
            {
                // Set new state
                switch (WindowState)
                {
                    case FormWindowState.Minimized:
                        _stateMinimized = true;
                        break;
                    case FormWindowState.Maximized:
                        _stateMaximized = true;
                        break;
                    case FormWindowState.Normal:
                        // Fix for maximize while minimized
                        if (_stateMaximized && _prevWindowState == FormWindowState.Minimized)
                            WindowState = FormWindowState.Maximized;
                        else
                            _stateMaximized = false;
                        break;
                }

                // Unset old state
                switch (_prevWindowState)
                {
                    case FormWindowState.Minimized:
                        _stateMinimized = false;
                        break;
                }

                _prevWindowState = WindowState;
            }
        }

        private void processMove(object sender = null, EventArgs e = null)
        {
            // A move event can happen when the user changes the screen resolution, but before the DisplaySettingsChanging and DisplaySettingsChanged events occur.
            // We need to ignore that spurious move event.
            var vs = SystemInformation.VirtualScreen;
            if (vs.Width + "x" + vs.Height != _lastScreenResolution)
                return;

            // Update normal size
            if (WindowState == FormWindowState.Normal)
            {
                _normalLeft = Left;
                _normalTop = Top;
            }
        }

        /// <summary>Determines if the current managed form is minimised.</summary>
        public bool Minimized
        {
            get
            {
                return _stateMinimized;
            }
            set
            {
                if (_stateMinimized == value)
                    return;

                if (value)
                    // Minimize
                    WindowState = FormWindowState.Minimized;
                else
                    // Un-minimize
                    WindowState = _stateMaximized ? FormWindowState.Maximized : FormWindowState.Normal;

                _stateMinimized = value;
            }
        }

        /// <summary>
        ///     Determines whether the current managed form is maximised, or is minimised and would be maximised if restored.</summary>
        public bool Maximized
        {
            get
            {
                return _stateMaximized;
            }
            set
            {
                if (_stateMaximized == value)
                    return;

                // Don't change the actual state if the window is minimized
                if (!_stateMinimized)
                {
                    if (value)
                        // Maximize
                        WindowState = FormWindowState.Maximized;
                    else
                        // Un-maximize
                        WindowState = FormWindowState.Normal;
                }

                _stateMaximized = value;
            }
        }

        /// <summary>Gets the width of the form when in normal state (i.e. not minimized or maximized).</summary>
        public int NormalWidth { get { return _normalWidth; } }

        /// <summary>Gets the height of the form when in normal state (i.e. not minimized or maximized).</summary>
        public int NormalHeight { get { return _normalHeight; } }

        /// <summary>Gets the X-coordinate of the form when in normal state (i.e. not minimized or maximized).</summary>
        public int NormalLeft { get { return _normalLeft; } }

        /// <summary>Gets the Y-coordinate of the form when in normal state (i.e. not minimized or maximized).</summary>
        public int NormalTop { get { return _normalTop; } }

        /// <summary>
        ///     Shows the form properly: if it is visible but minimized it will be restored and activated; otherwise the base
        ///     implementation of Show will be invoked.</summary>
        public virtual new void Show()
        {
            if (Visible)
            {
                Minimized = false;
                Activate();
            }
            else
                base.Show();
        }

        /// <summary>
        ///     Shows the form as a modal dialog box with the currently active window set as its owner.</summary>
        /// <param name="centerInForm">
        ///     If specified, this form will be centered relative to the specified form.</param>
        /// <param name="repositionParentAfterwards">
        ///     If set to true, will cause the parent to be moved after this form is closed to be centered with respect to it.</param>
        /// <returns>
        ///     One of the <see cref="System.Windows.Forms.DialogResult"/> values.</returns>
        public virtual DialogResult ShowDialog(Form centerInForm = null, bool repositionParentAfterwards = false)
        {
            if (centerInForm == null)
                return base.ShowDialog();

            if (!_settings.DimensionsByRes.ContainsKey(_lastScreenResolution))
                _settings.DimensionsByRes[_lastScreenResolution] = new FormDimensions { Left = Left, Top = Top, Width = Width, Height = Height };
            var dims = _settings.DimensionsByRes[_lastScreenResolution];
            if (!dims.Maximized)
            {
                // Make sure that this window doesn’t go off the edge of the working area, unless of course it absolutely doesn’t fit
                var scr = Screen.FromControl(centerInForm).WorkingArea;
                // Ensure that scr.Left/Top is used if the window is larger than the screen (using .Clip() would throw)
                dims.Left = (centerInForm.Left + (centerInForm.Width - dims.Width) / 2).ClipMax(scr.Right - dims.Width).ClipMin(scr.Left);
                dims.Top = (centerInForm.Top + (centerInForm.Height - dims.Height) / 2).ClipMax(scr.Bottom - dims.Height).ClipMin(scr.Top);
            }
            var prevRes = _lastScreenResolution;
            var prevLeft = dims.Left;
            var prevTop = dims.Top;

            var result = base.ShowDialog();

            // Only reposition the parent if this form was moved by the user
            if (repositionParentAfterwards && (prevLeft != _normalLeft || prevTop != _normalTop || prevRes != _lastScreenResolution))
            {
                dims = _settings.DimensionsByRes[_lastScreenResolution];
                if (!dims.Maximized)
                {
                    centerInForm.Left = dims.Left + (dims.Width - centerInForm.Width) / 2;
                    centerInForm.Top = dims.Top + (dims.Height - centerInForm.Height) / 2;
                }
            }

            return result;
        }

        #region Settings-related

        /// <summary>Holds the settings of the <see cref="ManagedForm"/>.</summary>
        public class Settings
        {
            /// <summary>Holds form dimensions for each screen resolution.</summary>
            public Dictionary<string, FormDimensions> DimensionsByRes = new Dictionary<string, FormDimensions>();

            /// <summary>Returns a deep clone of this class.</summary>
            public virtual object Clone()
            {
                var result = (Settings) MemberwiseClone();
                result.DimensionsByRes = new Dictionary<string, FormDimensions>(DimensionsByRes.Count);
                foreach (var kvp in DimensionsByRes)
                    result.DimensionsByRes[kvp.Key] = kvp.Value.Clone();
                return result;
            }
        }

        /// <summary>Stores the size, position and maximized state of the form.</summary>
        public sealed class FormDimensions
        {
            /// <summary>Stores the left (X) coordinate of the form when not maximized.</summary>
            public int Left;
            /// <summary>Stores the top (Y) coordinate of the form when not maximized.</summary>
            public int Top;
            /// <summary>Stores the width of the form when not maximized.</summary>
            public int Width;
            /// <summary>Stores the height of the form when not maximized.</summary>
            public int Height;
            /// <summary>Stores whether the form is maximized.</summary>
            public bool Maximized;

            /// <summary>Returns a deep clone of this class.</summary>
            public FormDimensions Clone()
            {
                return (FormDimensions) MemberwiseClone();
            }
        }

        private void updateDimensionsForLastScreenResolution()
        {
            var dimensions = new FormDimensions();
            dimensions.Left = _normalLeft;
            dimensions.Top = _normalTop;
            dimensions.Width = _normalWidth;
            dimensions.Height = _normalHeight;
            dimensions.Maximized = Maximized;
            _settings.DimensionsByRes[_lastScreenResolution] = dimensions;
        }

        private void saveSettings(object sender, FormClosedEventArgs e)
        {
            updateDimensionsForLastScreenResolution();
        }

        #endregion
    }
}
