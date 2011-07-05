using System;
using System.Collections.Generic;
using System.Linq;

namespace RT.Util
{
    /// <summary>Measures frame rate statistics (or, more generally, statistics of any event occurring multiple times in real time).</summary>
    public class FpsCounter
    {
        private Queue<double> _frameTimes = new Queue<double>(32);
        private long _lastCount;
        private double _secondsPerCount = 1.0 / WinAPI.PerformanceFreq;

        /// <summary>Gets the average length of one frame over the past second, in seconds.</summary>
        public double AverageFrameTime { get; private set; }
        /// <summary>Gets the minimum length of one frame over the past second, in seconds.</summary>
        public double MinFrameTime { get; private set; }
        /// <summary>Gets the maximum length of one frame over the past second, in seconds.</summary>
        public double MaxFrameTime { get; private set; }
        /// <summary>Gets the length of the last frame, in seconds.</summary>
        public double LastFrameTime { get; private set; }
        /// <summary>Gets the average number of frames per second, over the last second.</summary>
        public double AverageFps { get { return 1.0 / AverageFrameTime; } }

        /// <summary>Constructor.</summary>
        public FpsCounter()
        {
            Reset();
        }

        /// <summary>Counts another frame and updates all statistics. Returns the time since the last frame,
        /// or null if this is the first counted frame.</summary>
        public double? CountFrame()
        {
            double? result = null;
            long count;
            WinAPI.QueryPerformanceCounter(out count);
            if (_lastCount != 0)
            {
                while (_frameTimes.Count > 5 && _frameTimes.Count > AverageFps / 2)
                    delFromStats();
                result = (count - _lastCount) * _secondsPerCount;
                addToStats(result.Value);
            }
            _lastCount = count;
            return result;
        }

        /// <summary>Resets all statistics.</summary>
        public void Reset()
        {
            _frameTimes.Clear();
            _lastCount = 0;
            AverageFrameTime = 1.0;
            MinFrameTime = 1000.0;
            MaxFrameTime = 0.0;
        }

        private void addToStats(double frametime)
        {
            _frameTimes.Enqueue(frametime);
            LastFrameTime = frametime;
            MinFrameTime = Math.Min(MinFrameTime, frametime);
            MaxFrameTime = Math.Max(MaxFrameTime, frametime);
            AverageFrameTime = (AverageFrameTime * (_frameTimes.Count - 1) + frametime) / _frameTimes.Count;
        }

        private void delFromStats()
        {
            var frametime = _frameTimes.Dequeue();
            if (frametime == MinFrameTime)
                MinFrameTime = _frameTimes.Min();
            if (frametime == MaxFrameTime)
                MaxFrameTime = _frameTimes.Max();
            AverageFrameTime = (AverageFrameTime * (_frameTimes.Count + 1) - frametime) / _frameTimes.Count;
        }
    }

}
