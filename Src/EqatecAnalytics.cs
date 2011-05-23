using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using EQATEC.Analytics.Monitor;

namespace RT.Util
{
    /// <summary>
    /// Wraps common functionality when using EQATEC.Analytics, in particular, setting up unhandled exception handlers in Release mode.
    /// Also exposes the <see cref="EqatecAnalytics.Monitor"/> object, which is what applications use to track features.
    /// </summary>
    public static class EqatecAnalytics
    {
        /// <summary>Must be initialized to hold analytics monitor settings.</summary>
        public static AnalyticsMonitorSettings Settings;
        /// <summary>Optionally a function which returns true if the monitor may be started, or false if tracking should be disabled.</summary>
        public static Func<bool> CheckMayStart;
        /// <summary>Optionally a method to be invoked just before calling program core. Useful when different flavours of the application call different cores, but want to track something in common on startup.</summary>
        public static Action TrackImmediatelyAfterStart;
        /// <summary>Optionally a function that will be called whenever an unhandled exception occurs in release mode (to e.g. print an apology to the user before dying).</summary>
        public static Action<Exception> UnhandledException;
        /// <summary>Applications that come in several flavours, this may be modified to indicate which flavour is starting.</summary>
        public static string RunKind = "Runs";
        /// <summary>The value <see cref="RunMain(Func&lt;int&gt;)"/> should return in case of unhandled exception.</summary>
        public static int ReturnOnUnhandled = -12345;

        /// <summary>Analytics monitor instance - invoke methods on this instance to track features and enable/disable the tracking.</summary>
        public static IAnalyticsMonitor Monitor;

        /// <summary>Execute the core of the application, with all the necessary exception handlers in release mode and with Analytics configured.</summary>
        /// <param name="main">A method which executes the core of the application.</param>
        public static void RunMain(Action main)
        {
            RunMain(() => { main(); return 0; });
        }

        /// <summary>Execute the core of the application, with all the necessary exception handlers in release mode and with Analytics configured.</summary>
        /// <param name="main">A method which executes the core of the application.</param>
        public static int RunMain(Func<int> main)
        {
            if (Settings == null)
                throw new ArgumentException("EqatecAnalytics.Settings must not be null.");

            Thread.CurrentThread.Name = "Main";

#if !DEBUG
            AppDomain.CurrentDomain.UnhandledException += (_, eargs) =>
            {
                var excp = eargs.ExceptionObject as Exception;
                Monitor.TrackException(excp, "Thread: " + Thread.CurrentThread.Name);
                Monitor.ForceSync();
                if (UnhandledException != null)
                    UnhandledException(excp);
            };
#endif

            Monitor = AnalyticsMonitorFactory.Create(Settings);
            if (CheckMayStart == null || CheckMayStart())
            {
                Monitor.Start();
                Monitor.TrackFeature(RunKind);
                Monitor.TrackFeatureStart(RunKind);
            }

#if !DEBUG
            try
#endif
            {
                if (TrackImmediatelyAfterStart != null)
                    TrackImmediatelyAfterStart();
                var result = main();
                Monitor.TrackFeatureStop(RunKind);
                Monitor.Stop();
                return result;
            }
#if !DEBUG
            catch (Exception excp)
            {
                Monitor.TrackException(excp, "Thread: " + Thread.CurrentThread.Name);
                Monitor.TrackFeatureCancel(RunKind);
                Monitor.ForceSync();
                if (UnhandledException == null)
                    throw;
                else
                    UnhandledException(excp);
                return ReturnOnUnhandled;
            }
#endif
        }
    }
}
