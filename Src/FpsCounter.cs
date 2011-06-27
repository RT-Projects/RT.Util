using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RT.Util;

namespace RT.KitchenSink
{
    public class FpsCounter
    {
        private Queue<double> _frameTimes = new Queue<double>(32);
        private long _lastCount;
        private double _secondsPerCount = 1.0 / WinAPI.PerformanceFreq;

        public double AverageFrameTime { get; private set; }
        public double MinFrameTime { get; private set; }
        public double MaxFrameTime { get; private set; }
        public double LastFrameTime { get; private set; }
        public double AverageFps { get { return 1.0 / AverageFrameTime; } }

        public FpsCounter()
        {
            Reset();
        }

        public double? CountFrame()
        {
            double? result = null;
            long count;
            WinAPI.QueryPerformanceCounter(out count);
            if (_lastCount != 0)
            {
                while (_frameTimes.Count > 30)
                    delFromStats();
                result = (count - _lastCount) * _secondsPerCount;
                addToStats(result.Value);
            }
            _lastCount = count;
            return result;
        }

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
