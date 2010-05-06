using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RT.Util.Collections
{
    public class CustomComparer<T> : IComparer<T>
    {
        Comparison<T> _comparison;
        public CustomComparer(Comparison<T> comparison) { _comparison = comparison; }
        public int Compare(T x, T y) { return _comparison(x, y); }
    }

    public static class CustomComparer
    {
        public static CustomComparer<T> Create<T>(Comparison<T> comparison)
        {
            return new CustomComparer<T>(comparison);
        }
    }
}
