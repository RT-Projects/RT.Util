using System;
using System.Collections.Generic;
using System.Text;

namespace RT.Util
{
    /// <summary>
    /// This class records the time when it was created and a time-out interval. It provides
    /// a method to check whether the time-out interval has elapsed and another one to check
    /// how much time is left until the interval elapses.
    /// </summary>
    public sealed class TimeOut
    {
        private TimeSpan _interval;
        private DateTime _startTime;
        private TimeOut() { }

        /// <summary>
        /// Constructs an instance of the time-out class starting immediately and timing out
        /// after the Interval has elapsed.
        /// </summary>
        public TimeOut(TimeSpan interval)
        {
            _interval = interval;
            _startTime = DateTime.Now;
        }

        /// <summary>
        /// Constructs an instance of the time-out class starting immediately and timing out
        /// after the specified number of Seconds has elapsed.
        /// </summary>
        public TimeOut(double seconds)
        {
            _interval = TimeSpan.FromSeconds(seconds);
            _startTime = DateTime.Now;
        }

        /// <summary>
        /// Returns whether the time-out has occurred.
        /// </summary>
        public bool TimedOut
        {
            get { return TimeSpan.Compare(DateTime.Now - _startTime, _interval) >= 0; }
        }

        /// <summary>
        /// Returns how much time is left until the time-out.
        /// </summary>
        public TimeSpan TimeLeft
        {
            get
            {
                TimeSpan ts = (_startTime + _interval) - DateTime.Now;
                return ts < TimeSpan.Zero ? TimeSpan.Zero : ts;
            }
        }
    }
}
