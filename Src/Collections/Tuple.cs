using System;
using System.Collections.Generic;
using RT.Util.ExtensionMethods;

namespace RT.Util.Collections
{
    /// <summary>
    /// A static class with some tuple-related helper methods.
    /// </summary>
    public static class Tuple
    {
        /// <summary>
        /// Creates and returns a new <see cref="Tuple&lt;T1, T2&gt;"/> containing the provided values.
        /// Use this method instead of the Tuple constructor in order to be able to skip the type parameters.
        /// </summary>
        public static Tuple<T1, T2> New<T1, T2>(T1 element1, T2 element2)
        {
            return new Tuple<T1, T2>(element1, element2);
        }

        /// <summary>
        /// Creates and returns a new <see cref="Tuple&lt;T1, T2, T3&gt;"/> containing the provided values.
        /// Use this method instead of the Tuple constructor in order to be able to skip the type parameters.
        /// </summary>
        public static Tuple<T1, T2, T3> New<T1, T2, T3>(T1 element1, T2 element2, T3 element3)
        {
            return new Tuple<T1, T2, T3>(element1, element2, element3);
        }

        /// <summary>
        /// Creates and returns a new <see cref="Tuple&lt;T1, T2, T3, T4&gt;"/> containing the provided values.
        /// Use this method instead of the Tuple constructor in order to be able to skip the type parameters.
        /// </summary>
        public static Tuple<T1, T2, T3, T4> New<T1, T2, T3, T4>(T1 element1, T2 element2, T3 element3, T4 element4)
        {
            return new Tuple<T1, T2, T3, T4>(element1, element2, element3, element4);
        }
    }

    /// <summary>
    /// A tuple of two values of specified types.
    /// </summary>
    [Serializable]
    public struct Tuple<T1, T2> : IComparable<Tuple<T1, T2>>, IEquatable<Tuple<T1, T2>>
    {
        /// <summary>Default tuple comparer uses the default Comparer and EqualityComparer for each value's type.</summary>
        public static TupleComparer<T1, T2> DefaultComparer
        {
            get
            {
                if (_defaultComparer == null)
                    _defaultComparer = new TupleComparer<T1, T2>();
                return _defaultComparer;
            }
        }
        private static TupleComparer<T1, T2> _defaultComparer = null;

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

        /// <summary>Returns a string representation of this <see cref="Tuple&lt;T1, T2&gt;"/>.</summary>
        public override string ToString()
        {
            return "({0}, {1})".Fmt(E1.ToString(), E2.ToString());
        }

        /// <summary>Compares this tuple to another tuple of the same type using lexicographical ordering w.r.t. the tuple values.</summary>
        public int CompareTo(Tuple<T1, T2> other)
        {
            return DefaultComparer.Compare(this, other);
        }

        /// <summary>Compares this tuple to another tuple of the same type.</summary>
        public bool Equals(Tuple<T1, T2> other)
        {
            return DefaultComparer.Equals(this, other);
        }

        /// <summary>Gets a hash code of this tuple by combining the hash codes of individual values.</summary>
        public override int GetHashCode()
        {
            return DefaultComparer.GetHashCode(this);
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

    /// <summary>
    /// Contains some commonly used tuple comparers.
    /// </summary>
    public static class TupleComparer
    {
        /// <summary>This comparer uses invariant culture case-insensitive string comparer for both values.</summary>
        public static TupleComparer<string, string> StringInvariantCultureIgnoreCase2
        {
            get
            {
                if (_stringInvariantCultureIgnoreCase2 == null)
                {
                    var comparer = StringComparer.InvariantCultureIgnoreCase;
                    _stringInvariantCultureIgnoreCase2 = new TupleComparer<string, string>(comparer, comparer, comparer, comparer);
                }
                return _stringInvariantCultureIgnoreCase2;
            }
        }
        private static TupleComparer<string, string> _stringInvariantCultureIgnoreCase2 = null;
    }

    /// <summary>
    /// Provides methods to compare tuples for equality, order, and for computing hash codes of tuples.
    /// This class can use supplied comparers and/or equality comparers for each tuple value, or it can
    /// use the default ones if a comparer is omitted.
    /// </summary>
    public class TupleComparer<T1, T2> : IComparer<Tuple<T1, T2>>, IEqualityComparer<Tuple<T1, T2>>
    {
        private IComparer<T1> _comp1;
        private IComparer<T2> _comp2;
        private IEqualityComparer<T1> _eqcomp1;
        private IEqualityComparer<T2> _eqcomp2;

        /// <summary>Creates a tuple comparer using default comparers and equality-comparers for the types.</summary>
        public TupleComparer() : this(null, null, null, null) { }

        /// <summary>Creates a tuple comparer using the specified comparers and default equality-comparers.</summary>
        /// <param name="comp1">Comparer for the first value, or null to use default comparer for the type.</param>
        /// <param name="comp2">Comparer for the second value, or null to use default comparer for the type.</param>
        public TupleComparer(IComparer<T1> comp1, IComparer<T2> comp2) : this(comp1, comp2, null, null) { }

        /// <summary>Creates a tuple comparer using the specified equality-comparers and default comparers.</summary>
        /// <param name="eqcomp1">Equality comparer for the first value, or null to use default comparer for the type.</param>
        /// <param name="eqcomp2">Equality comparer for the second value, or null to use default comparer for the type.</param>
        public TupleComparer(IEqualityComparer<T1> eqcomp1, IEqualityComparer<T2> eqcomp2) : this(null, null, eqcomp1, eqcomp2) { }

        /// <summary>Creates a tuple comparer using the specified comparers and equality-comparers.</summary>
        /// <param name="comp1">Comparer for the first value, or null to use default comparer for the type.</param>
        /// <param name="comp2">Comparer for the second value, or null to use default comparer for the type.</param>
        /// <param name="eqcomp1">Equality comparer for the first value, or null to use default comparer for the type.</param>
        /// <param name="eqcomp2">Equality comparer for the second value, or null to use default comparer for the type.</param>
        public TupleComparer(IComparer<T1> comp1, IComparer<T2> comp2, IEqualityComparer<T1> eqcomp1, IEqualityComparer<T2> eqcomp2)
        {
            _comp1 = comp1 == null ? Comparer<T1>.Default : comp1;
            _comp2 = comp2 == null ? Comparer<T2>.Default : comp2;
            _eqcomp1 = eqcomp1 == null ? EqualityComparer<T1>.Default : eqcomp1;
            _eqcomp2 = eqcomp2 == null ? EqualityComparer<T2>.Default : eqcomp2;
        }

        /// <summary>Compares the two tuples lexicographically.</summary>
        public int Compare(Tuple<T1, T2> x, Tuple<T1, T2> y)
        {
            int res;
            res = _comp1.Compare(x.E1, y.E1);
            if (res != 0) return res;
            res = _comp2.Compare(x.E2, y.E2);
            return res;
        }

        /// <summary>Compares the two tuples for equality.</summary>
        public bool Equals(Tuple<T1, T2> x, Tuple<T1, T2> y)
        {
            return _eqcomp1.Equals(x.E1, y.E1) && _eqcomp2.Equals(x.E2, y.E2);
        }

        /// <summary>Computes the hash code of the tuple by combining the hash codes of the individual values.</summary>
        public int GetHashCode(Tuple<T1, T2> obj)
        {
            return _eqcomp1.GetHashCode(obj.E1) + 13 * _eqcomp2.GetHashCode(obj.E2);
        }
    }
}
