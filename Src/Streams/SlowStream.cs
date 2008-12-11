using System;
using System.IO;

namespace RT.Util.Streams
{
    /// <summary>
    /// Provides methods to read from a stream in small chunks at a time.
    /// </summary>
    public class SlowStream : Stream
    {
        /// <summary>Gets or sets the current chunk size (number of bytes read at a time).</summary>
        public int ChunkSize { get; set; }

        private Stream MyStream;

        /// <summary>Initialises a new SlowStream instance.</summary>
        /// <param name="myStream">The underlying stream to read in chunks from.</param>
        public SlowStream(Stream myStream) { MyStream = myStream; }

        /// <summary>Initialises a new SlowStream instance.</summary>
        /// <param name="myStream">The underlying stream to read in chunks from.</param>
        /// <param name="chunkSize">The number of bytes to read per chunk.</param>
        public SlowStream(Stream myStream, int chunkSize) { MyStream = myStream; ChunkSize = chunkSize; }

#pragma warning disable 1591    // Missing XML comment for publicly visible type or member

        public override bool CanRead
        {
            get { return MyStream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return MyStream.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return MyStream.CanWrite; }
        }

        public override void Flush()
        {
            MyStream.Flush();
        }

        public override long Length
        {
            get { return MyStream.Length; }
        }

        public override long Position
        {
            get
            {
                return MyStream.Position;
            }
            set
            {
                MyStream.Position = value;
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return MyStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            MyStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            MyStream.Write(buffer, offset, count);
        }

        public override void Close()
        {
            MyStream.Close();
        }

#pragma warning restore 1591    // Missing XML comment for publicly visible type or member

        /// <summary>Reads at most <see cref="ChunkSize"/> bytes from the underlying stream.</summary>
        /// <param name="buffer">Buffer to store results into.</param>
        /// <param name="offset">Offset in buffer to store results at.</param>
        /// <param name="count">Maximum number of bytes to read.</param>
        /// <returns>Number of bytes read.</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            return MyStream.Read(buffer, offset, Math.Min(count, ChunkSize));
        }
    }
}
