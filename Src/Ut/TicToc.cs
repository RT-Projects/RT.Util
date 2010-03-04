using System;

namespace RT.Util
{
    public static partial class Ut
    {
        private static long _start = 0;

        /// <summary>
        /// Starts a simple performance timer. Use <see cref="Toc"/> to stop.
        /// </summary>
        public static void Tic()
        {
            WinAPI.QueryPerformanceCounter(out _start);
        }

        /// <summary>
        /// Stops a simple performance timer started by <see cref="Tic"/> and returns the
        /// number of seconds elapsed, accurate to the resolution of the system
        /// timer. Note that there exists only a single timer; <see cref="Toc"/> returns the
        /// time since the very last call to <see cref="Tic"/>.
        /// </summary>
        public static double Toc()
        {
            long stop;
            WinAPI.QueryPerformanceCounter(out stop);
            return ((double) (stop - _start)) / (double) WinAPI.PerformanceFreq;
        }
    }
}
