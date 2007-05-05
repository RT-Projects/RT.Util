using System;
using System.Collections.Generic;
using System.Text;

namespace RT.Util.EventSim
{
    public class EventEngine
    {
        /// <summary>
        /// The list of all scheduled events.
        /// </summary>
        private SortedList<double, Event> EL = new SortedList<double, Event>();
        /// <summary>
        /// Current simulation time.
        /// </summary>
        private double FTime;

        /// <summary>
        /// Gets the current simulation time.
        /// </summary>
        public double Time { get { return FTime; } }

        /// <summary>
        /// Gets the next event to occur, or null if none are scheduled.
        /// Note that this does not affect the queue of events.
        /// </summary>
        public Event NextEvent
        {
            get
            {
                if (EL.Count > 0)
                    return EL.Values[0];
                else
                    return null;
            }
        }

        /// <summary>
        /// Schedules a new event at the specified time.
        /// </summary>
        public double AddEvent(Event Event, double Time)
        {
            if (Time <= FTime)
                throw new Exception("Events can only be added in the future");

            while (EL.ContainsKey(Time))
                Time = BitConverter.Int64BitsToDouble(
                    BitConverter.DoubleToInt64Bits(Time) + 1);
            EL.Add(Time, Event);
            return Time;
        }

        /// <summary>
        /// Removes the specified event from the scheduled event queue.
        /// </summary>
        public void CancelEvent(Event Event)
        {
            int i = EL.IndexOfValue(Event);
            if (i >= 0)
                EL.RemoveAt(i);
        }

        /// <summary>
        /// Advances the simulation time to the next scheduled event. Invokes
        /// the event's callback and removes it from the event queue. Returns
        /// true if there are other events left, or false otherwise.
        /// </summary>
        public bool Tick()
        {
            // Check whether the simulation has finished
            if (EL.Count == 0)
                return false;

            Event NE = EL.Values[0];

            // Process the event
            FTime = EL.Keys[0];
            NE.Callback(NE, this);
            // Remove the event. There is no way the Callback could have added another
            // event before NE, so we can safely delete 0'th element.
            EL.RemoveAt(0);

            return true;
        }
    }
}
