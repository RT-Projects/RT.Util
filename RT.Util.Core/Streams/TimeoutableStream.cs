using System;
using System.IO;
using System.Threading;

namespace RT.Util.Streams
{
    /// <summary>
    /// Implements a stream whose synchronous read/write operations respect the <see cref="Stream.ReadTimeout"/>and <see cref="Stream.WriteTimeout"/>
    /// properties. Useful for wrapping streams which do not directly support these properties.
    /// </summary>
    public sealed class TimeoutableStream : Stream
    {
        private Stream _stream;
        private bool _disposed = false;

        /// <summary>Constructor.</summary>
        /// <param name="underlyingStream">The underlying stream on which all operations are to be performed.</param>
        public TimeoutableStream(Stream underlyingStream)
        {
            _stream = underlyingStream;
            ReadTimeout = Timeout.Infinite;
            WriteTimeout = Timeout.Infinite;
        }

        /// <summary>Disposes of this stream and the underlying stream.</summary>
        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;
                _stream.Dispose();
                base.Dispose();
            }
        }

        /// <summary>Indicates whether the underlying stream, and hence this stream, supports writing.</summary>
        public override bool CanWrite { get { return _stream.CanWrite; } }
        /// <summary>Indicates whether the underlying stream, and hence this stream, supports reading.</summary>
        public override bool CanRead { get { return _stream.CanRead; } }
        /// <summary>Indicates whether the underlying stream, and hence this stream, supports seeking.</summary>
        public override bool CanSeek { get { return _stream.CanSeek; } }
        /// <summary>Always returns true.</summary>
        public override bool CanTimeout { get { return true; } }

        /// <summary>Flushes the underlying stream.</summary>
        public override void Flush() { _stream.Flush(); }

        /// <summary>Gets the length of the underlying stream, if supported by it.</summary>
        public override long Length { get { return _stream.Length; } }

        /// <summary>Sets the length of the underlying stream, if supported by it.</summary>
        public override void SetLength(long value) { _stream.SetLength(value); }

        /// <summary>Gets or sets the current position in the stream, if supported by the underlying stream.</summary>
        public override long Position { get { return _stream.Position; } set { _stream.Position = value; } }

        /// <summary>Seeks to the specified position in the underlying stream, if the underlying stream supports it.</summary>
        public override long Seek(long offset, SeekOrigin origin) { return _stream.Seek(offset, origin); }

        /// <summary>
        /// Reads up to <paramref name="count"/> bytes into <paramref name="buffer"/> starting  at <paramref name="offset"/>
        /// from the underlying stream, if it supports this. Will block waiting for the read to complete for at most <see cref="Stream.ReadTimeout"/>
        /// milliseconds; afterwards, a <see cref="TimeoutException"/> is thrown.
        /// </summary>
        public override int Read(byte[] buffer, int offset, int count)
        {
            var asyncRead = _stream.BeginRead(buffer, offset, count, null, null);
            asyncRead.AsyncWaitHandle.WaitOne(ReadTimeout);
            if (asyncRead.IsCompleted)
            {
                var result = _stream.EndRead(asyncRead);
                asyncRead.AsyncWaitHandle.Close();
                return result;
            }
            else
            {
                _stream.Dispose();
                _stream.EndRead(asyncRead);
                asyncRead.AsyncWaitHandle.Close();
                throw new TimeoutException("The Read operation has timed out.");
            }
        }

        /// <summary>
        /// Writes <paramref name="count"/> bytes from <paramref name="buffer"/> starting at <paramref name="offset"/>
        /// to the underlying stream, if it supports this. Will block waiting for the write to complete for at most <see cref="Stream.WriteTimeout"/>
        /// milliseconds; afterwards, a <see cref="TimeoutException"/> is thrown.
        /// </summary>
        public override void Write(byte[] buffer, int offset, int count)
        {
            var asyncWrite = _stream.BeginWrite(buffer, offset, count, null, null);
            asyncWrite.AsyncWaitHandle.WaitOne(WriteTimeout);
            if (asyncWrite.IsCompleted)
            {
                _stream.EndWrite(asyncWrite);
                asyncWrite.AsyncWaitHandle.Close();
            }
            else
            {
                _stream.Dispose();
                try { _stream.EndWrite(asyncWrite); }
                catch (OperationCanceledException) { }
                asyncWrite.AsyncWaitHandle.Close();
                throw new TimeoutException("The Write operation has timed out.");
            }
        }

        /// <summary>
        /// Gets or sets a value, in miliseconds, that determines how long the stream will attempt to read before timing out.
        /// Specify <see cref="Timeout.Infinite"/> to suppress the time-out (which is the default value).
        /// </summary>
        public override int ReadTimeout { get; set; }

        /// <summary>
        /// Gets or sets a value, in miliseconds, that determines how long the stream will attempt to write before timing out.
        /// Specify <see cref="Timeout.Infinite"/> to suppress the time-out (which is the default value).
        /// </summary>
        public override int WriteTimeout { get; set; }
    }

}