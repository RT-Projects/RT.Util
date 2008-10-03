using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace RT.Util.Forms
{
    /// <summary>Contains static methods to operate on managed forms (subclasses of <see cref="ManagedForm"/>).</summary>
    public static class FormManager
    {
        private static Dictionary<Type, ManagedForm> Instances = new Dictionary<Type, ManagedForm>();

        internal static void FormCreated(Type type, ManagedForm instance)
        {
            if (Instances.ContainsKey(type))
                throw new Exception(string.Format("Attempt to create an instance of the managed form {0} when another one is opened.", type.FullName));
            else
                Instances.Add(type, instance);
        }

        internal static void FormClosed(Type type)
        {
            if (!Instances.ContainsKey(type))
                throw new Exception(string.Format("Attempt to close managed form {0} when no instance is stored by FormManager.", type.FullName));
            else
                Instances.Remove(type);
        }

        /// <summary>Returns the managed form of the specified type. If none exists, it is created.</summary>
        /// <typeparam name="T">The managed form type to return.</typeparam>
        /// <returns>The current or new instance of the specified managed form.</returns>
        public static T GetForm<T>() where T : ManagedForm
        {
            T form;
            if (Instances.ContainsKey(typeof(T)))
                form = (T) Instances[typeof(T)];
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

        /// <summary>Shows the managed form of the specified type. If none exists, it is created.</summary>
        /// <typeparam name="T">The type of the managed form to show.</typeparam>
        public static void ShowForm<T>() where T : ManagedForm
        {
            GetForm<T>().Show();
        }

        /// <summary>Hides the managed form of the specified type. If none exists, it is created hidden.</summary>
        /// <typeparam name="T">The type of the managed form to hide.</typeparam>
        public static void HideForm<T>() where T : ManagedForm
        {
            GetForm<T>().Hide();
        }
    }
}
