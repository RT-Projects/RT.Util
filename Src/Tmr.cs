using System;
using System.Runtime.InteropServices;

namespace RT.Util
{
    /// <summary>
    ///     Base class for a timer with particularly self-explanatory method names which are chainable. See Remarks.</summary>
    /// <typeparam name="T">
    ///     The type of the values that the timer returns. This may be a reference type if need be, as long as the descendant
    ///     fully supports a zero being equal to <c>default(T)</c>.</typeparam>
    /// <remarks>
    ///     Idiomatic use example: <c>var tmr = new TmrSeconds().StartAndZero();</c></remarks>
    public abstract class Tmr<T>
    {
        /// <summary>
        ///     Expected to return the amount of time elapsed since the last call to <see cref="zero"/>, plus the specified
        ///     additional amount of time.</summary>
        /// <param name="add">
        ///     A length of time to be added to the actual time elapsed since the last call to <see cref="zero"/>.</param>
        protected abstract T read(T add);

        /// <summary>
        ///     Expected to zero the timer, so that future calls to <see cref="read"/> return a value relative to the last time
        ///     this method was called.</summary>
        protected abstract void zero();

        private bool _running = false;
        private T _pausedAt;

        /// <summary>
        ///     Gets the current reading of the timer. This value increases between consecutive reads while the timer is running,
        ///     and remains fixed while it's paused.</summary>
        public T Current { get { return _running ? read(_pausedAt) : _pausedAt; } }
        /// <summary>
        ///     Gets the reading the timer had just before the last call to any of the methods of this class related to zeroing,
        ///     pausing and resuming the timer.</summary>
        public T Last { get; private set; }

        /// <summary>Instantiates the timer. The timer will be zeroed, and it will not be running.</summary>
        public Tmr()
        {
            zero();
        }

        /// <summary>Resets the timer back to zero.</summary>
        public Tmr<T> Zero()
        {
            Last = Current;
            _pausedAt = default(T);
            zero();
            return this;
        }

        /// <summary>Resets the timer and makes sure it's running. The current state of the timer is irrelevant.</summary>
        public Tmr<T> StartAndZero()
        {
            Last = Current;
            _running = true;
            _pausedAt = default(T);
            zero();
            return this;
        }

        /// <summary>Makes sure the timer is not running and zeroes it. The current state of the timer is irrelevant.</summary>
        public Tmr<T> StopAndZero()
        {
            Last = Current;
            _running = false;
            _pausedAt = default(T);
            zero();
            return this;
        }

        /// <summary>
        ///     Pauses the timer without zeroing it. This method may be called while the timer is already paused, but a single
        ///     call to <see cref="Continue"/> is always sufficient to resume the timer.</summary>
        public Tmr<T> Pause()
        {
            _pausedAt = Current;
            _running = false; // must occur after reading Current
            return this;
        }

        /// <summary>
        ///     Makes sure the timer is running without zeroing the time reading. If the reading is non-zero, it will continue
        ///     growing. This method may be called while the timer is already running.</summary>
        public Tmr<T> Continue()
        {
            if (!_running)
            {
                zero();
                _running = true;
            }
            return this;
        }
    }

    /// <summary>
    ///     A timer measuring the number of real-time seconds elapsed between the calls. See Remarks on <see
    ///     cref="Tmr&lt;T&gt;"/>.</summary>
    public class TmrSeconds : Tmr<double>
    {
        private long _start = 0;

        /// <summary>Override; see base.</summary>
        protected override double read(double add)
        {
            long now;
            QueryPerformanceCounter(out now);
            return add + ((double) (now - _start)) / (double) _performanceFreq;
        }

        /// <summary>Override; see base.</summary>
        protected override void zero()
        {
            QueryPerformanceCounter(out _start);
        }

        private static readonly long _performanceFreq;

        static TmrSeconds() { QueryPerformanceFrequency(out _performanceFreq); }
        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceFrequency(out long lpFrequency);
        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

        /// <summary>Resets the timer back to zero.</summary>
        public new TmrSeconds Zero() { base.Zero(); return this; }
        /// <summary>Resets the timer and makes sure it's running. The current state of the timer is irrelevant.</summary>
        public new TmrSeconds StartAndZero() { base.StartAndZero(); return this; }
        /// <summary>Makes sure the timer is not running and zeroes it. The current state of the timer is irrelevant.</summary>
        public new TmrSeconds StopAndZero() { base.StopAndZero(); return this; }
        /// <summary>
        ///     Pauses the timer without zeroing it. This method may be called while the timer is already paused, but a single
        ///     call to <see cref="Continue"/> is always sufficient to resume the timer.</summary>
        public new TmrSeconds Pause() { base.Pause(); return this; }
        /// <summary>
        ///     Makes sure the timer is running without zeroing the time reading. If the reading is non-zero, it will continue
        ///     growing. This method may be called while the timer is already running.</summary>
        public new TmrSeconds Continue() { base.Continue(); return this; }
    }

    /// <summary>
    ///     A timer measuring the number of CPU clock cycles consumed by the calling thread between the calls. To ensure correct
    ///     operation, the timer must be instantiated on the same thread on which the measurements will be performed. See Remarks
    ///     on <see cref="Tmr&lt;T&gt;"/>.</summary>
    public sealed class TmrCycles : Tmr<long>
    {
        private IntPtr _threadHandle;
        private ulong _ticCycles;
        private long _calibrationCycles;

        /// <summary>Override; see base.</summary>
        protected override long read(long add)
        {
            ulong tocCycles;
            QueryThreadCycleTime(_threadHandle, out tocCycles);
            return add + (long) tocCycles - (long) _ticCycles - _calibrationCycles;
        }

        /// <summary>
        ///     Constructor. Calibrates the timer. To ensure correct operation, the timer must be instantiated on the same thread
        ///     on which the measurements will be performed.</summary>
        public TmrCycles()
        {
            _threadHandle = GetCurrentThread();

            _calibrationCycles = 0;

            long minCycles = long.MaxValue;
            for (int i = 0; i < 10000; i++)
            {
                long cycles;
                StartAndZero();
                cycles = Current;
                if (i > 200)
                    if (minCycles > cycles)
                        minCycles = cycles;
            }

            _calibrationCycles = minCycles;
            StopAndZero();
        }

        /// <summary>Override; see base.</summary>
        protected override void zero()
        {
            QueryThreadCycleTime(_threadHandle, out _ticCycles);
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentThread();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool QueryThreadCycleTime(IntPtr threadHandle, out ulong cycleTime);

        /// <summary>Resets the timer back to zero.</summary>
        public new TmrCycles Zero() { base.Zero(); return this; }
        /// <summary>Resets the timer and makes sure it's running. The current state of the timer is irrelevant.</summary>
        public new TmrCycles StartAndZero() { base.StartAndZero(); return this; }
        /// <summary>Makes sure the timer is not running and zeroes it. The current state of the timer is irrelevant.</summary>
        public new TmrCycles StopAndZero() { base.StopAndZero(); return this; }
        /// <summary>
        ///     Pauses the timer without zeroing it. This method may be called while the timer is already paused, but a single
        ///     call to <see cref="Continue"/> is always sufficient to resume the timer.</summary>
        public new TmrCycles Pause() { base.Pause(); return this; }
        /// <summary>
        ///     Makes sure the timer is running without zeroing the time reading. If the reading is non-zero, it will continue
        ///     growing. This method may be called while the timer is already running.</summary>
        public new TmrCycles Continue() { base.Continue(); return this; }
    }
}
