using System;
using System.Collections.Generic;
using System.Text;

namespace RT.Util.EventSim
{
    public abstract class EventTask
    {
        private IEnumerator<double> TheProc;
        private Event TheEvent;
        private EventEngine TheEngine;

        private void Callback(Event CbkEvent, EventEngine Engine)
        {
            if (TheProc == null)
                throw new Exception("Callback received for stopped process");

            if (!TheProc.MoveNext())
            {
                TheProc = null;
                TheEvent = null;
                return;
            }

            // Create an event for this delay
            // We can reuse the event we've just received. We also know that it's
            // the same one as stored in LastEvent.
            Engine.AddEvent(CbkEvent, Engine.Time + TheProc.Current);
        }

        public abstract IEnumerator<double> Process();

        public void Start(EventEngine Engine)
        {
            if (TheProc != null)
                throw new Exception("Cannot start task because it is already running");

            TheProc = Process();
            if (!TheProc.MoveNext())
            {
                TheProc = null;
                return;
            }

            TheEngine = Engine;

            // Create a new event to be used for advancing the process
            TheEvent = new Event();
            TheEvent.Callback = new EventCallback(Callback);
            TheEvent.User1 = this;
            TheEvent.User2 = Engine;
            // Schedule it with the first delay returned by the process code
            Engine.AddEvent(TheEvent, Engine.Time + TheProc.Current);
        }

        public void StopReset()
        {
            if (TheProc == null)
                throw new Exception("Cannot stop task because it is not running");

            TheEngine.CancelEvent(TheEvent);

            TheProc = null;
            TheEvent = null;
            TheEngine = null;
        }
    }
}
