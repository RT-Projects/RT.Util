using System;
using System.Collections.Generic;
using System.Linq;
using RT.Util.ExtensionMethods;

namespace RT.Util.Collections
{
    /// <summary>
    /// Represents the result of a partial comparison.
    /// </summary>
    public enum PartialComparisonResult
    {
        /// <summary>The items cannot be compared.</summary>
        NA,
        /// <summary>The first item is greater than the second item.</summary>
        Greater,
        /// <summary>The first item is less than the second item.</summary>
        Less,
        /// <summary>The items are equal to each other.</summary>
        Equal,
    }

    /// <summary>
    /// Implemented by items which support partial comparison.
    /// </summary>
    public interface IPartialComparable<T>
    {
        /// <summary>
        /// Compares this item to the other item.
        /// </summary>
        PartialComparisonResult PartialCompareTo(T other);
    }

    /// <summary>
    /// Represents a node in the poset graph. A node represents a single equivalence class,
    /// and maintains two sets of links to nearby nodes - specifically, those representing
    /// the just-larger and the just-smaller equivalence classes.
    /// </summary>
    /// <typeparam name="T">The type of the elements to be stored. Must implement <see cref="IPartialComparable&lt;T&gt;"/></typeparam>
    public class PosetNode<T> : IPartialComparable<PosetNode<T>>, IPartialComparable<T> where T : IPartialComparable<T>
    {
        private List<T> _elements;
        /// <summary>Stores a set of all nodes which are just-larger than this one. DO NOT CHANGE!</summary>
        internal Set<PosetNode<T>> _largers = new Set<PosetNode<T>>();
        /// <summary>Stores a set of all nodes which are just-smaller than this one. DO NOT CHANGE!</summary>
        internal Set<PosetNode<T>> _smallers = new Set<PosetNode<T>>();

        /// <summary>Gets a read-only collection of all nodes which are just-larger than this one.</summary>
        public ReadOnlyCollection<PosetNode<T>> Largers { get { return _largers.AsReadOnly(); } }
        /// <summary>Gets a read-only collection of all nodes which are just-smaller than this one.</summary>
        public ReadOnlyCollection<PosetNode<T>> Smallers { get { return _smallers.AsReadOnly(); } }

        /// <summary>
        /// Creates a new node and initialises it with the single element. A node is only
        /// permitted to exist if it contains at least one element.
        /// </summary>
        /// <param name="element">An element to store in this node. This determines the equivalence
        /// class, and it will not be possible to add further elements not equal to this one.</param>
        /// <param name="elements">Any additional elements to add to the node.</param>
        public PosetNode(T element, params T[] elements)
        {
            _elements = new List<T>();
            _elements.Add(element);
            foreach (var el in elements)
                AddElement(el);
        }

        /// <summary>
        /// Adds the specified element to this node. The element must be from the same equivalence
        /// class as those elements already in the node, otherwise <see cref="ArgumentException"/> is thrown.
        /// </summary>
        public void AddElement(T element)
        {
            if (element.PartialCompareTo(_elements[0]) != PartialComparisonResult.Equal)
                throw new ArgumentException("Cannot add an element to a PosetNode which is not equal to the elements already in it.");
            _elements.Add(element);
        }

        /// <summary>
        /// Exposes a read-only collection of the elements comprising this equivalence class.
        /// To add more elements, use <see cref="AddElement"/>
        /// </summary>
        public System.Collections.ObjectModel.ReadOnlyCollection<T> Elements
        {
            get { return _elements.AsReadOnly(); }
        }

        /// <summary>
        /// Returns a string enumerating all the elements stored in the node, separated by a comma.
        /// </summary>
        public override string ToString()
        {
            return _elements.Select(el => el.ToString()).JoinString(", ");
        }

        /// <summary>
        /// Compares this node to the other node, using the partial comparison implemented
        /// by the elements stored in the two nodes.
        /// </summary>
        public PartialComparisonResult PartialCompareTo(PosetNode<T> other)
        {
            return this._elements[0].PartialCompareTo(other._elements[0]);
        }

        /// <summary>
        /// Compares this node to the specified element, using the partial comparison implemented
        /// by the elements stored in the two nodes.
        /// </summary>
        public PartialComparisonResult PartialCompareTo(T other)
        {
            return this._elements[0].PartialCompareTo(other);
        }
    }

    /// <summary>
    /// Maintains a poset of all partially comparable elements added to this collection.
    /// Two DAGs are maintained - one starting from all the minimal elements towards the largest elements,
    /// and the other starting from the maximal elements.
    /// </summary>
    /// <typeparam name="T">The type of the elements to be stored. Must implement <see cref="IPartialComparable&lt;T&gt;"/></typeparam>
    public class Poset<T> where T : IPartialComparable<T>
    {
        private Set<PosetNode<T>> _minimals = new Set<PosetNode<T>>();
        private Set<PosetNode<T>> _maximals = new Set<PosetNode<T>>();

        /// <summary>
        /// Gets the set of all minimal elements in the poset.
        /// </summary>
        public ReadOnlyCollection<PosetNode<T>> Minimals { get { return _minimals.AsReadOnly(); } }

        /// <summary>
        /// Gets the set of all maximal elements in the poset.
        /// </summary>
        public ReadOnlyCollection<PosetNode<T>> Maximals { get { return _maximals.AsReadOnly(); } }

