using System;
using System.Collections.Generic;
using System.IO;
using RT.Util.ExtensionMethods;

namespace RT.Util.Streams
{
    /// <summary>
    /// Implements a stream that exposes methods to transparently peek at the bytes that would be read by a call to <see cref="Read"/>,
    /// without affecting the actual outcome of future calls to standard Stream methods. See Remarks for further info.
    /// </summary>
    /// <remarks>
    /// <para>In order for this stream to operate correctly, no direct reads, writes or seeks must be performed on the
    /// underlying stream. All operations must be executed through this class.</para>
    /// <para>Peeking will cause reads in the underlying stream, but this class ensures that the data is read only once,
    /// in order to be compatible with pure (read-once, non-seekable) streams.</para>
    /// <para>The class maintains that any operations on the streams returned by <see cref="GetPeekStream"/> do not modify
    /// the outcome of any other operations performed on this stream, including the value of <see cref="Position"/>, the bytes
    /// read by <see cref="Read"/>, or the effects of <see cref="Write"/>. One visible side effect may be the change in chunk
    /// size returned by a single call to <see cref="Read"/>. This assumes that if the underlying stream is seekable then nothing
    /// else changes the data stored in it.</para>
    /// <para>Neither this class nor the peek streams returned by it are thread-safe. All accesses must occur on a single thread.</para>
    /// </remarks>
    public class PeekableStream : Stream
    {
        private Stream _stream;
        private LinkedList<Tuple<byte[], int>> _buffers = new LinkedList<Tuple<byte[], int>>();
        private int _offset;
        private List<PeekStream> _peeks = new List<PeekStream>();
        private bool _disposed;

        /// <summary>Constructor.</summary>
        /// <param name="underlyingStream">The underlying stream on which all operations are to be performed.</param>
        public PeekableStream(Stream underlyingStream)
        {
            _stream = underlyingStream;
        }

