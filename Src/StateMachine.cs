using System;
using System.Collections.Generic;
using System.Reflection;
using RT.Util.Collections;

namespace RT.Util.FSM
{
    /// <summary>
    /// Represents a finite state machine. Terse overview: each state is represented by a static class
    /// (marked with <see cref="FsmStateAttribute"/> and expected to implement certain methods).
    /// The FSM receives inputs via "events", which occur at a specified point in time
    /// (some time in the future or "ASAP"). Events, when their time comes, get sent to the
    /// class representing the current state. The class determines which state to transition to,
    /// if at all.
    /// 
    /// NOTE: the implementation looks a bit contrived, do not use for new stuff without
    /// thoroughly reviewing this.
    /// </summary>
    public class StateMachine
    {
        /// <summary>
        /// Hide the no-parameter constructor.
        /// </summary>
        private StateMachine() { }

        /// <summary>
        /// Constructs a new State Machine, which is initially in the specified state.
        /// No events are sent to the state by this procedure.
        /// </summary>
        /// <param name="startingState">The initial state of the FSM</param>
        public StateMachine(Type startingState)
        {
            _cur = startingState;
        }

        /// <summary>
        /// The current state that the FSM is in.
        /// </summary>
        private Type _cur = null;

        /// <summary>
        /// Gets the current state of the FSM. Currently there appears to be no need for
        /// the ability to set state directly (i.e. circumventing the transitions), so this
        /// ability is not provided.
        /// </summary>
        public Type State
        {
            get { return _cur; }
        }

        /// <summary>
        /// The "queue" of inputs which haven't yet been processed. They are sent to the
        /// current state in FIFO order.
        /// </summary>
        private ListSorted<RealTimeEvent<object>> _inputs = new ListSorted<RealTimeEvent<object>>();

        /// <summary>
        /// This object is used to synchronize input addition with the input processing
        /// thread.
        /// </summary>
        private object _sendInputLock = new object();

        /// <summary>
        /// This is used to note calls to SendInput made by state transition code. The
        /// SendInputLock is not enough because it does not prevent the same thread from
        /// modifying Inputs while the enumeration is running.
        /// </summary>
        private bool _inputsModified;

        /// <summary>
        /// Sends an input to the state machine. The input is added to the list of
        /// inputs, which is processed separately (by ProcessQueue). This is done to allow
        /// state transition code to send inputs without being immediately deprived of
        /// control flow in favour of the new state.
        /// </summary>
        /// <param name="input">The input to send.</param>
        public void SendInput(object input)
        {
            SendInput(input, TimeSpan.Zero);
        }

        /// <summary>
        /// Same as HaveInput(object), except that the input will be sent after the
        /// specified interval has elapsed.
        /// </summary>
        /// <param name="input">The input received.</param>
        /// <param name="interval">Interval after which the input will be sent.</param>
        public void SendInput(object input, TimeSpan interval)
        {
            lock (_sendInputLock)
            {
                _inputs.Add(new RealTimeEvent<object>(DateTime.Now + interval, input));
                _inputsModified = true;
            }
        }

