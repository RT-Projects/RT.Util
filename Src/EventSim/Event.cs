using System;
using System.Collections.Generic;
using System.Text;

namespace RT.Util.EventSim
{
    public class Event
    {
        public double Time;
        public EventCallback Callback;
        public object User1, User2, User3;

        private bool Registered = false;

        public Event()
        {
        }

        /// <summary>
        /// This can only be called once; further invocations on the same event
        /// will throw an exception
        /// </summary>
        public void Register()
        {
            if (Registered)
                throw new Exception("Event is already registered.");

            EventEngine.AddEvent(this);
            Registered = true;
        }

        public void Unregister()
        {
            if (!Registered)
                throw new Exception("Event is not yet registered; cannot unregister.");

            EventEngine.CancelEvent(this);
            Registered = false;
        }
    }
}
