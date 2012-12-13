using System;
using System.Threading;

namespace RT.Util
{
    static partial class Ut
    {
        /// <summary>
        ///     Execute the core of the application. In debug mode, exceptions are passed through untouched. In release mode, all
        ///     exceptions are caught, including those on other threads, and passed on to the specified handlers. Also, the main thread
        ///     is given the name "Main".</summary>
        /// <param name="main">
        ///     A method which executes the core of the application.</param>
        /// <param name="onUnhandledMain">
        ///     Method called in case the main method throws an unhandled exception.</param>
        /// <param name="onUnhandledThread">
        ///     Method called in case a thread other than the one executing the main method throws an unhandled exception. If null,
        ///     <paramref name="onUnhandledMain"/> will be called instead.</param>
        public static void RunMain(Action main, Action<Exception> onUnhandledMain, Action<Exception> onUnhandledThread = null)
        {
            RunMain(() => { main(); return 0; }, excp => { onUnhandledMain(excp); return 0; }, onUnhandledThread);
        }

        /// <summary>
        ///     Execute the core of the application. In debug mode, exceptions are passed through untouched. In release mode, all
        ///     exceptions are caught, including those on other threads, and passed on to the specified handlers. Also, the main thread
        ///     is given the name "Main".</summary>
        /// <param name="main">
        ///     A method which executes the core of the application.</param>
        /// <param name="onUnhandledMain">
        ///     Method called in case the main method throws an unhandled exception. The return value is what this method will return.</param>
        /// <param name="onUnhandledThread">
        ///     Method called in case a thread other than the one executing the main method throws an unhandled exception. If null,
        ///     <paramref name="onUnhandledMain"/> will be called instead, and its return value will be ignored.</param>
        public static int RunMain(Func<int> main, Func<Exception, int> onUnhandledMain, Action<Exception> onUnhandledThread = null)
        {
            if (onUnhandledMain == null)
                throw new ArgumentNullException("onUnhandledMain");
            if (onUnhandledThread == null)
                onUnhandledThread = excp => { onUnhandledMain(excp); };

            Thread.CurrentThread.Name = "Main";

#if !DEBUG
            AppDomain.CurrentDomain.UnhandledException += (_, eargs) => onUnhandledThread(eargs.ExceptionObject as Exception);
#endif

#if !DEBUG
            try
#endif
            {
                return main();
            }
#if !DEBUG
            catch (Exception excp)
            {
                return onUnhandledMain(excp);
            }
#endif
        }
    }
}