        /// <summary>
        /// Adds an element to the poset. If available, the element will be added to an existing
        /// equivalence class (node), otherwise a new one will be created, and the DAGs will be
        /// updated as appropriate.
        /// </summary>
        public void Add(T element)
        {
            PosetNode<T> node;

            node = FindEqual(element);
            if (node != null)
            {
                node.AddElement(element);
                return;
            }

            node = new PosetNode<T>(element);
            add(true, node, _minimals, null);
            add(false, node, _maximals, null);
        }

        private void add(bool upwards, PosetNode<T> toadd, Set<PosetNode<T>> links, PosetNode<T> linkfrom)
        {
            bool any = false;
            List<PosetNode<T>> links_add = null;
            List<PosetNode<T>> links_del = null;
            foreach (var linkto in links)
            {
                var partcmp = toadd.PartialCompareTo(linkto);
                if (partcmp == PartialComparisonResult.Equal)
                {
                    any = true;
                    if (!object.ReferenceEquals(linkto, toadd))
                        throw new InternalError("Equal poset nodes are not the same instance");
                }
                else if ((upwards && partcmp == PartialComparisonResult.Greater) || (!upwards && partcmp == PartialComparisonResult.Less))
                {
                    any = true;
                    add(upwards, toadd, upwards ? linkto._largers : linkto._smallers, linkto);
                }
                else if ((upwards && partcmp == PartialComparisonResult.Less) || (!upwards && partcmp == PartialComparisonResult.Greater))
                {
                    any = true;
                    if (links_add == null) links_add = new List<PosetNode<T>>();
                    links_add.Add(toadd);
                    if (links_del == null) links_del = new List<PosetNode<T>>();
                    links_del.Add(linkto);
                    if (linkfrom != null)
                        (upwards ? toadd._smallers : toadd._largers).Add(linkfrom);
                    (upwards ? toadd._largers : toadd._smallers).Add(linkto);
                    (upwards ? linkto._smallers : linkto._largers).Add(toadd);
                }
            }
            if (links_add != null) foreach (var link in links_add) links.Add(link);
            if (links_del != null) foreach (var link in links_del) links.Remove(link);
            if (!any)
            {
                links.Add(toadd);
                if (linkfrom != null)
                    (upwards ? toadd._smallers : toadd._largers).Add(linkfrom);
            }
        }

        /// <summary>
        /// Finds the node containing elements from the same equivalence class as <paramref name="element"/>.
        /// Returns null if none are found.
        /// </summary>
        public PosetNode<T> FindEqual(T element)
        {
            // Search from minimals upwards. A search from maximals downwards will succeed iff this one does.
            Queue<PosetNode<T>> tocheck = new Queue<PosetNode<T>>(_minimals);
            while (tocheck.Count > 0)
            {
                var node = tocheck.Dequeue();
                switch (node.PartialCompareTo(element))
                {
                    case PartialComparisonResult.Equal: return node;
                    case PartialComparisonResult.Greater: return null; // can't be found in the largers of this node
                    case PartialComparisonResult.Less: foreach (var n in node._largers) tocheck.Enqueue(n); break;
                }
            }
            return null;
        }

        #region Consistency tests

        private PosetNode<T> findEqualDownwards(T element)
        {
            // Search from maximals downwards.
            Queue<PosetNode<T>> tocheck = new Queue<PosetNode<T>>(_maximals);
            while (tocheck.Count > 0)
            {
                var node = tocheck.Dequeue();
                switch (node.PartialCompareTo(element))
                {
                    case PartialComparisonResult.Equal: return node;
                    case PartialComparisonResult.Less: return null;
                    case PartialComparisonResult.Greater: foreach (var n in node._smallers) tocheck.Enqueue(n); break;
                }
            }
            return null;
        }

        /// <summary>
        /// Performs a bunch of tests to make sure the DAGs are in a consistent state. Very slow;
        /// only use when debugging and suspecting a bug in Poset.
        /// </summary>
        public void VerifyConsistency()
        {
            checkLinksTowardsMax(null, _minimals);
            checkLinksTowardsMin(null, _maximals);
            if (_minimals.Any(n => n._smallers.Count != 0))
                throw new InternalError("Minimals have smaller links");
            if (_maximals.Any(n => n._largers.Count != 0))
                throw new InternalError("Maximals have larger links");
        }

        private void checkLinksTowardsMax(PosetNode<T> node, Set<PosetNode<T>> links)
        {
            if (node != null)
            {
                if (FindEqual(node.Elements[0]) == null) throw new InternalError("Not findable!");
                if (findEqualDownwards(node.Elements[0]) == null) throw new InternalError("Not findable!");
                foreach (var l in links)
                    if (!l._smallers.Contains(node))
                        throw new InternalError("Links inconsistent");
            }

            foreach (var pair in links.UniquePairs())
                if (pair.E1 == pair.E2)
                    throw new InternalError("Duplicate link");
            foreach (var l in links)
                checkLinksTowardsMax(l, l._largers);
        }

        private void checkLinksTowardsMin(PosetNode<T> node, Set<PosetNode<T>> links)
        {
            if (node != null)
            {
                if (FindEqual(node.Elements[0]) == null) throw new InternalError("Not findable!");
                if (findEqualDownwards(node.Elements[0]) == null) throw new InternalError("Not findable!");
                foreach (var l in links)
                    if (!l._largers.Contains(node))
                        throw new InternalError("Links inconsistent");
            }

            foreach (var pair in links.UniquePairs())
                if (pair.E1 == pair.E2)
                    throw new InternalError("Duplicate link");
            foreach (var l in links)
                checkLinksTowardsMin(l, l._smallers);
        }

        #endregion
    }

}
