using System;
using System.Collections.Generic;
using System.Text;

namespace RT.Util.EventSim
{
    public class Event
    {
        public EventCallback Callback;
        public object User1, User2, User3;

        public Event()
        {
        }
    }
}
