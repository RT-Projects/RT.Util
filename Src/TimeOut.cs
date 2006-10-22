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
    public class TimeOut
    {
        /// Hidden from public use
        private TimeSpan Interval;
        private DateTime StartTime;
        private TimeOut() { }

        /// <summary>
        /// Constructs an instance of the time-out class starting immediately and timing out
        /// after the Interval has elapsed.
        /// </summary>
        public TimeOut(TimeSpan Interval)
        {
            this.Interval = Interval;
            this.StartTime = DateTime.Now;
        }

        /// <summary>
        /// Returns whether the time-out has occurred.
        /// </summary>
        public bool TimedOut
        {
            get { return TimeSpan.Compare(DateTime.Now - StartTime, Interval) >= 0; }
        }

        /// <summary>
        /// Returns how much time is left until the time-out.
        /// </summary>
        public TimeSpan TimeLeft
        {
            get
            {
                TimeSpan ts = (StartTime + Interval) - DateTime.Now;
                return ts < TimeSpan.Zero ? TimeSpan.Zero : ts;
            }
        }
    }
}
