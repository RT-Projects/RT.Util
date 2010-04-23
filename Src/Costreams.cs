using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace RT.KitchenSink
{
    static partial class Ut
    {
        /// <summary>Runs the two specified processes in parallel, allowing one to generate data by writing it to a stream, and the other to consume the data by reading it from the stream.</summary>
        /// <param name="writingAction">An action that generates data and writes it to a stream.</param>
        /// <param name="readingAction">An action that will want to read information from a stream.</param>
        public static void RunCostreams(Action<Stream> writingAction, Action<Stream> readingAction)
        {
            // Everything the writingAction writes will be enqueued in here and dequeued by the readingAction
            var queue = new Queue<byteChunk>();

            writingCostream writer = new writingCostream(queue);
            readingCostream reader = new readingCostream(queue);

            // Start reading in a new thread. The first call to reader.Read() will block until there is something in the queue to read.
            var thread = new Thread(() => readingAction(reader));
            thread.Start();

            // Start writing. Calls to writer.Write() will place the data in the queue and signal the reading thread.
            writingAction(writer);

            // Insert a null at the end of the queue to signal to the reader that this is where the data ends.
            queue.Enqueue(null);

            // Wait for the reader to consume all the remaining data.
            thread.Join();
        }

        private class byteChunk
        {
            public byte[] Buffer;
            public int Offset;
            public int Count;
        }

        private class readingCostream : Stream
        {
            private Queue<byteChunk> _queue;
            public readingCostream(Queue<byteChunk> queue) { _queue = queue; }

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
                lock (_queue)
                {
                    // If there is no data waiting to be read, wait for it.
                    if (_queue.Count == 0)
                        Monitor.Wait(_queue);

                    var peeked = _queue.Peek();

                    // A null element in the queue signals the end of the stream. Don't dequeue this item.
                    if (peeked == null)
                        return 0;

                    if (peeked.Count <= count)
                    {
                        // If we can return the complete item, dequeue it
                        Buffer.BlockCopy(peeked.Buffer, peeked.Offset, buffer, offset, peeked.Count);
                        _queue.Dequeue();
                        return peeked.Count;
                    }
                    else
                    {
                        // If we can only return part of the item, modify it accordingly
                        Buffer.BlockCopy(peeked.Buffer, peeked.Offset, buffer, offset, count);
                        peeked.Offset += count;
                        peeked.Count -= count;
                        return count;
                    }
                }
            }
        }

        private class writingCostream : Stream
        {
            private Queue<byteChunk> _queue;
            public writingCostream(Queue<byteChunk> queue) { _queue = queue; }

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

                lock (_queue)
                {
                    // We have to take a copy of the data because the calling thread might re-use the same buffer multiple times.
                    var bufferCopy = new byte[count];
                    Buffer.BlockCopy(buffer, offset, bufferCopy, 0, count);

                    // Put the data in the queue
                    _queue.Enqueue(new byteChunk { Buffer = buffer, Offset = offset, Count = count });

                    // Signal the reading thread(s) that the queue has changed (in case it's waiting)
                    Monitor.PulseAll(_queue);
                }
            }
        }
    }
}