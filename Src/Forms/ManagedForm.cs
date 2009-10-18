using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace RT.Util.Forms
{
    /// <summary>
    /// A form which has all the proper minimize/restore methods, and which remembers its position and size between instances of the application.
    /// </summary>
    public class ManagedForm : Form
    {
        private FormWindowState _prevWindowState;
        private bool _stateMaximized;
        private bool _stateMinimized;
        private int _normalWidth, _normalHeight;
        private int _normalLeft, _normalTop;
        private Settings _settings;

        // We need a default constructor for the form designer to work, but we don't want it to be used at runtime, so make it private
        private ManagedForm() { }

        /// <summary>Initialises a new managed form.</summary>
        /// <param name="settings">An object of type <see cref="ManagedForm.Settings"/> from which the position and size of the form are retrieved, and in which they will be stored.</param>
        public ManagedForm(Settings settings)
        {
            if (settings == null) throw new ArgumentNullException("settings");

            // Since the constructor is executed before InitializeComponent(), and InitializeComponent() potentially sets ClientSize, which reverts our changes,
            // we need to apply the settings later. Use the Load event for this
            Load += (sender, e) =>
            {
                // Just leaving the form font at the default uses the wrong font - the one
                // returned by SystemFonts.DefaultFont, which is not one configured through the Desktop Properties dialog.
                // Fix this.
                Font = System.Drawing.SystemFonts.MessageBoxFont;

                _settings = settings;
                try
                {
                    var vs = SystemInformation.VirtualScreen;
                    var resolution = vs.Width + "x" + vs.Height;

                    FormDimensions dimensions = null;
                    if (_settings.DimensionsByRes.ContainsKey(resolution))
                        dimensions = _settings.DimensionsByRes[resolution];

                    if (dimensions == null)
                    {
                        Left = _normalLeft = Screen.PrimaryScreen.WorkingArea.Left + Screen.PrimaryScreen.WorkingArea.Width / 2 - Width / 2;
                        Top = _normalTop = Screen.PrimaryScreen.WorkingArea.Top + Screen.PrimaryScreen.WorkingArea.Height / 2 - Height / 2;
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
                SizeChanged += new EventHandler(processResize);
                // Move event: keeps track of normal dimensions
                Move += new EventHandler(processMove);
                // Close event: save the settings
                FormClosed += new FormClosedEventHandler(saveSettings);

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
            };
        }

        private void processResize(object sender, EventArgs e)
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

        private void processMove(object sender, EventArgs e)
        {
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
        /// Shows the form properly: if it is visible but minimized it will be restored
        /// and activated; otherwise the base implementation of Show will be invoked.
        /// </summary>
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

        private void saveSettings(object sender, FormClosedEventArgs e)
        {
            var vs = SystemInformation.VirtualScreen;
            var resolution = vs.Width + "x" + vs.Height;
            var dimensions = new FormDimensions();
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
