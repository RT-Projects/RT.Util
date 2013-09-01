using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace RT.KitchenSink.Streams
{
    /// <summary>Provides functionality to create a stream object that has reading and writing reversed.</summary>
    public static class ReversePolarityStream
    {
        /// <summary>
        ///     Runs the specified process in a new thread and allows it to generate data by writing it to a stream, while
        ///     returning a stream that can be used to consume the data by reading from it.</summary>
        /// <param name="writer">
        ///     An action that generates data and writes it to a stream.</param>
        public static Stream CreateFromWriter(Action<Stream> writer)
        {
            // Everything the writer writes will be stored in here until the reader consumes it
            var info = new costreamInfo();

            // Start the writing action in a new thread
            var thread = new Thread(() =>
            {
                try
                {
                    writer(new writingCostream { Info = info });
                }
                catch (Exception e)
                {
                    info.Exception = e;
                }
                info.Ended = true;
                Monitor.PulseAll(info);
            });
            thread.Start();
            return new readingCostream { Info = info };
        }

        private sealed class costreamInfo
        {
            public byte[] Buffer;
            public int Offset;
            public int Count;
            public Exception Exception;
            public bool Ended;
        }

        [Serializable]
        private class streamAbortException : Exception
        {
            public streamAbortException(Exception inner) : base("The stream writer threw an exception.", inner) { }
        }

        private sealed class readingCostream : Stream
        {
            public costreamInfo Info;

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
                lock (Info)
                {
                    // If there is no data waiting to be read, wait for it.
                    while (!Info.Ended && Info.Count == 0)
                        Monitor.Wait(Info);

                    if (Info.Ended)
                    {
                        if (Info.Exception == null)
                            return 0;
                        throw new streamAbortException(Info.Exception);
                    }

                    if (Info.Count <= count)
                    {
                        // If we can return the complete item, we can signal the writer to write again
                        Buffer.BlockCopy(Info.Buffer, Info.Offset, buffer, offset, Info.Count);
                        Info.Count = 0;
                        Monitor.PulseAll(Info);
                        return Info.Count;
                    }
                    else
                    {
                        // If we can only return part of the item, modify it accordingly
                        Buffer.BlockCopy(Info.Buffer, Info.Offset, buffer, offset, count);
                        Info.Offset += count;
                        Info.Count -= count;
                        return count;
                    }
                }
            }
        }

        private sealed class writingCostream : Stream
        {
            public costreamInfo Info;
            private bool _closed = false;

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

                lock (Info)
                {
                    // We don’t need to take a copy of the data because this method won’t return until the buffer has been consumed
                    Info.Buffer = buffer;
                    Info.Offset = offset;
                    Info.Count = count;

                    // Inform the reading thread that the chunk now has data
                    Monitor.PulseAll(Info);

                    // Wait for the data to be consumed
                    while (Info.Count > 0)
                        Monitor.Wait(Info);
                }
            }

            public override void Close()
            {
                if (_closed)
                    return;

                lock (Info)
                {
                    Info.Ended = true;
                    Monitor.PulseAll(Info);
                }

                _closed = true;
                base.Close();
            }
        }
    }
}
