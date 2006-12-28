using System;
using System.Collections.Generic;
using System.Text;

namespace RT.Util
{
    /// <summary>
    /// A tuple of two values of specified types.
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

    /// <summary>
    /// A tuple of three values of specified types.
    /// </summary>
    [Serializable]
    public struct Tuple<T1, T2, T3>
    {
        public T1 E1;
        public T2 E2;
        public T3 E3;

        public Tuple(T1 Element1, T2 Element2, T3 Element3)
        {
            E1 = Element1;
            E2 = Element2;
            E3 = Element3;
        }
    }

    /// <summary>
    /// A tuple of four values of specified types.
    /// </summary>
    [Serializable]
    public struct Tuple<T1, T2, T3, T4>
    {
        public T1 E1;
        public T2 E2;
        public T3 E3;
        public T4 E4;

        public Tuple(T1 Element1, T2 Element2, T3 Element3, T4 Element4)
        {
            E1 = Element1;
            E2 = Element2;
            E3 = Element3;
            E4 = Element4;
        }
    }

}