        /// <summary>Disposes of this stream, the underlying stream and any associated peek streams.</summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;
                var peeks = _peeks;
                _peeks = null;
                foreach (var peek in peeks)
                    peek.Dispose();
                _stream.Dispose();
                _stream = null;
                _buffers = null;
            }
            base.Dispose(disposing);
        }

        /// <summary>Indicates whether the underlying stream, and hence this stream, supports writing.</summary>
        public override bool CanWrite { get { return _stream.CanWrite; } }
        /// <summary>Indicates whether the underlying stream, and hence this stream, supports reading.</summary>
        public override bool CanRead { get { return _stream.CanRead; } }
        /// <summary>Indicates whether the underlying stream, and hence this stream, supports seeking.</summary>
        public override bool CanSeek { get { return _stream.CanSeek; } }

        /// <summary>Flushes the underlying stream.</summary>
        public override void Flush() { _stream.Flush(); }

        /// <summary>Gets the length of the underlying stream, if supported by it.</summary>
        public override long Length { get { return _stream.Length; } }

        /// <summary>Sets the length of the underlying stream, if supported by it. Note that setting the length causes
        /// all peek streams to be invalidated.</summary>
        public override void SetLength(long value)
        {
            _stream.SetLength(value);
            clearPeekBuffersAndInvalidate();
        }

        /// <summary>
        /// Gets or sets the current position in the stream, if supported by the underlying stream. Note that
        /// seeking causes all peek streams to be reset so as to resume peeking from the point seeked to.
        /// Note also that getting the current position requires a small amount of computation and should be used with care in tight loops.
        /// </summary>
        public override long Position
        {
            get
            {
                long pos = _stream.Position;
                foreach (var buf in _buffers)
                    pos -= buf.Item2;
                return pos + _offset;
            }
            set { Seek(value, SeekOrigin.Begin); }
        }

        /// <summary>
        /// Seeks to the specified position in the underlying stream, if the underlying stream supports it. Note that
        /// seeking causes all peek streams to be invalidated.
        /// </summary>
        public override long Seek(long offset, SeekOrigin origin)
        {
            var result = _stream.Seek(offset, origin);
            clearPeekBuffersAndInvalidate();
            return result;
        }

        /// <summary>
        /// Writes data to the underlying stream. See Remarks for notes concerning seekable underlying streams.
        /// If the underlying stream is not seekable, writes are assumed to be separate to reads and thus peeks, so
        /// the peek streams aren't touched.
        /// </summary>
        /// <remarks>
        /// <para>Notes for seekable underlying streams:</para>
        /// <para>The write will occur at the current position regardless of how much the peek streams have peeked.</para>
        /// <para>All peek streams will be invalidated.</para>
        /// </remarks>
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_stream.CanSeek)
                _stream.Position = Position;
            _stream.Write(buffer, offset, count);
            if (_stream.CanSeek)
                clearPeekBuffersAndInvalidate();
        }

        /// <summary>
        /// Reads data from the stream into the specified buffer. Advances all peek streams that got overtaken by the
        /// new stream position so as to continue reading from //
        /// </summary>
        /// <returns>Number of bytes actually read. Zero if called on a stream that has already ended, or if <paramref name="count"/> was zero.</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_buffers.Count == 0)
            {
                return _stream.Read(buffer, offset, count);
            }
            else
            {
                byte[] firstbuf = _buffers.First.Value.Item1;
                int read = Math.Min(count, _buffers.First.Value.Item2 - _offset);
                if (read <= 0) throw new InternalErrorException("PeekableStream: _offset was pointing beyond the end of the first buffer");

                Buffer.BlockCopy(src: firstbuf, srcOffset: _offset, dst: buffer, dstOffset: offset, count: read);
                var removed = removeFromBuffers(read);
                if (removed != read) throw new InternalErrorException("PeekableStream: 827364");

                return read;
            }
        }

        /// <summary>Behaves like Read, except that the bytes are discarded.</summary>
        /// <param name="count">Maximum number of bytes to skip.</param>
        /// <returns>Number of bytes actually skipped. Zero if called on a stream that has already ended, or if <paramref name="count"/> was zero.</returns>
        public virtual int Skip(int count)
        {
            if (_buffers.Count == 0)
            {
                byte[] dummy = new byte[count];
                return _stream.Read(dummy, 0, count);
            }
            else
            {
                return removeFromBuffers(count);
            }
        }

        /// <summary>Skips the specified number of bytes in the current stream, and throws <see cref="EndOfStreamException"/>
        /// if the end of the stream is reached early.</summary>
        /// <param name="count">Number of bytes to skip.</param>
        public void SkipExactly(int count)
        {
            while (count > 0)
            {
                int skipped = Skip(count);
                if (skipped == 0)
                    throw new EndOfStreamException("Cannot Skip() {0} bytes.".Fmt(count));
                count -= skipped;
            }
        }

        /// <summary>
        /// <para>Creates and returns a new peek stream linked to this stream. Reading from the returned stream allows
        /// peeking at the bytes ahead of the current position in this stream, without changing the outcome of future
        /// calls to any methods on this stream.</para>
        /// <para>The returned stream must be disposed of when done, since outstanding undisposed peek streams have a slight
        /// performance impact on most operations on this peekable stream.</para>
        /// <para>See Remarks on <see cref="PeekableStream"/> for more info.</para>
        /// </summary>
        public PeekStream GetPeekStream()
        {
            var peek = new PeekStream(this);
            peek._buffer = _buffers.First;
            peek._offset = _offset;
            _peeks.Add(peek);
            return peek;
        }

        /// <summary>Clears the peek buffers and invalidates all peek streams.</summary>
        private void clearPeekBuffersAndInvalidate()
        {
            _buffers.Clear();
            _offset = 0;
            foreach (var peek in _peeks)
                peek.IsValid = false;
        }

        /// <summary>
        /// Reads at most the specified number of bytes into the peek buffers and updates all peek streams which had
        /// run out of peek buffers. Doesn't change anything if the stream has ended or <paramref name="count"/> was zero.
        /// </summary>
        private void peekIntoBuffers(int count)
        {
            byte[] data = new byte[count];
            int read = _stream.Read(data, 0, count);
            if (read > 0)
            {
                var node = _buffers.AddLast(new Tuple<byte[], int>(data, read));
                foreach (var peek in _peeks)
                    if (peek._buffer == null)
                        peek._buffer = node; // the offset should already be at zero in this case
            }
        }

        /// <summary>
        /// Removes exactly the specified number of bytes from the start of the peek buffers. Advances all peek streams
        /// that got overtaken to resume peeking from the next unremoved byte, if available. If there aren't enough bytes
        /// left in the peek buffers, removes all the available bytes.
        /// </summary>
        /// <returns>The actual number of bytes removed (which is always the requested count unless the buffers became completely emptied).</returns>
        private int removeFromBuffers(int count)
        {
            if (_buffers.Count == 0)
                return 0;

            _offset += count;
            while (_buffers.Count > 0 && _offset >= _buffers.First.Value.Item2)
            {
                foreach (var peek in _peeks)
                    if (peek._buffer == _buffers.First)
                    {
                        peek._buffer = peek._buffer.Next;
                        peek._offset = 0;
                    }
                _offset -= _buffers.First.Value.Item2;
                _buffers.RemoveFirst();
            }

            if (_buffers.Count == 0)
            {
                int removed = count - _offset;
                _offset = 0;
                foreach (var peek in _peeks)
                {
                    peek._buffer = null;
                    peek._offset = 0;
                }
                return removed;
            }
            else
            {
                foreach (var peek in _peeks)
                    if (peek._buffer == _buffers.First)
                        peek._offset = Math.Max(peek._offset, _offset);
                return count;
            }
        }

        /// <summary>
        /// Reads on this stream are implemented as peeking into the parent stream. That is, the position of the parent stream
        /// is unaffected when this stream is used to peek ahead of the current position in the parent stream.
        /// </summary>
        public class PeekStream : Stream
        {
            /// <summary>The <see cref="PeekableStream"/> that this peek stream belongs to.</summary>
            internal PeekableStream _parent;
            /// <summary>The buffer of the <see cref="_parent"/> in which this stream is currently positioned.</summary>
            internal LinkedListNode<Tuple<byte[], int>> _buffer;
            /// <summary>Current position of the peek stream within the current <see cref="_buffer"/>.</summary>
            internal int _offset;

            internal PeekStream(PeekableStream parent)
            {
                _parent = parent;
                IsValid = true;
            }

            /// <summary>Disposes of this stream and unregisters it from its parent.</summary>
            protected override void Dispose(bool disposing)
            {
                if (disposing && _parent != null)
                {
                    if (_parent._peeks != null)
                        _parent._peeks.Remove(this);
                    _parent = null;
                    _buffer = null;
                }
                base.Dispose(disposing);
            }

            /// <summary>Always returns true.</summary>
            public override bool CanRead { get { return true; } }
            /// <summary>Always returns false.</summary>
            public override bool CanSeek { get { return false; } }
            /// <summary>Always returns false.</summary>
            public override bool CanWrite { get { return false; } }
            /// <summary>Does nothing.</summary>
            public override void Flush() { }
            /// <summary>Throws a <see cref="NotSupportedException"/>.</summary>
            public override long Length { get { throw new NotSupportedException(); } }
            /// <summary>Throws a <see cref="NotSupportedException"/>.</summary>
            public override void SetLength(long value) { throw new NotSupportedException(); }
            /// <summary>Throws a <see cref="NotSupportedException"/>.</summary>
            public override long Position { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }
            /// <summary>Throws a <see cref="NotSupportedException"/>.</summary>
            public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
            /// <summary>Throws a <see cref="NotSupportedException"/>.</summary>
            public override void Write(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }

            /// <summary>Initially true. Whenever the parent stream's position overtakes the position of this stream, changes to
            /// false. In this state, attempts to read will throw an <see cref="InvalidOperationException"/> and the stream
            /// becomes useless.</summary>
            public bool IsValid { get; internal set; }

            /// <summary>
            /// Peeks at most the specified number of bytes from the parent stream. Because the peek stream maintains its own
            /// position, subsequent calls to this method will peek further and further into the parent stream. Note however that
            /// direct operations on the parent's underlying stream can break the sequence, and that some calls on the parent
            /// stream will reset this position. All such calls are documented to this effect.
            /// </summary>
            /// <returns>The actual number of bytes peeked. Zero if called when peeked all the way to the end of the parent stream, or if <paramref name="count"/> was zero.</returns>
            public override int Read(byte[] buffer, int offset, int count)
            {
                if (!IsValid)
                    throw new InvalidOperationException("Cannot read through this peek stream because the parent stream has advanced past the current position of this peek stream.");
                if (_buffer == null)
                    _parent.peekIntoBuffers(count);
                if (_buffer == null)
                    return 0;
                int read = Math.Min(_buffer.Value.Item2 - _offset, count);
                Buffer.BlockCopy(_buffer.Value.Item1, _offset, buffer, offset, read);
                _offset += read;
                if (_offset == _buffer.Value.Item2)
                {
                    _buffer = _buffer.Next;
                    _offset = 0;
                }
                return read;
            }

        }
    }
}
