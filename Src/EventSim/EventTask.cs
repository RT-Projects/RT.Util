using System;
using System.Collections.Generic;

namespace R.Util.EventSim
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
        private IEnumerator<double> _proc;
        private Event _event;
        private EventEngine _engine;

        private void Callback(Event callback, EventEngine engine)
        {
            if (_proc == null)
                throw new Exception("Callback received for stopped process");

            if (!_proc.MoveNext())
            {
                _proc = null;
                _event = null;
                return;
            }

            // Create an event for this delay
            // We can reuse the event we've just received. We also know that it's
            // the same one as stored in LastEvent.
            engine.AddEvent(callback, engine.Time + _proc.Current);
        }

        /// <summary>
        /// Override to implement the task that needs to be simulated.
        /// </summary>
        public abstract IEnumerator<double> Process();

        /// <summary>
        /// Starts the task running in the specified <see cref="EventEngine"/>.
        /// </summary>
        public void Start(EventEngine engine)
        {
            if (_proc != null)
                throw new Exception("Cannot start task because it is already running");

            _proc = Process();
            if (!_proc.MoveNext())
            {
                _proc = null;
                return;
            }

            _engine = engine;

            // Create a new event to be used for advancing the process
            _event = new Event();
            _event.Callback = new EventCallback(Callback);
            _event.User1 = this;
            _event.User2 = engine;
            // Schedule it with the first delay returned by the process code
            engine.AddEvent(_event, engine.Time + _proc.Current);
        }

        /// <summary>
        /// Stops/resets the task, ready to be started again if necessary. If the task
        /// is already stopped throws an exception.
        /// </summary>
        public void StopReset()
        {
            if (_proc == null)
                throw new Exception("Cannot stop task because it is not running");

            _engine.CancelEvent(_event);

            _proc = null;
            _event = null;
            _engine = null;
        }
    }
}
