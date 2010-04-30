
namespace RT.KitchenSink.EventSim
{
    /// <summary>
    /// Represents an Event that will occur at a certain time in the simulation.
    /// Events are maintained by <see cref="EventEngine"/> and the callback gets
    /// invoked whenever the simulation time reaches the time of the event.
    /// </summary>
    public sealed class Event
    {
        /// <summary>
        /// Invoked whenever an event is "due", i.e. whenever the simulation
        /// reaches the time at which the event is supposed to occur.
        /// </summary>
        public EventCallback Callback;

        /// <summary>
        /// User-defined data associated with this event.
        /// </summary>
        public object User1, User2, User3;
    }
}
