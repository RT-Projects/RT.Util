using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using RT.Util.ExtensionMethods;

namespace RT.Util
{
    /// <summary>
    /// Helps limit the rate of an action to be within all of the defined rate limits, by suspending the calling thread
    /// until the action may be executed without exceeding any of the limits. Warning: this class is not thread-safe!
    /// </summary>
    public sealed class Waiter
    {
        /// <summary>A list of limits to be observed. The waiter will ensure that *all* of these limits are satisfied.</summary>
        public List<WaiterLimit> Limits = new List<WaiterLimit>();
        /// <summary>Whenever a wait is triggered, a warning is reported through this logger.</summary>
        public LoggerBase Log { get; set; }

        /// <summary>Constructor.</summary>
        /// <param name="limits">The limits to be observed.</param>
        public Waiter(params WaiterLimit[] limits) : this(null, limits) { }

        /// <summary>Constructor.</summary>
        /// <param name="log">Whenever a wait is triggered, a warning is reported through this logger.</param>
        /// <param name="limits">The limits to be observed.</param>
        public Waiter(LoggerBase log, params WaiterLimit[] limits)
        {
            if (limits.Length == 0)
                throw new ArgumentException("Must specify at least one limit");
            Limits = limits.ToList();
            Log = log == null ? new NullLogger() : log;
        }

        /// <summary>
        /// Call this method every time before performing the limited action. If the action would exceed any of the limits,
        /// the call will block until the action can be performed without exceeding any limits.
        /// </summary>
        public void WaitIfNecessary()
        {
            var max = Limits.Select(l => new { limit = l, wait = l.WaitRequired() }).MaxElement(z => z.wait);
            if (max.wait > TimeSpan.Zero)
            {
                Log.Warn("Would exceed allowed occurrence frequency: max {0} occ. per {1}; sleeping until {2}".Fmt(max.limit.MaxOccurrences, max.limit.Interval, DateTime.Now + max.wait));
                Thread.Sleep(max.wait);
            }
            foreach (var limit in Limits)
                limit.Occurrence();
        }
    }

    /// <summary>
    /// Defines a rate limit for use with <see cref="Waiter"/>, as an absolute maximum number of occurrences
    /// permitted in the specified time interval.
    /// </summary>
    public sealed class WaiterLimit
    {
        /// <summary>Time interval to which the limit applies.</summary>
        public TimeSpan Interval { get; set; }
        /// <summary>Maximum number of occurrences allowed per time interval.</summary>
        public int MaxOccurrences { get; set; }

        /// <summary>Constructor.</summary>
        /// <param name="interval">Time interval to which the limit applies.</param>
        /// <param name="maxOccurrences">Maximum number of occurrences allowed per time interval.</param>
        public WaiterLimit(TimeSpan interval, int maxOccurrences)
        {
            Interval = interval;
            MaxOccurrences = maxOccurrences;
            _firstRequest = DateTime.UtcNow - TimeSpan.FromSeconds(Interval.TotalSeconds / 2);
        }

        private DateTime _firstRequest = DateTime.MinValue;
        private Queue<DateTime> _requests = new Queue<DateTime>();

        internal TimeSpan WaitRequired()
        {
            while (_requests.Count > 0 && DateTime.UtcNow - _requests.Peek() > Interval)
                _requests.Dequeue();

            if (_firstRequest == DateTime.MinValue)
            {
                _firstRequest = DateTime.UtcNow;
                return TimeSpan.Zero;
            }
            else
            {
                double allowed = MaxOccurrences * Math.Min((DateTime.UtcNow - _firstRequest).TotalSeconds, Interval.TotalSeconds) / Interval.TotalSeconds;
                return TimeSpan.FromSeconds((_requests.Count - allowed) * (Interval.TotalSeconds / MaxOccurrences));
            }
        }

        internal void Occurrence()
        {
            while (_requests.Count > 0 && DateTime.UtcNow - _requests.Peek() > Interval)
                _requests.Dequeue();

            _requests.Enqueue(DateTime.UtcNow);
        }
    }
}
