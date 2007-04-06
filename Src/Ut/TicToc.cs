/// TicToc.cs  -  simple timer functions

using System;

namespace RT.Util
{
    public static partial class Ut
    {
        private static long TicStart = 0;

        /// <summary>
        /// Starts a simple performance timer.
        /// </summary>
        public static void Tic()
        {
            WinAPI.QueryPerformanceCounter(out TicStart);
        }

        /// <summary>
        /// Stops a simple performance timer started by Tic() and returns the
        /// number of seconds elapsed, accurate to the resolution of the system
        /// timer. Note that there exists only a single timer; Toc returns the
        /// time since the very last call to Tic.
        /// </summary>
        public static double Toc()
        {
            long TicStop;
            WinAPI.QueryPerformanceCounter(out TicStop);
            return ((double)(TicStop - TicStart)) / (double)WinAPI.PerformanceFreq;
        }
    }
}
