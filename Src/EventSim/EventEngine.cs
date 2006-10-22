using System;
using System.Collections.Generic;
using System.Text;

namespace RT.Util.EventSim
{
    public static class EventEngine
    {
        private static SortedList<double, Event> EL = new SortedList<double, Event>();
        private static double FTime;

        public static double Time { get { return FTime; } }

        public static Event NextEvent
        {
            get
            {
                if (EL.Count > 0)
                    return EL.Values[0];
                else
                    return null;
            }
        }

        public static void AddEvent(Event Event)
        {
            if (Event.Time <= FTime)
                throw new Exception("Events can only be added in the future");
            else
                EL.Add(Event.Time, Event);
        }

        public static void CancelEvent(Event Event)
        {
            int i = EL.IndexOfValue(Event);
            if (i >= 0)
                EL.RemoveAt(i);
        }

        public static bool Tick()
        {
            // Check whether the simulation has finished
            if (EL.Count == 0)
                return false;

            Event NE = EL.Values[0];

            // Process the event
            FTime = NE.Time;
            NE.Callback(NE);
            // Remove the event. There is no way the Callback could have added another
            // event before NE, so we can safely delete 0'th element.
            EL.RemoveAt(0);

            return true;
        }
    }
}
