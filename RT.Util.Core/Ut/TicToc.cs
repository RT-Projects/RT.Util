namespace RT.Util;

public static partial class Ut
{
    private static long _start = 0;

    /// <summary>
    ///     Starts / resets a simple performance timer. Returns the number of seconds elapsed since the last call to <see
    ///     cref="Tic"/>, or zero if this is the first call. See also <see cref="Toc"/>.</summary>
    public static double Tic()
    {
        long prevStart = _start;
        WinAPI.QueryPerformanceCounter(out _start);
        return prevStart == 0 ? 0 : ((_start - prevStart) / (double) WinAPI.PerformanceFreq);
    }

    /// <summary>Returns the number of seconds elapsed since the last call to <see cref="Tic"/>.</summary>
    public static double Toc()
    {
        long stop;
        WinAPI.QueryPerformanceCounter(out stop);
        return (stop - _start) / (double) WinAPI.PerformanceFreq;
    }
}

/// <summary>
///     Provides a more accurate way to measure the amount of CPU time consumed by a thread, using the new
///     QueryThreadCycleTime call introduced in Vista. Unlike <see cref="Ut.Tic"/>, this measures in unspecified units, and
///     only counts the times when this thread is actually in possession of a CPU time slice.</summary>
public class TicTocCycles
{
    private IntPtr _threadHandle;
    private ulong _ticCycles;
    private long _calibrationCycles;

    /// <summary>
    ///     Constructor. For the results to be valid, this class must be instantiated on the same thread on which the
    ///     measurements are to be made.</summary>
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

    /// <summary>
    ///     Starts / resets the timer. Returns the amount of CPU time consumed by this thread only since the last call to
    ///     <see cref="Tic"/>, or zero if this is the first call.</summary>
    public long Tic()
    {
        ulong prevTicCycles = _ticCycles;
        WinAPI.QueryThreadCycleTime(_threadHandle, out _ticCycles);
        return prevTicCycles == 0 ? 0 : ((long) _ticCycles - (long) prevTicCycles - _calibrationCycles);
    }

    /// <summary>Returns the amount of CPU time consumed by this thread only since the last call to <see cref="Tic"/>.</summary>
    public long Toc()
    {
        ulong tocCycles;
        WinAPI.QueryThreadCycleTime(_threadHandle, out tocCycles);
        return (long) tocCycles - (long) _ticCycles - _calibrationCycles;
    }

}
