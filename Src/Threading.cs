using System;
using System.Collections.Generic;
using System.Threading;
using RT.Util.ExtensionMethods;

namespace RT.Util
{
    /// <summary>
    /// Provides a thread with the ability to yield (sleep) for a specified interval in a cleanly
    /// interruptible way.
    /// </summary>
    /// <remarks>This class itself is not entirely thread-safe - see individual members' restrictions.</remarks>
    public class ThreadSleeper
    {
        private readonly object _sleeplock = new object();
        private volatile bool _preventSleep = false;
        private volatile bool _preventSleepOnce = false;

        /// <summary>
        /// Causes the calling thread to sleep for the specified interval in a way that can be interrupted
        /// or prevented altogether using other methods of this class.
        /// <para>Threading: may only be called on the thread that "owns" this instance.</para>
        /// </summary>
        public void Sleep(int milliseconds)
        {
            lock (_sleeplock)
            {
                try
                {
                    if (_preventSleep || _preventSleepOnce) return;
                    if (milliseconds == int.MinValue)
                        Monitor.Wait(_sleeplock);
                    else
                        Monitor.Wait(_sleeplock, milliseconds);
                }
                finally
                {
                    _preventSleepOnce = false;
                }
            }
        }

        /// <summary>
        /// Causes the calling thread to sleep indefinitely, in a way that can be interrupted
        /// or prevented altogether using other methods of this class.
        /// <para>Threading: may only be called on the thread that "owns" this instance.</para>
        /// </summary>
        public void Sleep()
        {
            Sleep(int.MinValue);
        }

        /// <summary>
        /// Permanently prevents all further calls to Sleep from waiting - they will return immediately.
        /// If the owning thread is currently inside a Sleep, causes it to return immediately too.
        /// <para>Threading: thread-safe (call from any thread)</para>
        /// </summary>
        public void PreventSleep()
        {
            _preventSleep = true;
            lock (_sleeplock)
                Monitor.Pulse(_sleeplock);
        }

        /// <summary>
        /// If the owning thread is currently inside Sleep, will cause Sleep to return immediately;
        /// otherwise will cause the next call to Sleep to return immediately.
        /// <para>Threading: thread-safe (call from any thread)</para>
        /// </summary>
        public void PreventSleepOnce()
        {
            _preventSleepOnce = true;
            lock (_sleeplock)
                Monitor.Pulse(_sleeplock);
        }
    }

    /// <summary>
    /// Implements a threading primitive that allows a thread to easily expose methods that allow
    /// the thread to be terminated in a cooperative way. See also: example.
    /// </summary>
    /// <example>
    /// Intended use:
    /// <code>
    /// void ThreadProc()
    /// {
    ///     while (!myThreadExiter.ShouldExit)
    ///     {
    ///         // do work
    ///     }
    ///     myThreadExiter.SignalExited();
    /// }
    /// </code>
    /// </example>
    public class ThreadExiter
    {
        private readonly object _exitlock = new object();
        private volatile bool _shouldExit = false;
        private volatile bool _exited = false;

        /// <summary>
        /// Set this value to true to inform the thread that it should exit. Note that resetting this
        /// value back to false quickly may or may not result in the thread actually exiting.
        /// </summary>
        public bool ShouldExit { get { return _shouldExit; } set { _shouldExit = value; } }

        /// <summary>
        /// Indicates whether the thread has signalled that it has exited.
        /// </summary>
        public bool Exited { get { return _exited; } }

        /// <summary>
        /// Blocks until the thread has indicated that it has exited. Note that this method does not
        /// actually signal the thread to exit - see <see cref="ShouldExit"/> for that.
        /// </summary>
        public void WaitExited()
        {
            lock (_exitlock)
            {
                if (_exited) return;
                Monitor.Wait(_exitlock);
            }
        }

        /// <summary>
        /// Used by a thread to indicate that it is about to return from its thread proc. Causes all calls to
        /// <see cref="WaitExited"/>, if any, to return. Also makes <see cref="Exited"/> true.
        /// </summary>
        public void SignalExited()
        {
            _exited = true;
            lock (_exitlock)
                Monitor.PulseAll(_exitlock);
        }
    }

    /// <summary>
    /// Encapsulates a class performing a certain activity periodically, which can be initiated once
    /// and then permanently shut down, but not paused/resumed. The class owns its own separate
    /// _foreground_ thread, and manages this thread all by itself. The periodic task is executed on
    /// this thread.
    /// <para>The chief differences to <see cref="System.Threading.Timer"/> are as follows. This
    /// class will never issue overlapping activities, even if an activity takes much longer than the interval;
    /// the interval is between the end of the previous occurrence of the activity and the start of the next.
    /// The activity is executed on a foreground thread, and thus will complete once started, unless a
    /// catastrophic abort occurs. When shutting down the activity, it's possible to wait until the last
    /// occurrence, if any, has completed fully.</para>
    /// <para>Threading: unsafe (call public methods on the creating thread only) - or is it?</para>
    /// </summary>
    public abstract class Periodic
    {
        private Thread _thread;
        private ThreadExiter _exiter;
        private ThreadSleeper _sleeper;

        /// <summary>
        /// Override to indicate how long to wait between the call to <see cref="Start"/> and the first occurrence
        /// of the periodic activity.
        /// </summary>
        protected abstract TimeSpan FirstInterval { get; }

