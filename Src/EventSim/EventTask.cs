using System;
using System.Collections.Generic;

namespace RT.Util.EventSim
{
    /// <summary>
    /// Base class for implementing a Task (which is a piece of C# code) with
    /// delays injected as appropriate. The descendant implements Process, and
    /// yield returns a delay in simulation time units. The <see cref="EventTask"/>
    /// will schedule an event with the <see cref="EventEngine"/> to signify that
    /// the delay has elapsed, at which point the process will be resumed.
    /// Thus several tasks can be simulated to be happening "in parallel" in the
    /// simulated universe, with known and strictly defined delays. Moreover,
    /// when all tasks are sleeping the simulation can simply "skip over" this
    /// interval of no activity.
    /// </summary>
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

        /// <summary>
        /// Override to implement the task that needs to be simulated.
        /// </summary>
        public abstract IEnumerator<double> Process();

        /// <summary>
        /// Starts the task running in the specified <see cref="EventEngine"/>.
        /// </summary>
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

        /// <summary>
        /// Stops/resets the task, ready to be started again if necessary. If the task
        /// is already stopped throws an exception.
        /// </summary>
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
