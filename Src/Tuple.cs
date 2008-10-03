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
        /// <summary>The first element in the tuple.</summary>
        public T1 E1;
        /// <summary>The second element in the tuple.</summary>
        public T2 E2;

        /// <summary>Initialises a new two-element tuple.</summary>
        /// <param name="Element1">First element.</param>
        /// <param name="Element2">Second element.</param>
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
        /// <summary>The first element in the tuple.</summary>
        public T1 E1;
        /// <summary>The second element in the tuple.</summary>
        public T2 E2;
        /// <summary>The third element in the tuple.</summary>
        public T3 E3;

        /// <summary>Initialises a new three-element tuple.</summary>
        /// <param name="Element1">First element.</param>
        /// <param name="Element2">Second element.</param>
        /// <param name="Element3">Third element.</param>
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
        /// <summary>The first element in the tuple.</summary>
        public T1 E1;
        /// <summary>The second element in the tuple.</summary>
        public T2 E2;
        /// <summary>The third element in the tuple.</summary>
        public T3 E3;
        /// <summary>The fourth element in the tuple.</summary>
        public T4 E4;

        /// <summary>Initialises a new four-element tuple.</summary>
        /// <param name="Element1">First element.</param>
        /// <param name="Element2">Second element.</param>
        /// <param name="Element3">Third element.</param>
        /// <param name="Element4">Fourth element.</param>
        public Tuple(T1 Element1, T2 Element2, T3 Element3, T4 Element4)
        {
            E1 = Element1;
            E2 = Element2;
            E3 = Element3;
            E4 = Element4;
        }
    }

}
