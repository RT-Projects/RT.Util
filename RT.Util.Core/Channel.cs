using System.Collections;

namespace RT.KitchenSink;

/// <summary>
///     Provides a synchronized channel for communication between concurrent threads.</summary>
/// <typeparam name="T">
///     The type of message exchanged between threads.</typeparam>
/// <remarks>
///     <para>
///         The design of this class is based on the concept of channels in the Go programming language:</para>
///     <list type="bullet">
///         <item><description>
///             The basic principle is to have one thread “write” (enqueue) items and another “read” (dequeue) them. The
///             writing thread can signal the end of the channel by closing it.</description></item>
///         <item><description>
///             Any number of threads can write to a channel. However, a master thread must be responsible for closing the
///             channel; if nobody closes it, reads will block indefinitely, and if a thread attempts to write after another
///             thread closed the channel, an exception occurs.</description></item>
///         <item><description>
///             Any number of threads can read from a channel. If no item is waiting in the queue, the reading thread blocks
///             until another thread either writes an item or closes the channel.</description></item>
///         <item><description>
///             A closed channel can still be read from until all the enqueued elements are exhausted, at which point reading
///             from the closed channel throws. The <see cref="TryRead"/> method can be used to avoid this exception. Note
///             that accessing <see cref="HasMore"/> is not enough as another reading thread can dequeue an item at any time.</description></item>
///         <item><description>
///             The easiest way to read from a channel safely is to iterate over it using a <c>foreach</c> loop. Multiple
///             threads can use such a loop simultaneously; doing so will “spread” the items across the threads. The
///             <c>foreach</c> loops all end when the channel is closed and all elements are exhausted.</description></item></list></remarks>
public sealed class Channel<T> : IEnumerable<T>, IDisposable
{
    private Queue<T> _queue = new Queue<T>();
    private bool _closed = false;

    /// <summary>
    ///     Writes an element to the channel.</summary>
    /// <remarks>
    ///     If there are threads waiting to read an element, one is resumed and receives this element.</remarks>
    public void Write(T element)
    {
        lock (_queue)
        {
            if (_closed)
                throw new InvalidOperationException("The channel has already been closed.");
            _queue.Enqueue(element);
            Monitor.PulseAll(_queue);
        }
    }

    /// <summary>
    ///     Reads an element from the channel. If no element is waiting to be read, blocks until an element is received or the
    ///     channel is closed.</summary>
    /// <returns>
    ///     The element read from the channel.</returns>
    /// <remarks>
    ///     If multiple threads call this method to wait for elements, each element is received by only one thread.</remarks>
    /// <exception cref="InvalidOperationException">
    ///     The end of the channel has been reached. There are no more elements waiting.</exception>
    public T Read()
    {
        lock (_queue)
        {
            while (!_closed && _queue.Count == 0)
                Monitor.Wait(_queue);
            if (_queue.Count > 0)
                return _queue.Dequeue();
            throw new InvalidOperationException("The channel has been closed and there are no more elements waiting.");
        }
    }

    /// <summary>
    ///     Determines whether an element can be read from the channel and if so, reads it. If no element is waiting to be
    ///     read, blocks until an element is received or the channel is closed.</summary>
    /// <param name="result">
    ///     Receives the element read from the channel (or <c>default(T)</c> if the end of the channel is reached).</param>
    /// <returns>
    ///     <c>true</c> if an element has been read from the channel; <c>false</c> if the end of the channel is reached.</returns>
    /// <remarks>
    ///     If multiple threads call this method to wait for elements, each element is received by only one thread.</remarks>
    public bool TryRead(out T result)
    {
        lock (_queue)
        {
            if (HasMore)
            {
                result = Read();
                return true;
            }
            else
            {
                result = default(T);
                return false;
            }
        }
    }

    /// <summary>
    ///     Signals the end of the channel. Threads can still read elements waiting in the channel, but no further elements
    ///     can be written to it.</summary>
    public void Close()
    {
        lock (_queue)
        {
            _closed = true;
            Monitor.PulseAll(_queue);
        }
    }
    void IDisposable.Dispose() { Close(); }

    /// <summary>
    ///     Determines whether the channel contains more items.</summary>
    /// <remarks>
    ///     <para>
    ///         Blocks until the determination can be made.</para>
    ///     <para>
    ///         In most cases, this method is only useful if there is only one reading thread. If there are multiple, then
    ///         even after this method returns <c>true</c>, another thread can consume the item before this thread can get to
    ///         it. Only if this method returns <c>false</c> can there be no such race condition.</para></remarks>
    public bool HasMore
    {
        get
        {
            lock (_queue)
            {
                while (!_closed && _queue.Count == 0)
                    Monitor.Wait(_queue);
                return !_closed || _queue.Count > 0;
            }
        }
    }

    /// <summary>
    ///     Returns an enumerator that allows safe reading from this channel. Multiple threads can call this to iterate over
    ///     the channel; the items from the channel are spread across those threads.</summary>
    /// <remarks>
    ///     The safest way to use this is with a <c>foreach</c> loop.</remarks>
    public IEnumerator<T> GetEnumerator()
    {
        return new Enumerator(this);
    }

    IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

    private sealed class Enumerator : IEnumerator<T>
    {
        private Channel<T> _this;
        private T _current;
        private bool _everMoved = false;
        private bool _reachedEnd = false;

        public Enumerator(Channel<T> channel) { _this = channel; }

        public T Current
        {
            get
            {
                if (!_everMoved)
                    throw new InvalidOperationException("The Current property cannot be accessed before MoveNext is invoked.");
                if (_reachedEnd)
                    throw new InvalidOperationException("The Current property cannot be accessed after MoveNext returned false.");
                return _current;
            }
        }
        object IEnumerator.Current { get { return Current; } }

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            _everMoved = true;
            if (_reachedEnd)
                return false;
            var ret = _this.TryRead(out _current);
            if (!ret)
                _reachedEnd = true;
            return ret;
        }

        public void Reset()
        {
            throw new NotSupportedException("A channel enumerator cannot be reset.");
        }
    }
}
