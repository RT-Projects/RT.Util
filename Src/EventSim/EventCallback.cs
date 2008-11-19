
namespace RT.Util.EventSim
{
    /// <summary>
    /// Invoked whenever an event is "due", i.e. whenever the simulation
    /// reaches the time at which the event is supposed to occur.
    /// </summary>
    public delegate void EventCallback(Event Event, EventEngine Engine);
}
