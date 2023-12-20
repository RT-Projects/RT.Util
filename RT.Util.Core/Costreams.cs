namespace RT.KitchenSink;

static partial class Ks
{
    /// <summary>
    ///     Runs the two specified processes in parallel, allowing one to generate data by writing it to a stream, and the
    ///     other to consume the data by reading it from a stream.</summary>
    /// <param name="writingAction">
    ///     An action that generates data and writes it to a stream.</param>
    /// <param name="readingAction">
    ///     An action that will want to read information from a stream.</param>
    public static void RunCostreams(Action<Stream> writingAction, Action<Stream> readingAction)
    {
        // Everything the writingAction writes will be enqueued in here and dequeued by the readingAction
        var queue = new Queue<byteChunk>();
        using (var hasData = new ManualResetEvent(false))
        {
            var writer = new writingCostream(queue, hasData);
            var reader = new readingCostream(queue, hasData);

            // Start reading in a new thread. The first call to reader.Read() will block until there is something in the queue to read.
            var thread = new Thread(() => readingAction(reader));
            thread.Start();

            // Start writing. Calls to writer.Write() will place the data in the queue and signal the reading thread.
            writingAction(writer);

            // Insert a null at the end of the queue to signal to the reader that this is where the data ends.
            lock (queue)
            {
                queue.Enqueue(null);
                hasData.Set();
            }

            // Wait for the reader to consume all the remaining data.
            thread.Join();
        }
    }

    private sealed class byteChunk
    {
        public byte[] Buffer;
        public int Offset;
        public int Count;
    }

    private sealed class readingCostream : Stream
    {
        private Queue<byteChunk> _queue;
        private ManualResetEvent _hasData;

        public readingCostream(Queue<byteChunk> queue, ManualResetEvent hasData)
        {
            _queue = queue;
            _hasData = hasData;
        }

        public override bool CanRead { get { return true; } }
        public override bool CanSeek { get { return false; } }
        public override bool CanWrite { get { return false; } }
        public override void Flush() { }
        public override long Length { get { throw new NotSupportedException(); } }
        public override long Position { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }
        public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
        public override void SetLength(long value) { throw new NotSupportedException(); }
        public override void Write(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }

        public override int Read(byte[] buffer, int offset, int count)
        {
            // If there is no data waiting to be read, wait for it.
            _hasData.WaitOne();
            byteChunk peeked;
            lock (_queue)
                peeked = _queue.Peek();

            // A null element in the queue signals the end of the stream. Don't dequeue this item.
            if (peeked == null)
                return 0;

            if (peeked.Count <= count)
            {
                // If we can return the complete item, dequeue it
                Buffer.BlockCopy(peeked.Buffer, peeked.Offset, buffer, offset, peeked.Count);
                lock (_queue)
                {
                    _queue.Dequeue();
                    // If this has emptied the queue, tell the next call to read
                    if (_queue.Count == 0)
                        _hasData.Reset();
                }

                return peeked.Count;
            }

            // If we can only return part of the item, modify it accordingly
            Buffer.BlockCopy(peeked.Buffer, peeked.Offset, buffer, offset, count);
            peeked.Offset += count;
            peeked.Count -= count;
            return count;
        }
    }

    private sealed class writingCostream : Stream
    {
        private Queue<byteChunk> _queue;
        private ManualResetEvent _hasData;
        public writingCostream(Queue<byteChunk> queue, ManualResetEvent hasData)
        {
            _queue = queue;
            _hasData = hasData;
        }

        public override bool CanRead { get { return false; } }
        public override bool CanSeek { get { return false; } }
        public override bool CanWrite { get { return true; } }
        public override void Flush() { }
        public override long Length { get { throw new NotSupportedException(); } }
        public override long Position { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }
        public override int Read(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }
        public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
        public override void SetLength(long value) { throw new NotSupportedException(); }

        public override void Write(byte[] buffer, int offset, int count)
        {
            // Ignore zero-length writes
            if (count == 0)
                return;

            // We have to take a copy of the data because the calling thread might re-use the same buffer multiple times.
            var bufferCopy = new byte[count];
            Buffer.BlockCopy(buffer, offset, bufferCopy, 0, count);

            // Put the data in the queue
            lock (_queue)
            {
                _queue.Enqueue(new byteChunk { Buffer = bufferCopy, Offset = 0, Count = count });

                // Inform the reading thread that the queue now has data
                _hasData.Set();
            }
        }
    }
}
