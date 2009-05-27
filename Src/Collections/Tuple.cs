using System;
using System.Collections.Generic;
using System.Text;
using RT.Util.ExtensionMethods;

namespace RT.Util.Collections
{
    /// <summary>
    /// A tuple of two values of specified types.
    /// </summary>
    [Serializable]
    public struct Tuple<T1, T2> : IComparable<Tuple<T1, T2>>
    {
        /// <summary>The first element in the tuple.</summary>
        public T1 E1;
        /// <summary>The second element in the tuple.</summary>
        public T2 E2;

        /// <summary>Initialises a new two-element tuple.</summary>
        /// <param name="element1">First element.</param>
        /// <param name="element2">Second element.</param>
        public Tuple(T1 element1, T2 element2)
        {
            E1 = element1;
            E2 = element2;
        }

        /// <summary>
        /// Returns a string representation of the current <see cref="Tuple&lt;T1, T2&gt;"/>.
        /// </summary>
        public override string ToString()
        {
            return "({0}, {1})".Fmt(E1.ToString(), E2.ToString());
        }

        /// <summary>
        /// Compares this tuple to another tuple of the same type.
        /// </summary>
        public int CompareTo(Tuple<T1, T2> other)
        {
            int res;

            if (E1 is IComparable<T1>) res = (E1 as IComparable<T1>).CompareTo(other.E1);
            else if (E1 is IComparable) res = (E1 as IComparable).CompareTo(other.E1);
            else throw new RTException("Cannot compare Tuple because type T1 ({0}) does not implement IComparable or IComparable<T1>".Fmt(typeof(T1)));

            if (res != 0) return res;

            if (E2 is IComparable<T2>) res = (E2 as IComparable<T2>).CompareTo(other.E2);
            else if (E2 is IComparable) res = (E2 as IComparable).CompareTo(other.E2);
            else throw new RTException("Cannot compare Tuple because type T2 ({0}) does not implement IComparable or IComparable<T2>".Fmt(typeof(T2)));

            return res;
        }
    }

    /// <summary>
    /// A tuple of three values of specified types.
    /// </summary>
    [Serializable]
    public struct Tuple<T1, T2, T3> : IComparable<Tuple<T1, T2, T3>>
    {
        /// <summary>The first element in the tuple.</summary>
        public T1 E1;
        /// <summary>The second element in the tuple.</summary>
        public T2 E2;
        /// <summary>The third element in the tuple.</summary>
        public T3 E3;

        /// <summary>Initialises a new three-element tuple.</summary>
        /// <param name="element1">First element.</param>
        /// <param name="element2">Second element.</param>
        /// <param name="element3">Third element.</param>
        public Tuple(T1 element1, T2 element2, T3 element3)
        {
            E1 = element1;
            E2 = element2;
            E3 = element3;
        }

        /// <summary>
        /// Returns a string representation of the current <see cref="Tuple&lt;T1, T2, T3&gt;"/>.
        /// </summary>
        public override string ToString()
        {
            return "({0}, {1}, {2})".Fmt(E1.ToString(), E2.ToString(), E3.ToString());
        }

        /// <summary>
        /// Compares this tuple to another tuple of the same type.
        /// </summary>
        public int CompareTo(Tuple<T1, T2, T3> other)
        {
            int res;

            if (E1 is IComparable<T1>) res = (E1 as IComparable<T1>).CompareTo(other.E1);
            else if (E1 is IComparable) res = (E1 as IComparable).CompareTo(other.E1);
            else throw new RTException("Cannot compare Tuple because type T1 ({0}) does not implement IComparable or IComparable<T1>".Fmt(typeof(T1)));

            if (res != 0) return res;

            if (E2 is IComparable<T2>) res = (E2 as IComparable<T2>).CompareTo(other.E2);
            else if (E2 is IComparable) res = (E2 as IComparable).CompareTo(other.E2);
            else throw new RTException("Cannot compare Tuple because type T2 ({0}) does not implement IComparable or IComparable<T2>".Fmt(typeof(T2)));

            if (res != 0) return res;

            if (E3 is IComparable<T3>) res = (E3 as IComparable<T3>).CompareTo(other.E3);
            else if (E3 is IComparable) res = (E3 as IComparable).CompareTo(other.E3);
            else throw new RTException("Cannot compare Tuple because type T3 ({0}) does not implement IComparable or IComparable<T3>".Fmt(typeof(T3)));

            return res;
        }
    }

    /// <summary>
    /// A tuple of four values of specified types.
    /// </summary>
    [Serializable]
    public struct Tuple<T1, T2, T3, T4> : IComparable<Tuple<T1, T2, T3, T4>>
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
        /// <param name="element1">First element.</param>
        /// <param name="element2">Second element.</param>
        /// <param name="element3">Third element.</param>
        /// <param name="element4">Fourth element.</param>
        public Tuple(T1 element1, T2 element2, T3 element3, T4 element4)
        {
            E1 = element1;
            E2 = element2;
            E3 = element3;
            E4 = element4;
        }

        /// <summary>
        /// Returns a string representation of the current <see cref="Tuple&lt;T1, T2, T3, T4&gt;"/>.
        /// </summary>
        public override string ToString()
        {
            return "({0}, {1}, {2}, {3})".Fmt(E1.ToString(), E2.ToString(), E3.ToString(), E4.ToString());
        }

        /// <summary>
        /// Compares this tuple to another tuple of the same type.
        /// </summary>
        public int CompareTo(Tuple<T1, T2, T3, T4> other)
        {
            int res;

            if (E1 is IComparable<T1>) res = (E1 as IComparable<T1>).CompareTo(other.E1);
            else if (E1 is IComparable) res = (E1 as IComparable).CompareTo(other.E1);
            else throw new RTException("Cannot compare Tuple because type T1 ({0}) does not implement IComparable or IComparable<T1>".Fmt(typeof(T1)));

            if (res != 0) return res;

            if (E2 is IComparable<T2>) res = (E2 as IComparable<T2>).CompareTo(other.E2);
            else if (E2 is IComparable) res = (E2 as IComparable).CompareTo(other.E2);
            else throw new RTException("Cannot compare Tuple because type T2 ({0}) does not implement IComparable or IComparable<T2>".Fmt(typeof(T2)));

            if (res != 0) return res;

            if (E3 is IComparable<T3>) res = (E3 as IComparable<T3>).CompareTo(other.E3);
            else if (E3 is IComparable) res = (E3 as IComparable).CompareTo(other.E3);
            else throw new RTException("Cannot compare Tuple because type T3 ({0}) does not implement IComparable or IComparable<T3>".Fmt(typeof(T3)));

            if (res != 0) return res;

            if (E4 is IComparable<T4>) res = (E4 as IComparable<T4>).CompareTo(other.E4);
            else if (E4 is IComparable) res = (E4 as IComparable).CompareTo(other.E4);
            else throw new RTException("Cannot compare Tuple because type T4 ({0}) does not implement IComparable or IComparable<T4>".Fmt(typeof(T4)));

            return res;
        }
    }
}
