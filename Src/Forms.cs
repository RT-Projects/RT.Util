using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace RT.Util
{
    /// <summary>
    /// A form which has all the proper minimize/restore methods
    /// </summary>
    public class ManagedForm : Form
    {
        public ManagedForm()
        {
            // Load event: registers with the FormManager
            Load += new EventHandler(ManagedForm_Load);
            // FormClose event: unregisters with the FormManager
            FormClosed += new FormClosedEventHandler(RaForm_FormClosed);
            // SizeChanged event: keeps track of minimize/maximize
            SizeChanged += new EventHandler(RaForm_SizeChanged);

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

        void ManagedForm_Load(object sender, EventArgs e)
        {
            // Register with the FormManager
            if (!DesignMode)
                FormManager.FormCreated(this.GetType(), this);
        }

        void RaForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            // Notify the form manager that this form is gone
            if (!DesignMode)
                FormManager.FormClosed(this.GetType());
        }

        void RaForm_SizeChanged(object sender, EventArgs e)
        {
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

        private FormWindowState PrevWindowState;
        private bool StateMaximized;
        private bool StateMinimized;

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


    public static class FormManager
    {
        private static Dictionary<Type, ManagedForm> Instances = new Dictionary<Type, ManagedForm>();

        internal static void FormCreated(Type type, ManagedForm instance)
        {
            if (Instances.ContainsKey(type))
                throw new Exception("Attempted to create an instance of the form '" + type.ToString() + "' when another one is opened.");
            else
                Instances.Add(type, instance);
        }

        internal static void FormClosed(Type type)
        {
            if (!Instances.ContainsKey(type))
                throw new Exception("Attempted to close form '" + type.ToString() + "' when no instances are stored by FormManager.");
            else
                Instances.Remove(type);
        }

        public static T GetForm<T>() where T : ManagedForm
        {
            T form;
            if (Instances.ContainsKey(typeof(T)))
                form = (T)Instances[typeof(T)];
            else
            {
                // Create from a private constructor. Since this form is derived from
                // ManagedForm, creating an instance will automatically notify the FormManager
                // which adds the form to Instances.
                try
                {
                    form = typeof(T).InvokeMember(typeof(T).Name,
                        System.Reflection.BindingFlags.CreateInstance,
                        null, null, null) as T;
                }
                catch (Exception E)
                {
                    // It looks like the derived form constructor has thrown an exception.
                    // Whatever the reason, ensure that there is no instance of this form
                    // stored by the FormManager. Propagate the exception in any case.
                    if (Instances.ContainsKey(typeof(T)))
                        Instances.Remove(typeof(T));
                    throw E;
                }
            }
            return form;
        }

        public static void ShowForm<T>() where T : ManagedForm
        {
            GetForm<T>().Show();
        }

        public static void HideForm<T>() where T : ManagedForm
        {
            GetForm<T>().Hide();
        }
    }


    //public class SingleForm<T> : RaForm where T : Form,new()
    //{
    //    private SingleForm()
    //    {
    //        SingleInstance.CreatedInstance(this);
    //    }

    //    protected override void Dispose(bool disposing)
    //    {
    //        SingleInstance.DisposedInstance(this);
    //        base.Dispose(disposing);
    //    }

    //    public T Instance
    //    {
            
    //        get { return SingleInstance.GetInstance<T>(false); }
    //    }

    //    public virtual void ShowForm()
    //    {
    //        T form = SingleInstance.GetInstance<T>(true);
    //        form.Show();
    //    }
    //}
}
