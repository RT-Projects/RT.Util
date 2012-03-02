using System;
using System.Collections.Generic;
using System.Windows;

namespace RT.Util.Forms
{
    /// <summary>
    /// A window which has all the proper minimize/restore methods, and which remembers its position and size between instances of the application.
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

        /// <summary>We need a default constructor for the WPF designer to work. Don't invoke this or else the settings won't work.</summary>
        public ManagedWindow() { }

        /// <summary>Initialises a new managed window.</summary>
        /// <param name="settings">An object of type <see cref="ManagedWindow.Settings"/> from which the position and size of the form are retrieved, and in which they will be stored.</param>
        public ManagedWindow(Settings settings)
        {
            if (settings == null) throw new ArgumentNullException("settings");

            _settings = settings;
            try
            {
                var resolution = (int) SystemParameters.VirtualScreenWidth + "x" + (int) SystemParameters.VirtualScreenHeight;

                WindowDimensions dimensions = null;
                if (_settings.DimensionsByRes.ContainsKey(resolution))
                    dimensions = _settings.DimensionsByRes[resolution];

                if (dimensions == null)
                {
                    Left = _normalLeft = SystemParameters.WorkArea.Left + SystemParameters.WorkArea.Width / 2 - Width / 2;
                    Top = _normalTop = SystemParameters.WorkArea.Top + SystemParameters.WorkArea.Height / 2 - Height / 2;
                    _normalWidth = Width;
                    _normalHeight = Height;
                }
                else
                {
                    Left = _normalLeft = dimensions.Left;
                    Top = _normalTop = dimensions.Top;
                    Width = _normalWidth = dimensions.Width;
                    Height = _normalHeight = dimensions.Height;
                    Maximized = dimensions.Maximized;
                }
            }
            catch
            { }

            // SizeChanged event: keeps track of minimize/maximize and normal size
            SizeChanged += new SizeChangedEventHandler(processResize);
            // Move event: keeps track of normal dimensions
            LocationChanged += new EventHandler(processMove);
            // Close event: save the settings
            Closed += new EventHandler(saveSettings);

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
        }

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
                        // The window was just maximized. Due to a stupid bug in WPF, a LocationChanged event will have occurred before this Resize event.
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

        private void saveSettings(object sender, EventArgs e)
        {
            var resolution = (int) SystemParameters.VirtualScreenWidth + "x" + (int) SystemParameters.VirtualScreenHeight;
            var dimensions = new WindowDimensions();
            dimensions.Left = _normalLeft;
            dimensions.Top = _normalTop;
            dimensions.Width = _normalWidth;
            dimensions.Height = _normalHeight;
            dimensions.Maximized = Maximized;
            _settings.DimensionsByRes[resolution] = dimensions;
        }

        #endregion
    }
}
