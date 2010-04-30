using System;
using System.Collections.Generic;

namespace RT.KitchenSink.EventSim
{
    /// <summary>
    /// Implements an event-driven time-based simulation.
    /// The idea is to "skip over" chunks of time where nothing of interest happens.
    /// This is done by maintaining a queue of known events, each with a simulation time
    /// at which the event will occur. Whenever the Event "occurs", its callback is invoked.
    /// This can insert new events in response, however these must occur in the future.
    /// Thus, if the first queued event is in 3.5 seconds of simulated time, the simulation
    /// can jump straight over the 3.5 seconds because there is no way any other events of
    /// interest could have occurred in this time.
    /// </summary>
    public sealed class EventEngine
    {
        /// <summary>
        /// The list of all scheduled events.
        /// </summary>
        private SortedList<double, Event> _el = new SortedList<double, Event>();
        /// <summary>
        /// Current simulation time.
        /// </summary>
        private double _time;

        /// <summary>
        /// Gets the current simulation time.
        /// </summary>
        public double Time { get { return _time; } }

        /// <summary>
        /// Gets the next event to occur, or null if none are scheduled.
        /// Note that this does not affect the queue of events.
        /// </summary>
        public Event NextEvent
        {
            get
            {
                if (_el.Count > 0)
                    return _el.Values[0];
                else
                    return null;
            }
        }

        /// <summary>
        /// Schedules a new event at the specified time.
        /// </summary>
        public double AddEvent(Event evt, double time)
        {
            if (time <= _time)
                throw new Exception("Events can only be added in the future");

            while (_el.ContainsKey(time))
                time = BitConverter.Int64BitsToDouble(
                    BitConverter.DoubleToInt64Bits(time) + 1);
            _el.Add(time, evt);
            return time;
        }

        /// <summary>
        /// Removes the specified event from the scheduled event queue.
        /// </summary>
        public void CancelEvent(Event evt)
        {
            int i = _el.IndexOfValue(evt);
            if (i >= 0)
                _el.RemoveAt(i);
        }

        /// <summary>
        /// Advances the simulation time to the next scheduled event. Invokes
        /// the event's callback and removes it from the event queue. Returns
        /// true if there are other events left, or false otherwise.
        /// </summary>
        public bool Tick()
        {
            // Check whether the simulation has finished
            if (_el.Count == 0)
                return false;

            Event ne = _el.Values[0];

            // Process the event
            _time = _el.Keys[0];
            ne.Callback(ne, this);
            // Remove the event. There is no way the Callback could have added another
            // event before NE, but it might have deleted the 0'th event.
            if (_el.Values[0] == ne)
                _el.RemoveAt(0);

            return true;
        }
    }
}
