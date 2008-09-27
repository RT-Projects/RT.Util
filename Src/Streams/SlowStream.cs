using System;
using System.IO;

namespace RT.Util.Streams
{
    /// <summary>
    /// Provides methods to read from a stream in small chunks at a time.
    /// </summary>
    public class SlowStream : Stream
    {
        /// <summary>Size of each chunk to read at a time.</summary>
        public static int ChunkSize = 20;

        private Stream MyStream;

        /// <summary>Initialises a new SlowStream instance.</summary>
        /// <param name="MyStream">The underlying stream to read in chunks from.</param>
        public SlowStream(Stream MyStream) { this.MyStream = MyStream; }

#pragma warning disable 1591

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

#pragma warning restore 1591

        /// <summary>Reads at mose <see cref="ChunkSize"/> bytes from the underlying stream.</summary>
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
