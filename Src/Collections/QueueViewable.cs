using System;
using System.Collections;
using System.Collections.Generic;
using RT.Util.ExtensionMethods;

namespace RT.Util.Collections
{
    /// <summary>
    /// A queue whose queued items can be accessed by index. The item at the head of the queue
    /// has index 0 and is the next item to be dequeued.
    /// </summary>
    /// <typeparam name="T">The type of the elements stored in the queue</typeparam>
    public class QueueViewable<T> : IEnumerable<T>, ICollection<T>, IList<T>
    {
        private T[] _data;
        private int _head = 0;
        private int _tail = 0;
        private int _count = 0;

        /// <summary>
        /// Constructor
        /// </summary>
        public QueueViewable()
        {
            _data = new T[8];
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="initialCapacity">An appropriate initial capacity will help
        /// avoid unnecessarily growing the internal buffer.</param>
        public QueueViewable(int initialCapacity)
        {
            _data = new T[initialCapacity];
        }

        /// <summary>
        /// Adds an item at the tail of the queue.
        /// </summary>
        public void Enqueue(T item)
        {
            if (_count == _data.Length)
                growCapacity();
            _data[_tail] = item;
            _count++;
            _tail++;
            if (_tail == _data.Length)
                _tail = 0;
        }

        /// <summary>
        /// Removes and returns the item at the head of the queue.
        /// </summary>
        public T Dequeue()
        {
            if (_count == 0)
                throw new InvalidOperationException("Cannot dequeue item because queue is empty");
            T item = _data[_head];
            _count--;
            _head++;
            if (_head == _data.Length)
                _head = 0;
            return item;
        }

        /// <summary>
        /// Accesses the Nth queued item. The next item to be dequeued always has the index 0.
        /// The existing items can be both read and changed. No new items can be added using this indexer.
        /// </summary>
        public T this[int index]
        {
            get
            {
                if (index >= _count)
                    throw new ArgumentException("Cannot access element at index {0} because only {1} elements are in the queue".Fmt(index, _count));
                return _data[(_head + index) % _data.Length];
            }
            set
            {
                if (index >= _count)
                    throw new ArgumentException("Cannot set element at index {0} because only {1} elements are in the queue. Use Enqueue instead.".Fmt(index, _count));
                _data[(_head + index) % _data.Length] = value;
            }
        }

        /// <summary>
        /// Gets the current capacity of the queue (that is, the maximum number of items it can store before
        /// the internal store needs to be resized).
        /// </summary>
        public int Capacity
        {
            get
            {
                return _data.Length;
            }
        }

        private void growCapacity()
        {
            T[] newdata = new T[_data.Length == 0 ? 8 : _data.Length * 2];
            CopyTo(newdata, 0);
            _head = 0;
            _tail = _count;
            _data = newdata;
        }

        /// <summary>
        /// Copies all elements to an array, in the order in which they would be dequeued.
        /// The destination array must have enough space for all items.
        /// </summary>
        public void CopyTo(T[] array, int arrayIndex)
        {
            if (_head < _tail)
                Array.Copy(_data, _head, array, arrayIndex, _count);
            else if (_count > 0)
            {
                Array.Copy(_data, _head, array, arrayIndex, _data.Length - _head);
                Array.Copy(_data, 0, array, arrayIndex + _data.Length - _head, _tail);
            }
        }

        /// <summary>
        /// Enumerates all items in the queue in the order in which they would be dequeued.
        /// </summary>
        public IEnumerator<T> GetEnumerator()
        {
            int ptr = _head;
            for (int i = 0; i < _count; i++)
            {
                yield return _data[ptr];
                ptr++;
                if (ptr == _data.Length)
                    ptr = 0;
            }
        }

        /// <summary>
        /// Enumerates all items in the queue in the order in which they would be dequeued.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            int ptr = _head;
            for (int i = 0; i < _count; i++)
            {
                yield return _data[ptr];
                ptr++;
                if (ptr == _data.Length)
                    ptr = 0;
            }
        }

        /// <summary>
        /// Identical to <see cref="Enqueue"/>. Use Enqueue instead of this method.
        /// </summary>
        public void Add(T item)
        {
            Enqueue(item);
        }

        /// <summary>
        /// Empties the queue.
        /// </summary>
        public void Clear()
        {
            _head = _tail = _count = 0;
        }

        /// <summary>
        /// Returns the number of elements in the queue.
        /// </summary>
        public int Count
        {
            get { return _count; }
        }

        /// <summary>
        /// Returns "false".
        /// </summary>
        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>Not implemented.</summary>
        public int IndexOf(T item)
        {
            throw new NotImplementedException();
        }

        /// <summary>Not implemented.</summary>
        public bool Contains(T item)
        {
            throw new NotImplementedException();
        }

        /// <summary>Not supported by <see cref="QueueViewable&lt;T&gt;"/></summary>
        public bool Remove(T item)
        {
            throw new InvalidOperationException("Queue does not support removal of arbitrary items. Use Dequeue instead.");
        }

        /// <summary>Not supported by <see cref="QueueViewable&lt;T&gt;"/></summary>
        public void Insert(int index, T item)
        {
            throw new InvalidOperationException("Queue does not support insertion of items at arbitrary positions. Use Enqueue/Dequeue instead.");
        }

        /// <summary>Not supported by <see cref="QueueViewable&lt;T&gt;"/></summary>
        public void RemoveAt(int index)
        {
            throw new InvalidOperationException("Queue does not support removal of items at arbitrary positions. Use Enqueue/Dequeue instead.");
        }

    }
}
