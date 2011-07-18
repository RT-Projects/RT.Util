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
        private DateTime _laststamp, _firststamp;
        private double _lastamount;
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
            if (_counts == 1)
            {
                _firststamp = DateTime.UtcNow;
            }
            else if (_counts == 2 || DateTime.UtcNow - _firststamp < TimeSpan.FromSeconds(1))
            {
                _lastamount += amount;
                _rate = _lastamount / (DateTime.UtcNow - _firststamp).TotalSeconds;
            }
            else if (_counts > 2)
            {
                var dt = (DateTime.UtcNow - _laststamp).TotalSeconds;
                if (dt == 0)
                {
                    _lastamount += amount;
                    return;
                }
                var instrate = (amount + _lastamount) / dt;
                _lastamount = 0;
                var ratio = 1 / (dt / _scale + 1);
                _rate = _rate * ratio + instrate * (1 - ratio);
            }
            _laststamp = DateTime.UtcNow;
        }
    }
}
