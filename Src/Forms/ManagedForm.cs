using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using RT.Util.Settings;

namespace RT.Util
{
    [Serializable]
    public class ManagedFormSettings
    {
        public FormWindowState PrevWindowState = FormWindowState.Normal;
        public bool StateMaximized = false;
        public bool StateMinimized = false;
        public int NormalWidth = 800, NormalHeight = 600;
        public int NormalLeft = 0, NormalTop = 0;
        public ManagedFormSettings() { }
        public ManagedFormSettings(ManagedForm SettingsFrom)
        {
            NormalLeft = SettingsFrom.Left;
            NormalTop = SettingsFrom.Top;
            NormalWidth = SettingsFrom.Width;
            NormalHeight = SettingsFrom.Height;
            StateMaximized = SettingsFrom.Maximized;
            StateMinimized = SettingsFrom.Minimized;
        }
    }

    /// <summary>
    /// A form which has all the proper minimize/restore methods
    /// </summary>
    public class ManagedForm : Form
    {
        private ManagedFormSettings FManagedFormSettings = new ManagedFormSettings();

        public ManagedForm()
        {
            // Load event: registers with the FormManager
            Load += new EventHandler(ManagedForm_Load);
            // FormClose event: unregisters with the FormManager
            FormClosed += new FormClosedEventHandler(ManagedForm_FormClosed);
            // SizeChanged event: keeps track of minimize/maximize and normal size
            SizeChanged += new EventHandler(ManagedForm_SizeChanged);
            // Move event: keeps track of normal position
            Move += new EventHandler(ManagedForm_Move);

            FManagedFormSettings.PrevWindowState = WindowState;

            switch (WindowState)
            {
                case FormWindowState.Minimized:
                    FManagedFormSettings.StateMinimized = true;
                    FManagedFormSettings.StateMaximized = false; // (guessing?)
                    break;
                case FormWindowState.Maximized:
                    FManagedFormSettings.StateMinimized = false;
                    FManagedFormSettings.StateMaximized = true;
                    break;
                case FormWindowState.Normal:
                    FManagedFormSettings.StateMinimized = false;
                    FManagedFormSettings.StateMaximized = false;
                    break;
            }
        }

        private void ManagedForm_Load(object sender, EventArgs e)
        {
            // Register with the FormManager
            if (!DesignMode)
                FormManager.FormCreated(this.GetType(), this);
        }

        private void ManagedForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            // Notify the form manager that this form is gone
            if (!DesignMode)
                FormManager.FormClosed(this.GetType());
        }

        private void ManagedForm_SizeChanged(object sender, EventArgs e)
        {
            // Update normal size
            if (WindowState == FormWindowState.Normal)
            {
                FManagedFormSettings.NormalWidth = Width;
                FManagedFormSettings.NormalHeight = Height;
                FManagedFormSettings.NormalLeft = Left;
                FManagedFormSettings.NormalTop = Top;
            }

            if (WindowState != FManagedFormSettings.PrevWindowState)
            {
                // Set new state
                switch (WindowState)
                {
                    case FormWindowState.Minimized:
                        FManagedFormSettings.StateMinimized = true;
                        break;
                    case FormWindowState.Maximized:
                        FManagedFormSettings.StateMaximized = true;
                        break;
                    case FormWindowState.Normal:
                        // Fix for maximize while minimized
                        if (FManagedFormSettings.StateMaximized && FManagedFormSettings.PrevWindowState == FormWindowState.Minimized)
                            WindowState = FormWindowState.Maximized;
                        else
                            FManagedFormSettings.StateMaximized = false;
                        break;
                }

                // Unset old state
                switch (FManagedFormSettings.PrevWindowState)
                {
                    case FormWindowState.Minimized:
                        FManagedFormSettings.StateMinimized = false;
                        break;
                }

                FManagedFormSettings.PrevWindowState = WindowState;
            }
        }

        void ManagedForm_Move(object sender, EventArgs e)
        {
            // Update normal size
            if (WindowState == FormWindowState.Normal)
            {
                FManagedFormSettings.NormalLeft = Left;
                FManagedFormSettings.NormalTop = Top;
            }
        }

        public bool Minimized
        {
            get
            {
                return FManagedFormSettings.StateMinimized;
            }
            set
            {
                if (FManagedFormSettings.StateMinimized == value)
                    return;

                if (value)
                    // Minimize
                    WindowState = FormWindowState.Minimized;
                else
                    // Un-minimize
                    WindowState = FManagedFormSettings.StateMaximized ? FormWindowState.Maximized : FormWindowState.Normal;

                FManagedFormSettings.StateMinimized = value;
            }
        }

        public bool Maximized
        {
            get
            {
                return FManagedFormSettings.StateMaximized;
            }
            set
            {
                if (FManagedFormSettings.StateMaximized == value)
                    return;

                // Don't change the actual state if the window is minimized
                if (!FManagedFormSettings.StateMinimized)
                {
                    if (value)
                        // Maximize
                        WindowState = FormWindowState.Maximized;
                    else
                        // Un-maximize
                        WindowState = FormWindowState.Normal;
                }

                FManagedFormSettings.StateMaximized = value;
            }
        }

        /// <summary>
        /// Gets the width of the form when in normal state (i.e. not minimized or maximized)
        /// </summary>
        public int NormalWidth { get { return FManagedFormSettings.NormalWidth; } }

        /// <summary>
        /// Gets the height of the form when in normal state (i.e. not minimized or maximized)
        /// </summary>
        public int NormalHeight { get { return FManagedFormSettings.NormalHeight; } }

        /// <summary>
        /// Gets the X-coordinate of the form when in normal state (i.e. not minimized or maximized)
        /// </summary>
        public int NormalLeft { get { return FManagedFormSettings.NormalLeft; } }

        /// <summary>
        /// Gets the Y-coordinate of the form when in normal state (i.e. not minimized or maximized)
        /// </summary>
        public int NormalTop { get { return FManagedFormSettings.NormalTop; } }

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

        /// <summary>
        /// Loads Width, Height, Left, Top, and Maximized from EasySettings.
        /// </summary>
        public void LoadSettings(string UniqueName)
        {
            object Output;
            bool Success = EasySettings.Settings.TryGetValue("Managed form " + UniqueName, out Output);
            if (Success && Output is ManagedFormSettings)
            {
                // Restore the window position and state from the settings
                Width = (Output as ManagedFormSettings).NormalWidth;
                Height = (Output as ManagedFormSettings).NormalHeight;
                Left = (Output as ManagedFormSettings).NormalLeft;
                Top = (Output as ManagedFormSettings).NormalTop;
                Maximized = (Output as ManagedFormSettings).StateMaximized;

                // Overwrite FSettings now - not earlier, because the above will have triggered events
                FManagedFormSettings = Output as ManagedFormSettings;
            }
            else FManagedFormSettings = new ManagedFormSettings(this);
        }

        /// <summary>
        /// Saves Width, Height, Left, Top, and Maximized to EasySettings.
        /// </summary>
        public void SaveSettings(string UniqueName)
        {
            EasySettings.Set("Managed form " + UniqueName, FManagedFormSettings);
        }
    }
}
