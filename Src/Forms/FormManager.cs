using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace RT.Util
{
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
}
