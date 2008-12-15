using System;
using System.Collections.Generic;
using System.Text;

namespace RT.Util.Collections
{
    /// <summary>
    /// SortedDictionaryDT is a special kind of SortedDictionary which is intended to keep
    /// track of events ordered by time. The only difference from the SortedDictionary is
    /// that an attempt to add an event with the same time as an existing entry does not
    /// fail - instead the next available time is used.
    /// </summary>
    /// <typeparam name="TV"></typeparam>
    public class SortedDictionaryDT<TV> : SortedDictionary<DateTime, TV>
    {
        /// <summary>
        /// Adds an element at the specified time. If an element with that time already
        /// exists, the next available time will be used, effectively adding Value just
        /// after the other items with the same time.
        /// </summary>
        public new void Add(DateTime key, TV value)
        {
            while (ContainsKey(key))
                key.AddTicks(1);
            base.Add(key, value);
        }
    }
}
