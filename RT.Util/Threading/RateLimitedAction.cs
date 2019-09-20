using System;
using System.Collections.Generic;
using System.Threading;
using RT.Util.ExtensionMethods;

namespace RT.Util.Threading
{
    /// <summary>
    /// Simplifies the task of performing a certain action on an arbitrary thread with a certain minimum interval
    /// between two invocations. Only a single thread will execute the task; no threads will block waiting for that.
    /// </summary>
    public class RateLimitedAction
    {
        private DateTime _lastTimestamp;

        /// <summary>Gets/sets the minimum interval between two invocations.</summary>
        public TimeSpan MinActionInterval { get; set; }

        /// <summary>Constructor.</summary>
        /// <param name="minActionInterval">The minimum interval between two invocations.</param>
        public RateLimitedAction(TimeSpan minActionInterval)
        {
            MinActionInterval = minActionInterval;
        }

        /// <summary>
        /// Checks whether it is time to execute the action. If so, executes it on the calling thread and returns only when
        /// the action is complete. If not, returns very quickly without blocking. If another thread is currently executing the
        /// action, returns very quickly without blocking.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        public void Check(Action action)
        {
            // Most threads perform this very quick check and do nothing
            if (DateTime.UtcNow - _lastTimestamp < MinActionInterval)
                return;
            // Any thread that gets here will proceed to do the check unless something else is already doing it.
            // So it's ok to do the timestamp now; we certainly don't need to let any *more* threads past the above check
            // at this time.
            _lastTimestamp = DateTime.UtcNow;

            bool gotlock = false;
            try
            {
                gotlock = Monitor.TryEnter(this);
                if (!gotlock)
                    return; // another thread is already doing the action
                action();
                _lastTimestamp = DateTime.UtcNow;
            }
            finally
            {
                if (gotlock)
                    Monitor.Exit(this);
            }
        }

        /// <summary>
        /// Makes it so that the next action will occur no earlier than the <see cref="MinActionInterval"/>.
        /// If action is already in progress, it will complete as usual.
        /// </summary>
        public void Postpone()
        {
            _lastTimestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Makes it so that the action will occur as soon as possible - namely, the next time a thread
        /// invokes the <see cref="Check"/> method.
        /// </summary>
        public void Expire()
        {
            _lastTimestamp = DateTime.MinValue;
        }
    }
}
