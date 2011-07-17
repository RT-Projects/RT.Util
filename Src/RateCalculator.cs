using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RT.KitchenSink
{
    /// <summary>Calculates the rate of arriving "stuff" over time.</summary>
    public class RateCalculator
    {
        private double _rate;
        private DateTime _laststamp;
        private int _counts;
        private double _scale = 1.0;

        /// <summary>Larger values average over longer time intervals.</summary>
        public double Scale { get { return _scale; } set { _scale = value; } }

        /// <summary>Gets the average rate of "stuff" arrival per second.</summary>
        public double Rate { get { return _rate; } }

        /// <summary>Updates the rate by adding statistics about the next installment of "stuff" arriving at the current time.</summary>
        public void Count(double amount)
        {
            _counts++;
            if (_counts == 2)
            {
                _rate = amount / (DateTime.UtcNow - _laststamp).TotalSeconds;
            }
            else if (_counts > 2)
            {
                var dt = (DateTime.UtcNow - _laststamp).TotalSeconds;
                var instrate = amount / dt;
                var ratio = 1 / (dt / _scale + 1);
                _rate = _rate * ratio + instrate * (1 - ratio);
            }
            _laststamp = DateTime.UtcNow;
        }
    }
}