        /// <summary>
        /// Override to indicate how long to wait between second and subsequent occurrences of the periodic activity.
        /// </summary>
        protected abstract TimeSpan SubsequentInterval { get; }

        /// <summary>
        /// Override with a method that performs the desired periodic activity. If this method throws an exception
        /// the thread will terminate, but the <see cref="LastActivity"/> will occur nevertheless.
        /// </summary>
        protected abstract void PeriodicActivity();

        /// <summary>
        /// Override with a method that performs an activity on the same thread as <see cref="PeriodicActivity"/> during
        /// shutdown, just before signalling that the shutdown is complete. The default implementation of this method
        /// does nothing. This method is guaranteed to be called during a shutdown, even if the shutdown is due to an
        /// exception propagating outside of <see cref="PeriodicActivity"/>.
        /// </summary>
        protected virtual void LastActivity() { }

        /// <summary>
        /// Returns false before the first call to <see cref="Start"/> and after the first call to <see cref="Shutdown"/>;
        /// true between them.
        /// </summary>
        public bool IsRunning { get { return _exiter != null && !_exiter.ShouldExit; } }

        /// <summary>
        /// Schedules the periodic activity to start occurring. This method may only be called once.
        /// </summary>
        public virtual void Start()
        {
            if (_thread != null)
                throw new InvalidOperationException("\"Start\" called multiple times ({0})".Fmt(GetType().Name));

            _exiter = new ThreadExiter();
            _sleeper = new ThreadSleeper();
            _thread = new Thread(threadProc);
            _thread.Start();
        }

        private volatile bool _periodicActivityRunning = false;

        /// <summary>
        /// Causes the periodic activity to stop occurring. If called while the activity is being performed,
        /// will wait until the activity has completed before returning. Ensures that <see cref="IsRunning"/>
        /// is false once this method returns.
        /// </summary>
        public virtual bool Shutdown(bool waitForExit)
        {
            if (waitForExit && _periodicActivityRunning && Thread.CurrentThread.ManagedThreadId == _thread.ManagedThreadId)
                throw new InvalidOperationException("Cannot call Shutdown(true) from within PeriodicActivity() on the same thread (this would cause a deadlock).");
            if (_exiter == null || _exiter.ShouldExit)
                return false;
            _exiter.ShouldExit = true;
            _sleeper.PreventSleep();
            if (waitForExit)
                _exiter.WaitExited();
            return true;
        }

        private void threadProc()
        {
            try
            {
                _sleeper.Sleep((int) FirstInterval.TotalMilliseconds);
                while (!_exiter.ShouldExit)
                {
                    _periodicActivityRunning = true;
                    PeriodicActivity();
                    _periodicActivityRunning = false;
                    _sleeper.Sleep((int) SubsequentInterval.TotalMilliseconds);
                }
            }
            finally
            {
                try { LastActivity(); }
                finally { _exiter.SignalExited(); }
            }
        }
    }

    /// <summary>
    /// Encapsulates a class performing multiple related yet independent tasks on the same thread
    /// at a certain minimum interval each. Schedules the activity that is the most late at every opportunity,
    /// but will never execute more than one activity at a time (as they all share the same thread).
    /// <para>Threading: unsafe (call public methods on the creating thread only)</para>
    /// </summary>
    public abstract class PeriodicMultiple : Periodic
    {
        /// <summary>
        /// Used to define the activities to be executed periodically.
        /// </summary>
        protected class Task
        {
            /// <summary>The activity to be performed.</summary>
            public Action Action;
            /// <summary>The mimimum interval at which this activity should be repeated. May be delayed arbitrarily though.</summary>
            public TimeSpan MinInterval;
            /// <summary>Stores the last time this activity was executed.</summary>
            public DateTime LastExecuted;
            /// <summary>Calculates by how much this activity has been delayed. Is used internally to pick the next activity to run. Returns negative values for activities that aren't due yet.</summary>
            public TimeSpan DelayedBy()
            {
                if (LastExecuted == default(DateTime))
                    return TimeSpan.FromDays(1000) - MinInterval; // to run shortest interval first when none of the tasks have ever executed
                else
                    return (DateTime.UtcNow - LastExecuted) - MinInterval;
            }
        }

        /// <summary>If desired, override to provide a custom interval at which the scheduler
        /// should re-check whether any activity is due to start. Defaults to 1 second.</summary>
        protected override TimeSpan SubsequentInterval { get { return TimeSpan.FromSeconds(1); } }

        /// <summary>Initialise this with the list of activities to be executed.</summary>
        protected IList<Task> Tasks;

        /// <summary>For internal use.</summary>
        protected sealed override void PeriodicActivity()
        {
            TimeSpan maxDelay = TimeSpan.MinValue;
            Task maxDelayTask = null;

            foreach (var task in Tasks)
            {
                var delayedBy = task.DelayedBy();
                if (maxDelay < delayedBy && delayedBy > TimeSpan.Zero)
                {
                    maxDelay = delayedBy;
                    maxDelayTask = task;
                }
            }

            if (maxDelayTask != null)
            {
                maxDelayTask.LastExecuted = DateTime.UtcNow;
                maxDelayTask.Action();
            }
        }
    }
}
