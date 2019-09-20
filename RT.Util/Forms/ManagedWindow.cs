using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;

// Requirements:
// - on first run, centered on primary or current monitor using the designer width/height and maximized state
// - on subsequent runs, restored to the same state / position / size as it had on last shutdown, without flicker
// - if restored maximized, must be on the same screen, and after un-maximizing must have the same position / size as it would have had on last shutdown
// - must keep the passed in settings object reasonably up-to-date at all times, without any explicit calls to do so
// - when window defaults to maximized, a start and immediate close must not remember a silly size (e.g. zeroes)

// To do:
// - investigate replacing MaximizedByDefault with WindowState="Maximized" in XAML support
// - disallow window being completely off-screen for whatever reason (e.g. resolution change)
// - on resolution change, move the window to its last state at that resolution

namespace RT.Util.Forms
{
    /// <summary>
    /// A window which has all the proper minimize/restore methods, and which remembers its position and size between instances of the application.
    /// The size/position/state values are automatically reflected in the settings object whenever the user resizes or moves the window. If XAML
    /// supplies an initial size, that size will be used whenever no previous size is available (such as first run).
    /// </summary>
    public class ManagedWindow : Window
    {
        private WindowState _prevWindowState;
        private bool _stateMaximized;
        private bool _stateMinimized;
        private double _normalWidth, _normalHeight;
        private double _normalLeft, _normalTop;
        private double _prevLeft, _prevTop;  // WPF bug workaround: see processResize for explanation
        private Settings _settings;
        private DispatcherTimer _storeSettingsTimer;

        /// <summary>We need a default constructor for the WPF designer to work. Don't invoke this or else the settings won't work.</summary>
        public ManagedWindow() { }

        /// <summary>Initialises a new managed window.</summary>
        /// <param name="settings">An object of type <see cref="ManagedWindow.Settings"/> from which the position and size of the form are retrieved, and in which they will be stored.</param>
        public ManagedWindow(Settings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            _settings = settings;

            try
            {
                var resolution = (int) SystemParameters.VirtualScreenWidth + "x" + (int) SystemParameters.VirtualScreenHeight;
                WindowDimensions dimensions;
                if (_settings.DimensionsByRes.TryGetValue(resolution, out dimensions))
                {
                    // Already have settings for this window/resolution. Make sure they don't put the window off every screen.
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
                    SourceInitialized += delegate
                    {
                        // Can't do this in the constructor because then maximizing to secondary montor is broken, and also because XAML would then have priority.
                        // Also can't do this in the conventional "Loaded" event, because it's fired too late, when the window is already visible, causing flicker.
                        Left = _normalLeft = dimensions.Left;
                        Top = _normalTop = dimensions.Top;
                        Width = _normalWidth = dimensions.Width;
                        Height = _normalHeight = dimensions.Height;
                        Maximized = dimensions.Maximized;
                        finishInitialization();
                    };
                }
                else
                {
                    // Use default settings: center on active monitor, using the size and state defined in the designer / XAML
                    WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen; // must be set in the constructor
                    Maximized = MaximizedByDefault;
                    Loaded += delegate
                    {
                        // Left/Top/Width/Height not available until after the window has become visible
                        _normalLeft = Left;
                        _normalTop = Top;
                        _normalWidth = Width;
                        _normalHeight = Height;
                        finishInitialization();
                    };
                }
            }
            catch { }
        }

