using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RT.ParseCs
{
    public class AutoList<T> : List<T>
    {
        public new T this[int index]
        {
            get { return index >= Count ? default(T) : base[index]; }
            set
            {
                while (index >= Count)
                    Add(default(T));
                base[index] = value;
            }
        }
        public AutoList() : base() { }
        public AutoList(int capacity) : base(capacity) { }
        public AutoList(IEnumerable<T> collection) : base(collection) { }
    }
}
