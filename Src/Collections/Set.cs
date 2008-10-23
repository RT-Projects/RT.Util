using System;
using System.Collections.Generic;
using System.Text;

namespace RT.Util.Collections
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

        /// <summary>Compares the current Set against the specified Set for equality.</summary>
        /// <param name="other">The other Set to compare this set against.</param>
        /// <returns>True if the Sets are equal.</returns>
        public bool Equals(Set<T> other)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion

        #region ICollection<T> Members

        /// <summary>Adds the specified item only if it is not already in this Set.</summary>
        /// <param name="item">Item to add if it is unique.</param>
        public void Add(T item)
        {
            if (!L.Contains(item))
                L.Add(item);
        }

        /// <summary>Empties the Set.</summary>
        public void Clear()
        {
            L.Clear();
        }

        /// <summary>Determined whether the specified item is in the current Set.</summary>
        /// <param name="item">Item to determine membership in this Set for.</param>
        /// <returns>True if the current Set contains the specified item.</returns>
        public bool Contains(T item)
        {
            return L.Contains(item);
        }

        /// <summary>Copies the contents of the current Set to the specified Array.</summary>
        /// <param name="array">Destination Array to copy to.</param>
        /// <param name="arrayIndex">Index in array at which copying begins.</param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            L.CopyTo(array, arrayIndex);
        }

        /// <summary>Determines the number of elements in this set.</summary>
        public int Count
        {
            get { return L.Count; }
        }

        /// <summary>Returns false.</summary>
        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>Removes the specified item if it is contained in the current Set.</summary>
        /// <param name="item">Item to remove.</param>
        /// <returns>True if the item was contained in this Set.</returns>
        public bool Remove(T item)
        {
            return L.Remove(item);
        }

        #endregion

        #region IEnumerable<T> Members

        /// <summary>Returns an enumerator to iterate over the elements in this Set.</summary>
        /// <returns>An IEnumerator&lt;T&gt; to iterate over the elements in this Set.</returns>
        public IEnumerator<T> GetEnumerator()
        {
            return L.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        /// <summary>Returns an enumerator to iterate over the elements in this Set.</summary>
        /// <returns>An IEnumerator to iterate over the elements in this Set.</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return L.GetEnumerator();
        }

        #endregion

        #region ICloneable Members

        /// <summary>Clones this Set (creates another copy).</summary>
        /// <returns>A clone (copy) of the current Set.</returns>
        public object Clone()
        {
            Set<T> set = new Set<T>();
            set.L.AddRange(this.L);
            return set;
        }

        #endregion

        /// <summary>Sorts the current <see cref="Set&lt;T&gt;"/>.</summary>
        /// <param name="comparison">Comparison to use when sorting.</param>
        public void Sort(Comparison<T> comparison)
        {
            L.Sort(comparison);
        }
    }
}