        void finishInitialization()
        {
            _prevWindowState = WindowState;
            switch (WindowState)
            {
                case WindowState.Minimized:
                    _stateMinimized = true;
                    _stateMaximized = false; // (guessing?)
                    break;
                case WindowState.Maximized:
                    _stateMinimized = false;
                    _stateMaximized = true;
                    break;
                case WindowState.Normal:
                    _stateMinimized = false;
                    _stateMaximized = false;
                    break;
            }

            // SizeChanged event: keeps track of minimize/maximize and normal size
            SizeChanged += processResize;
            // Move event: keeps track of normal dimensions
            LocationChanged += processMove;
            // Close event: save the settings
            Closed += storeSettings;
            // Settings are stored in the dispatcher a short while after the user has stopped resizing/moving the window,
            // to work around the maximize-related bug described in processResize.
            _storeSettingsTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150), IsEnabled = false };
            _storeSettingsTimer.Tick += storeSettingsDelayedTick;
        }

        /// <summary>To make the window maximized by default (i.e. on first run, before the user has had a chance to move/resize the window),
        /// override this property and return true. This property is read from the window constructor. Do not use XAML to set Maximized to true
        /// because <see cref="ManagedWindow"/> will not be able to override that without flicker (and thus doesn't support that at all).</summary>
        public virtual bool MaximizedByDefault { get { return false; } }

        private void processResize(object sender, SizeChangedEventArgs e)
        {
            // Update normal size
            if (WindowState == WindowState.Normal)
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
                    case WindowState.Minimized:
                        _stateMinimized = true;
                        break;
                    case WindowState.Maximized:
                        // ARGH: The window was just maximized. Due to a stupid bug in WPF, a LocationChanged event will have occurred before this Resize event.
                        // In that LocationChanged event, WindowState is incorrectly still set to "normal", but "Left" and "Top" are already set to -4 each.
                        // Consequently, we have to remember the *actual* left/top in _prevLeft/_prevTop and restore them here.
                        _normalLeft = _prevLeft;
                        _normalTop = _prevTop;
                        _stateMaximized = true;
                        break;
                    case WindowState.Normal:
                        // Fix for maximize while minimized
                        if (_stateMaximized && _prevWindowState == WindowState.Minimized)
                            WindowState = WindowState.Maximized;
                        else
                            _stateMaximized = false;
                        break;
                }

                // Unset old state
                switch (_prevWindowState)
                {
                    case WindowState.Minimized:
                        _stateMinimized = false;
                        break;
                }

                _prevWindowState = WindowState;
            }
            storeSettingsDelayed();
        }

        private void processMove(object sender, EventArgs e)
        {
            // Update normal size
            if (WindowState == WindowState.Normal)
            {
                // WPF bug workaround: see processResize for explanation
                _prevLeft = _normalLeft;
                _prevTop = _normalTop;
                _normalLeft = Left;
                _normalTop = Top;
            }
            storeSettingsDelayed();
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
                    WindowState = WindowState.Minimized;
                else
                    // Un-minimize
                    WindowState = _stateMaximized ? WindowState.Maximized : WindowState.Normal;

                _stateMinimized = value;
            }
        }

        /// <summary>Determines whether the current managed form is maximised, or is minimised and would be maximised if restored.</summary>
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
                        WindowState = WindowState.Maximized;
                    else
                        // Un-maximize
                        WindowState = WindowState.Normal;
                }

                _stateMaximized = value;
            }
        }

        /// <summary>Gets the width of the form when in normal state (i.e. not minimized or maximized).</summary>
        public double NormalWidth { get { return _normalWidth; } }

        /// <summary>Gets the height of the form when in normal state (i.e. not minimized or maximized).</summary>
        public double NormalHeight { get { return _normalHeight; } }

        /// <summary>Gets the X-coordinate of the form when in normal state (i.e. not minimized or maximized).</summary>
        public double NormalLeft { get { return _normalLeft; } }

        /// <summary>Gets the Y-coordinate of the form when in normal state (i.e. not minimized or maximized).</summary>
        public double NormalTop { get { return _normalTop; } }

        /// <summary>
        /// Shows the form properly: if it is visible but minimized it will be restored
        /// and activated; otherwise the base implementation of Show will be invoked.
        /// </summary>
        public virtual new void Show()
        {
            if (this.Visibility == Visibility.Visible)
            {
                Minimized = false;
                Activate();
            }
            else
                base.Show();
        }

        #region Settings-related

        /// <summary>Holds the settings of the <see cref="ManagedWindow"/>.</summary>
        public class Settings
        {
            /// <summary>Holds form dimensions for each screen resolution.</summary>
            public Dictionary<string, WindowDimensions> DimensionsByRes = new Dictionary<string, WindowDimensions>();
        }

        /// <summary>Stores the size, position and maximized state of the form.</summary>
        public class WindowDimensions
        {
            /// <summary>Stores the left (X) coordinate of the form when not maximized.</summary>
            public double Left;
            /// <summary>Stores the top (Y) coordinate of the form when not maximized.</summary>
            public double Top;
            /// <summary>Stores the width of the form when not maximized.</summary>
            public double Width;
            /// <summary>Stores the height of the form when not maximized.</summary>
            public double Height;
            /// <summary>Stores whether the form is maximized.</summary>
            public bool Maximized;
        }

        private void storeSettings(object _ = null, EventArgs __ = null)
        {
            var resolution = (int) SystemParameters.VirtualScreenWidth + "x" + (int) SystemParameters.VirtualScreenHeight;
            WindowDimensions dimensions;
            if (!_settings.DimensionsByRes.TryGetValue(resolution, out dimensions))
                dimensions = _settings.DimensionsByRes[resolution] = new WindowDimensions();
            dimensions.Left = _normalLeft;
            dimensions.Top = _normalTop;
            dimensions.Width = _normalWidth;
            dimensions.Height = _normalHeight;
            dimensions.Maximized = Maximized;
        }

        private void storeSettingsDelayed()
        {
            _storeSettingsTimer.Stop();
            _storeSettingsTimer.Start();
        }

        private void storeSettingsDelayedTick(object _, EventArgs __)
        {
            _storeSettingsTimer.Stop();
            storeSettings();
        }

        #endregion
    }
}
