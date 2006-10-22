using System;
using System.Collections.Generic;
using System.Text;

namespace RT.Util
{
    [Serializable]
    public class SortedSet<T> : Set<T>
    {
        public void Sort(Comparison<T> comparison)
        {
            L.Sort(comparison);
        }
    }
}
