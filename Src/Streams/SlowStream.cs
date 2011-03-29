using System;
using System.IO;
using System.Threading;

namespace RT.Util.Streams
{
    /// <summary>
    /// Provides methods to read from a stream in small chunks at a time. Optionally suspends
    /// the thread for a specified interval on every chunk.
    /// </summary>
    public sealed class SlowStream : Stream
    {
        /// <summary>Gets or sets the current chunk size (number of bytes read at a time).</summary>
        public int ChunkSize { get; set; }
        /// <summary>Gets or sets the current interval, in ms, for which the reading thread is suspended on every chunk. Defaults to 0, which means no delay.</summary>
        public int SleepInterval { get; set; }

        private Stream _stream;

        /// <summary>Initialises a new SlowStream instance.</summary>
        /// <param name="stream">The underlying stream to read in chunks from.</param>
        /// <param name="chunkSize">The number of bytes to read per chunk. Defaults to 1 KB.</param>
        public SlowStream(Stream stream, int chunkSize = 1024)
        {
            if (chunkSize < 1)
                throw new ArgumentOutOfRangeException("chunkSize", "chunkSize cannot be zero or negative.");
            _stream = stream;
            ChunkSize = chunkSize;
        }

#pragma warning disable 1591    // Missing XML comment for publicly visible type or member

        public override bool CanRead
        {
            get { return _stream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return _stream.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return _stream.CanWrite; }
        }

        public override void Flush()
        {
            _stream.Flush();
        }

        public override long Length
        {
            get { return _stream.Length; }
        }

        public override long Position
        {
            get
            {
                return _stream.Position;
            }
            set
            {
                _stream.Position = value;
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _stream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            while (count > 0)
            {
                if (SleepInterval > 0) Thread.Sleep(SleepInterval);
                var len = Math.Min(count, ChunkSize);
                _stream.Write(buffer, offset, len);
                offset += len;
                count -= len;
            }
        }

        public override void Close()
        {
            _stream.Close();
        }

#pragma warning restore 1591    // Missing XML comment for publicly visible type or member

        /// <summary>Reads at most <see cref="ChunkSize"/> bytes from the underlying stream.</summary>
        /// <param name="buffer">Buffer to store results into.</param>
        /// <param name="offset">Offset in buffer to store results at.</param>
        /// <param name="count">Maximum number of bytes to read.</param>
        /// <returns>Number of bytes read.</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (SleepInterval > 0) Thread.Sleep(SleepInterval);
            return _stream.Read(buffer, offset, Math.Min(count, ChunkSize));
        }
    }
}
