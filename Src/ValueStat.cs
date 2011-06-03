using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RT.KitchenSink
{
    /// <summary>
    /// Incrementally accumulates basic statistics about a value, one observation at a time, making current estimates
    /// of the underlying distribution available at every point.
    /// </summary>
    public class ValueStat
    {
        /// <summary>Gets the total number of observations so far.</summary>
        public int ObservationCount { get; private set; }
        /// <summary>Gets the sample mean.</summary>
        public double Mean { get; private set; }
        /// <summary>Gets the current estimate of the population variance.</summary>
        public double Variance { get { return _m2 / ObservationCount; } }
        /// <summary>Gets the current estimate of the population variance using an unbiased estimator.</summary>
        public double VarianceUnbiased { get { return _m2 / (ObservationCount - 1); } }
        /// <summary>Gets the current estimate of the population standard deviation.</summary>
        public double StdDev { get { return Math.Sqrt(Variance); } }
        /// <summary>Gets the current estimate of the population standard deviation using an unbiased estimator.</summary>
        public double StdDevUnbiased { get { return Math.Sqrt(VarianceUnbiased); } }
        /// <summary>Gets the smallest value observed so far.</summary>
        public double Min { get; private set; }
        /// <summary>Gets the largest value observed so far.</summary>
        public double Max { get; private set; }

        private double _m2;

        /// <summary>Constructor.</summary>
        public ValueStat()
        {
            Min = Max = double.NaN;
        }

        /// <summary>Removes all observations and resets the class to its initial state.</summary>
        public void Clear()
        {
            Min = Max = double.NaN;
            ObservationCount = 0;
            Mean = _m2 = 0;
        }

        /// <summary>Adds a new observation, updating all statistics as appropriate.</summary>
        public void AddObservation(double value)
        {
            if (ObservationCount == 0)
            {
                Min = value;
                Max = value;
            }
            else
            {
                if (Min > value)
                    Min = value;
                if (Max < value)
                    Max = value;
            }

            ObservationCount += 1;
            double delta = value - Mean;
            Mean = Mean + delta / ObservationCount;
            _m2 = _m2 + delta * (value - Mean);
        }
    }

    /// <summary>
    /// Incrementally accumulates basic statistics about a value, one observation at a time, making current estimates
    /// of the underlying distribution available at every point.
    /// </summary>
    public class ValueStatDec
    {
        /// <summary>Gets the total number of observations so far.</summary>
        public int ObservationCount { get; private set; }
        /// <summary>Gets the sample mean.</summary>
        public decimal Mean { get; private set; }
        /// <summary>Gets the current estimate of the population variance.</summary>
        public decimal Variance { get { return _m2 / ObservationCount; } }
        /// <summary>Gets the current estimate of the population variance using an unbiased estimator.</summary>
        public decimal VarianceUnbiased { get { return _m2 / (ObservationCount - 1); } }
        /// <summary>Gets the current estimate of the population standard deviation.</summary>
        public double StdDev { get { return Math.Sqrt((double) Variance); } }
        /// <summary>Gets the current estimate of the population standard deviation using an unbiased estimator.</summary>
        public double StdDevUnbiased { get { return Math.Sqrt((double) VarianceUnbiased); } }
        /// <summary>Gets the smallest value observed so far.</summary>
        public decimal Min { get; private set; }
        /// <summary>Gets the largest value observed so far.</summary>
        public decimal Max { get; private set; }

        private decimal _m2;

        /// <summary>Constructor.</summary>
        public ValueStatDec()
        {
            Min = decimal.MaxValue;
            Max = decimal.MinValue;
        }

        /// <summary>Adds a new observation, updating all statistics as appropriate.</summary>
        public void AddObservation(decimal value)
        {
            if (ObservationCount == 0)
            {
                Min = value;
                Max = value;
            }
            else
            {
                if (Min > value)
                    Min = value;
                if (Max < value)
                    Max = value;
            }

            ObservationCount += 1;
            decimal delta = value - Mean;
            Mean = Mean + delta / ObservationCount;
            _m2 = _m2 + delta * (value - Mean);
        }
    }
}
