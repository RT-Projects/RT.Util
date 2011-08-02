using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RT.Util.ExtensionMethods
{
    /// <summary>
    /// Provides extension methods for Windows Forms controls.
    /// </summary>
    public static class MiscExtensions
    {
        /// <summary>
        /// If this control is located within a <see cref="TabPage"/>, returns that TabPage
        /// by iterating recursively through this item's parents. Otherwise returns null.
        /// </summary>
        public static TabPage ParentTab(this Control control)
        {
            while (control != null)
            {
                if (control.Parent is TabPage)
                    return control.Parent as TabPage;
                control = control.Parent;
            }
            return null;
        }

        /// <summary>Same as Control.Invoke, except that the action is not invoked immediately
        /// if we are on the GUI thread. Instead, it is scheduled to be run the next time the GUI thread is idle.</summary>
        /// <param name="invokable">Control to invoke action on.</param>
        /// <param name="action">Action to invoke.</param>
        public static void InvokeLater(this Control invokable, Action action)
        {
            Task.Factory.StartNew(() => { invokable.Invoke(action); });
        }

        /// <summary>Blocks this thread until this token is cancelled.</summary>
        /// <param name="token">Cancellation token.</param>
        public static void SleepCancellable(this CancellationToken token)
        {
            token.WaitHandle.WaitOne();
        }

        /// <summary>Like Thread.Sleep, but the sleep will be aborted if Cancel is invoked on this cancellation token.</summary>
        /// <param name="token">Cancellation token.</param>
        /// <param name="milliseconds">The number of milliseconds to sleep for, or Timeout.Infinite to wait until cancelled.</param>
        public static void SleepCancellable(this CancellationToken token, int milliseconds)
        {
            token.WaitHandle.WaitOne(milliseconds);
        }

        /// <summary>Like Thread.Sleep, but the sleep will be aborted if Cancel is invoked on this cancellation token.</summary>
        /// <param name="token">Cancellation token.</param>
        /// <param name="time">The amount of time to sleep for.</param>
        public static void SleepCancellable(this CancellationToken token, TimeSpan time)
        {
            token.WaitHandle.WaitOne(time);
        }

        /// <summary>Enumerates a chain of objects where each object refers to the next one. The chain starts with the specified object and ends when null is encountered.</summary>
        /// <typeparam name="T">Type of object to enumerate.</typeparam>
        /// <param name="obj">Initial object.</param>
        /// <param name="next">A function that returns the next object given the current one. If null is returned, enumeration will end.</param>
        public static IEnumerable<T> SelectChain<T>(this T obj, Func<T, T> next) where T : class
        {
            while (obj != null)
            {
                yield return obj;
                obj = next(obj);
            }
        }

        /// <summary>Returns a new comparer, which is compares items in the opposite order to this one.</summary>
        public static IComparer<T> Inverted<T>(this IComparer<T> comparer)
        {
            return new CustomComparer<T>((a, b) => -comparer.Compare(a, b));
        }

        /// <summary>Applies the specified action to this object, and returns the object.</summary>
        public static T Apply<T>(this T subject, Action<T> action)
        {
            action(subject);
            return subject;
        }
    }
}
