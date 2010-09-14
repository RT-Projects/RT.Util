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

    /// <summary>
    /// Provides a more accurate way to measure the amount of CPU time consumed by a thread, using
    /// the new QueryThreadCycleTime call introduced in Vista. Unlike <see cref="Ut.Tic"/>, this measures in
    /// unspecified units, and only counts the times when this thread is actually in possession of a CPU time slice.
    /// Important: must be instantiated and used on the same thread for the results to be valid.
    /// </summary>
    public class TicTocCycles
    {
        private IntPtr _threadHandle;
        private ulong _ticCycles;
        private long _calibrationCycles;

        /// <summary>Constructor.</summary>
        public TicTocCycles()
        {
            _threadHandle = WinAPI.GetCurrentThread();

            _calibrationCycles = 0;

            long minCycles = long.MaxValue;
            for (int i = 0; i < 10000; i++)
            {
                long cycles;
                Tic();
                cycles = Toc();
                if (i > 200)  // it is important to discard the initial measurements because they are slower than the rest
                    if (minCycles > cycles)
                        minCycles = cycles;
            }

            _calibrationCycles = minCycles;
        }

        /// <summary>Indicates the start of a measurement interval.</summary>
        public void Tic()
        {
            WinAPI.QueryThreadCycleTime(_threadHandle, out _ticCycles);
        }

        /// <summary>Indicates the end of a measurement interval. Returns the amount of CPU time
        /// consumed by _this thread only_ since the last call to <see cref="Tic"/>.</summary>
        public long Toc()
        {
            ulong tocCycles;
            WinAPI.QueryThreadCycleTime(_threadHandle, out tocCycles);
            return (long) tocCycles - (long) _ticCycles - _calibrationCycles;
        }

    }
}