        /// <summary>
        /// Processes the first N inputs (whose time interval, if any, has elapsed) on the
        /// queue of inputs by sending it to the Transition method of the current state in
        /// FIFO order. Note that the current state may be different by this time than 
        /// what it was when the input was sent.
        /// </summary>
        /// <param name="n">The maximum number of inputs to process. If zero, all inputs
        ///        will be processed but there can be circumstances in which this never
        ///        returns.</param>
        /// <returns>True if there are still inputs left on the queue. Note that this means
        ///        any inputs, not just those which are due. This means that a construct like
        ///        while (ProcessQueue(5)); should not be used.</returns>
        public bool ProcessQueue(int n)
        {
            int pn = 0; // number of items processed - to respect the N parameter
            bool toAddEntered = false;

            // This while loop allows us to continue processing the Inputs after adding
            // new inputs during processing.
            while (pn < n)
            {
                lock (_sendInputLock)
                {
                    // Add the Entered input if necessary
                    if (toAddEntered)
                    {
                        _inputs.Add(new RealTimeEvent<object>(DateTime.MinValue, new FsmEvent_Entered()));
                        toAddEntered = false;
                    }

                    List<int> toRemove = new List<int>(); // inputs to be removed

                    _inputsModified = false;
                    int index = -1;
                    foreach (RealTimeEvent<object> Inp in _inputs)
                    {
                        index++;

                        // Stop if we hit one which isn't due
                        if (Inp.Timestamp > DateTime.Now)
                        {
                            pn = int.MaxValue;
                            break;
                        }

                        // This signal is definitely getting processed, so make sure it'll
                        // get removed once we're done enumerating.
                        toRemove.Add(index);
                        pn++;

                        // Invoke the transition method
                        MethodBase transitionMethod = _cur.GetMethod("Transition");
                        Type newState = (Type) transitionMethod.Invoke(null, new object[] { Inp.Data });
                        // Update the state if a transition was requested
                        if (newState != null)
                        {
                            Fault.AddMT("newstate = " + newState.Name);
                            _cur = newState;
                            // The Entry input must be sent immediately, otherwise it may
                            // get sent to the wrong state. Can't do this from within the
                            // enumeration, so we have to flag and restart.
                            toAddEntered = true;
                            break;
                        }

                        // Restart enumeration if Inputs modified
                        if (_inputsModified)
                            break;
                        // Stop if we've processed N items
                        if (pn >= n)
                            break;
                    }

                    // Remove all processed inputs
                    foreach (int i in toRemove)
                        _inputs.RemoveAt(i);
                }
            }

            // Return true if any inputs remaining
            lock (_sendInputLock)
                return _inputs.Count > 0;
        }
    }

    /// <summary>
    /// Represents a "state entered" event.
    /// </summary>
    public class FsmEvent_Entered
    {
    }

    /// <summary>
    /// Represents a user defined event.
    /// </summary>
    public class FsmEvent_User
    {
        /// <summary>
        /// A user-defined object which the state can use to determine the transition.
        /// </summary>
        private object _userObj = null;
        private string _userStr = null;

        /// <summary>
        /// Creates a new User event setting UserObject to null.
        /// </summary>
        public FsmEvent_User()
        {
            _userObj = null;
        }

        /// <summary>
        /// Creates a new User event, storing the specified object so that it can be
        /// used by the state to determine a transition.
        /// </summary>
        public FsmEvent_User(object userObj)
        {
            _userObj = userObj;
        }

        /// <summary>
        /// Creates a new User event, storing the specified string. This may be more
        /// convenient for simple events than an object.
        /// </summary>
        /// <param name="userStr"></param>
        public FsmEvent_User(string userStr)
        {
            _userStr = userStr;
        }

        /// <summary>
        /// Gets the user object that the Event was created with.
        /// </summary>
        public object UserObject
        {
            get { return _userObj; }
        }

        /// <summary>
        /// Gets the user string that the Event was created with.
        /// </summary>
        public string UserString
        {
            get { return _userStr; }
        }
    }

    /// <summary>
    /// Specifies a particular static class as an FSM state. Such a class is expected
    /// to implement a Transtition method with the following signature:
    /// 
    /// public static Type Transition(object)
    /// 
    /// This method will be invoked whenever the FSM gets an input. The input is passed
    /// to the Transition method of the active state. The return value determines the
    /// new state, which must be null (to remain in the current state) or the Type
    /// of another class marked with this attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class FsmStateAttribute : Attribute
    {
    }

    public class RealTimeEvent<T> : IComparable<DateTime>
    {
        private DateTime _timestamp;
        private T _data;

        public RealTimeEvent(DateTime timestamp, T data)
        {
            _timestamp = timestamp;
            _data = data;
        }

        public DateTime Timestamp { get { return _timestamp; } }

        public T Data { get { return _data; } }

        public int CompareTo(DateTime other)
        {
            return _timestamp.CompareTo(other);
        }
    }
}