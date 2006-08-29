/// Set.cs  -  defines a Set class and related classes

using System;
using System.Collections.Generic;
using System.Text;

namespace RT.Util
{
    /// <summary>
    /// A set of objects is a collection of objects where all objects are
    /// distinct (i.e. there exist no multiple instances of an object), and the
    /// order of the elements is undefined.
    /// 
    /// Note that complex types should implement IEquatable, otherwise they are
    /// likely to be tested for equality by reference.
    /// </summary>
    /// <typeparam name="T">The type of the items to be stored.</typeparam>
    [Serializable]
    public class Set<T> : IEnumerable<T>, ICollection<T>, ICloneable, IEquatable<Set<T>>
    {
        /// <summary>
        /// This is where the set is stored internally.
        /// </summary>
        protected List<T> L = new List<T>();

        /// <summary>
        /// Set[item] returns the specified item from the set. Returns null if
        /// the item is not in the set. This may be necessary in case 'item' is
        /// not the object in the set, but is equal to an object in the set.
        /// </summary>
        public T this[T item]
        {
            get
            {
                foreach (T n in L)
                    if (item.Equals(n))
                        return n;
                return default(T);
            }
        }

        /// <summary>
        /// Unions this set with the specified set, storing the result in this
        /// set.
        /// </summary>
        public void Union(Set<T> set)
        {
            foreach (T item in set)
                Add(item);
        }

        /// <summary>
        /// Intersects this set with the specified set, storing the result in
        /// this set.
        /// </summary>
        public void Intersect(Set<T> set)
        {
            foreach (T item in L)
                if (!set.L.Contains(item))
                    L.Remove(item);
        }

        /// <summary>
        /// Subtracts the specified set from this set, removing the items in
        /// <code>set</code> from this set.
        /// </summary>
        public void Subtract(Set<T> set)
        {
            foreach (T item in set)
                Remove(item);
        }

        /// <summary>
        /// Returns a new set which represents the union of this set and the
        /// specified set.
        /// </summary>
        public Set<T> GetUnion(Set<T> set)
        {
            Set<T> res = (Set<T>)Clone();
            res.Union(set);
            return res;
        }

        /// <summary>
        /// Returns a new set which represents the intersection of this set and
        /// the specified set.
        /// </summary>
        public Set<T> GetIntersect(Set<T> set)
        {
            Set<T> res = (Set<T>)Clone();
            res.Intersect(set);
            return res;
        }

        /// <summary>
        /// Returns a new set which contains this set minus the specified set.
        /// </summary>
        public Set<T> GetSubtract(Set<T> set)
        {
            Set<T> res = (Set<T>)Clone();
            res.Subtract(set);
            return res;
        }

        /// <summary>
        /// Returns true if the set is the empty set
        /// </summary>
        public bool IsEmpty
        {
            get { return L.Count == 0; }
        }


        #region IEquatable<Set<T>> Members

        public bool Equals(Set<T> other)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion

        #region ICollection<T> Members

        public void Add(T item)
        {
            if (!L.Contains(item))
                L.Add(item);
        }

        public void Clear()
        {
            L.Clear();
        }

        public bool Contains(T item)
        {
            return L.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            L.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return L.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(T item)
        {
            return L.Remove(item);
        }

        #endregion

        #region IEnumerable<T> Members

        public IEnumerator<T> GetEnumerator()
        {
            return L.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return L.GetEnumerator();
        }

        #endregion

        #region ICloneable Members

        public object Clone()
        {
            Set<T> set = new Set<T>();
            set.L.AddRange(this.L);
            return set;
        }

        #endregion
    }


    [Serializable]
    public class SortedSet<T> : Set<T>
    {
        public void Sort(Comparison<T> comparison)
        {
            L.Sort(comparison);
        }
    }
}
