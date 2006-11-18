using System;
using System.Collections.Generic;
using System.Text;

namespace RT.Util
{
    /// <summary>
    /// A pair of two values of specified types.
    /// </summary>
    [Serializable]
    public struct Tuple<T1, T2>
    {
        public T1 E1;
        public T2 E2;

        public Tuple(T1 Element1, T2 Element2)
        {
            E1 = Element1;
            E2 = Element2;
        }
    }

}
