using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace RT.KitchenSink
{
    public sealed class Channel<T> : IEnumerable<T>, IDisposable
    {
        private Queue<T> _queue = new Queue<T>();
        private bool _closed = false;

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

        public T Read()
        {
            lock (_queue)
            {
                while (!_closed && _queue.Count == 0)
                    Monitor.Wait(_queue);
                if (_queue.Count > 0)
                    return _queue.Dequeue();
                throw new InvalidOperationException("The channel has already been closed.");
            }
        }

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

        public void Close()
        {
            lock (_queue)
            {
                _closed = true;
                Monitor.PulseAll(_queue);
            }
        }
        void IDisposable.Dispose() { Close(); }

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

        public IEnumerator<T> GetEnumerator()
        {
            return new enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

        private sealed class enumerator : IEnumerator<T>
        {
            private Channel<T> _this;
            private T _current;
            private bool _everMoved = false;
            private bool _reachedEnd = false;

            public enumerator(Channel<T> channel) { _this = channel; }

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
                return _this.TryRead(out _current);
            }

            public void Reset()
            {
                throw new NotSupportedException("A channel enumerator cannot be reset.");
            }
        }
    }
}
