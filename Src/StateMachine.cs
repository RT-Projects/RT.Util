using System;
using System.Collections.Generic;
using System.Reflection;
using RT.Util.Collections;

namespace RT.Util.FSM
{
    /// <summary>
    /// Represents a finite state machine. Terse overview: each state is represented by a static class.
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
        /// <param name="StartingState">The initial state of the FSM</param>
        public StateMachine(Type StartingState)
        {
            Cur = StartingState;
        }

        /// <summary>
        /// The current state that the FSM is in.
        /// </summary>
        private Type Cur = null;

        /// <summary>
        /// Gets the current state of the FSM. Currently there appears to be no need for
        /// the ability to set state directly (i.e. circumventing the transitions), so this
        /// ability is not provided.
        /// </summary>
        public Type State
        {
            get { return Cur; }
        }

        /// <summary>
        /// The "queue" of inputs which haven't yet been processed. They are sent to the
        /// current state in FIFO order.
        /// </summary>
        private SortedDictionaryDT<object> Inputs = new SortedDictionaryDT<object>();

        /// <summary>
        /// This object is used to synchronize input addition with the input processing
        /// thread.
        /// </summary>
        private object SendInputLock = new object();

        /// <summary>
        /// This is used to note calls to SendInput made by state transition code. The
        /// SendInputLock is not enough because it does not prevent the same thread from
        /// modifying Inputs while the enumeration is running.
        /// </summary>
        private bool InputsModified;

        /// <summary>
        /// Sends an input to the state machine. The input is added to the list of
        /// inputs, which is processed separately (by ProcessQueue). This is done to allow
        /// state transition code to send inputs without being immediately deprived of
        /// control flow in favour of the new state.
        /// </summary>
        /// <param name="Input">The input to send.</param>
        public void SendInput(object Input)
        {
            SendInput(Input, TimeSpan.Zero);
        }

        /// <summary>
        /// Same as HaveInput(object), except that the input will be sent after the
        /// specified interval has elapsed.
        /// </summary>
        /// <param name="Input">The input received.</param>
        /// <param name="Interval">Interval after which the input will be sent.</param>
        public void SendInput(object Input, TimeSpan Interval)
        {
            lock (SendInputLock)
            {
                Inputs.Add(DateTime.Now + Interval, Input);
                InputsModified = true;
            }
        }

        /// <summary>
        /// Processes the first N inputs (whose time interval, if any, has elapsed) on the
        /// queue of inputs by sending it to the Transition method of the current state in
        /// FIFO order. Note that the current state may be different by this time than 
        /// what it was when the input was sent.
        /// </summary>
        /// <param name="N">The maximum number of inputs to process. If zero, all inputs
        ///        will be processed but there can be circumstances in which this never
        ///        returns.</param>
        /// <returns>True if there are still inputs left on the queue. Note that this means
        ///        any inputs, not just those which are due. This means that a construct like
        ///        while (ProcessQueue(5)); should not be used.</returns>
        public bool ProcessQueue(int N)
        {
            int PN = 0; // number of items processed - to respect the N parameter
            List<DateTime> ToRemove = new List<DateTime>(); // inputs to be removed
            bool ToAddEntered = false;

            // This while loop allows us to continue processing the Inputs after adding
            // new inputs during processing.
            while (PN < N)
            {
                lock (SendInputLock)
                {
                    // Add the Entered input if necessary
                    if (ToAddEntered)
                    {
                        Inputs.Add(DateTime.MinValue, new FsmEvent_Entered());
                        ToAddEntered = false;
                    }

                    InputsModified = false;
                    foreach (KeyValuePair<DateTime, object> Inp in Inputs)
                    {
                        // Stop if we hit one which isn't due
                        if (Inp.Key > DateTime.Now)
                        {
                            PN = int.MaxValue;
                            break;
                        }

                        // This signal is definitely getting processed, so make sure it'll
                        // get removed once we're done enumerating.
                        ToRemove.Add(Inp.Key);
                        PN++;

                        // Invoke the transition method
                        MethodBase TransitionMethod = Cur.GetMethod("Transition");
                        Type newstate = (Type)TransitionMethod.Invoke(null, new object[] { Inp.Value });
                        // Update the state if a transition was requested
                        if (newstate != null)
                        {
                            Fault.AddMT("newstate = " + newstate.Name);
                            Cur = newstate;
                            // The Entry input must be sent immediately, otherwise it may
                            // get sent to the wrong state. Can't do this from within the
                            // enumeration, so we have to flag and restart.
                            ToAddEntered = true;
                            break;
                        }

                        // Restart enumeration if Inputs modified
                        if (InputsModified)
                            break;
                        // Stop if we've processed N items
                        if (PN >= N)
                            break;
                    }

                    // Remove all processed inputs
                    foreach (DateTime dt in ToRemove)
                        Inputs.Remove(dt);
                }
            }

            // Return true if any inputs remaining
            lock (SendInputLock)
                return Inputs.Count > 0;
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
        private object UserObj = null;
        private string UserStr = null;

        /// <summary>
        /// Creates a new User event setting UserObject to null.
        /// </summary>
        public FsmEvent_User()
        {
            UserObj = null;
        }

        /// <summary>
        /// Creates a new User event, storing the specified object so that it can be
        /// used by the state to determine a transition.
        /// </summary>
        public FsmEvent_User(object UserObj)
        {
            this.UserObj = UserObj;
        }

        /// <summary>
        /// Creates a new User event, storing the specified string. This may be more
        /// convenient for simple events than an object.
        /// </summary>
        /// <param name="UserStr"></param>
        public FsmEvent_User(string UserStr)
        {
            this.UserStr = UserStr;
        }

        /// <summary>
        /// Gets the user object that the Event was created with.
        /// </summary>
        public object UserObject
        {
            get { return UserObj; }
        }

        /// <summary>
        /// Gets the user string that the Event was created with.
        /// </summary>
        public string UserString
        {
            get { return UserStr; }
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
    [AttributeUsage(AttributeTargets.Class, AllowMultiple=false)]
    public class FsmStateAttribute : Attribute
    {
    }
}