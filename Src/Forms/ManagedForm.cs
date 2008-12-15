using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using RT.Util.Settings;

namespace RT.Util.Forms
{
    /// <summary>
    /// A form which has all the proper minimize/restore methods
    /// </summary>
    public class ManagedForm : Form
    {
        private FormWindowState PrevWindowState;
        private bool StateMaximized;
        private bool StateMinimized;
        private int FNormalWidth, FNormalHeight;
        private int FNormalLeft, FNormalTop;

        /// <summary>Initialises a new managed form.</summary>
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

            PrevWindowState = WindowState;

            switch (WindowState)
            {
                case FormWindowState.Minimized:
                    StateMinimized = true;
                    StateMaximized = false; // (guessing?)
                    break;
                case FormWindowState.Maximized:
                    StateMinimized = false;
                    StateMaximized = true;
                    break;
                case FormWindowState.Normal:
                    StateMinimized = false;
                    StateMaximized = false;
                    break;
            }
        }

        private void ManagedForm_Load(object sender, EventArgs e)
        {
            if (!DesignMode)
            {
                // Load settings
                SettingsStore st = PrgSettings.Store;
                string path = "Managed-Form-Settings." + this.GetType().ToString() + ".";
                Width = st.Get(path + "Width", Width);
                Height = st.Get(path + "Height", Height);
                Left = st.Get(path + "Left", Screen.PrimaryScreen.WorkingArea.Width/2 - Width/2);
                Top = st.Get(path + "Top", Screen.PrimaryScreen.WorkingArea.Height/2 - Height/2);
                if (st.Get(path + "Maximized", false))
                    Maximized = true;
                // Register with the FormManager
                FormManager.formCreated(this.GetType(), this);
            }
        }

        private void ManagedForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (!DesignMode)
            {
                // Save settings
                SettingsStore st = PrgSettings.Store;
                string path = "Managed-Form-Settings." + this.GetType().ToString() + ".";
                st.Set(path + "Width", NormalWidth);
                st.Set(path + "Height", NormalHeight);
                st.Set(path + "Left", NormalLeft);
                st.Set(path + "Top", NormalTop);
                st.Set(path + "Maximized", Maximized);
                // Notify the form manager that this form is gone
                FormManager.formClosed(this.GetType());
            }
        }

        private void ManagedForm_SizeChanged(object sender, EventArgs e)
        {
            // Update normal size
            if (WindowState == FormWindowState.Normal)
            {
                FNormalWidth = Width;
                FNormalHeight = Height;
                FNormalLeft = Left;
                FNormalTop = Top;
            }

            if (WindowState != PrevWindowState)
            {
                // Set new state
                switch (WindowState)
                {
                    case FormWindowState.Minimized:
                        StateMinimized = true;
                        break;
                    case FormWindowState.Maximized:
                        StateMaximized = true;
                        break;
                    case FormWindowState.Normal:
                        // Fix for maximize while minimized
                        if (StateMaximized && PrevWindowState == FormWindowState.Minimized)
                            WindowState = FormWindowState.Maximized;
                        else
                            StateMaximized = false;
                        break;
                }

                // Unset old state
                switch (PrevWindowState)
                {
                    case FormWindowState.Minimized:
                        StateMinimized = false;
                        break;
                }

                PrevWindowState = WindowState;
            }
        }

        void ManagedForm_Move(object sender, EventArgs e)
        {
            // Update normal size
            if (WindowState == FormWindowState.Normal)
            {
                FNormalLeft = Left;
                FNormalTop = Top;
            }
        }

        /// <summary>
        /// Determines if the current managed form is minimised.
        /// </summary>
        public bool Minimized
        {
            get
            {
                return StateMinimized;
            }
            set
            {
                if (StateMinimized == value)
                    return;

                if (value)
                    // Minimize
                    WindowState = FormWindowState.Minimized;
                else
                    // Un-minimize
                    WindowState = StateMaximized ? FormWindowState.Maximized : FormWindowState.Normal;

                StateMinimized = value;
            }
        }

        /// <summary>
        /// Determines whether the current managed form is maximised, or is minimised and would be maximised if restored.
        /// </summary>
        public bool Maximized
        {
            get
            {
                return StateMaximized;
            }
            set
            {
                if (StateMaximized == value)
                    return;

                // Don't change the actual state if the window is minimized
                if (!StateMinimized)
                {
                    if (value)
                        // Maximize
                        WindowState = FormWindowState.Maximized;
                    else
                        // Un-maximize
                        WindowState = FormWindowState.Normal;
                }

                StateMaximized = value;
            }
        }

        /// <summary>
        /// Gets the width of the form when in normal state (i.e. not minimized or maximized)
        /// </summary>
        public int NormalWidth { get { return FNormalWidth; } }

        /// <summary>
        /// Gets the height of the form when in normal state (i.e. not minimized or maximized)
        /// </summary>
        public int NormalHeight { get { return FNormalHeight; } }

        /// <summary>
        /// Gets the X-coordinate of the form when in normal state (i.e. not minimized or maximized)
        /// </summary>
        public int NormalLeft { get { return FNormalLeft; } }

        /// <summary>
        /// Gets the Y-coordinate of the form when in normal state (i.e. not minimized or maximized)
        /// </summary>
        public int NormalTop { get { return FNormalTop; } }

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
    }
}
