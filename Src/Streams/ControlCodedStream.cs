using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using RT.Util.ExtensionMethods;

namespace RT.KitchenSink.Streams
{
    /// <summary>
    /// Implements a stream which enables control codes to be read from or written to the underlying stream at
    /// certain points. This stream handles encoding and decoding the control codes and ensuring they are distinguishable
    /// from the payload data. See Remarks.
    /// </summary>
    /// <remarks>
    /// This stream does not support seeking because of the variable length nature of the data between control points.
    /// Seeking directly on the underlying stream must be avoided, since seeking into the middle of an escape sequence
    /// will result in incorrect data being read.
    /// </remarks>
    public class ControlCodedStream : Stream
    {
        private PeekableStream _stream;
        private byte[] _buffer;

        /// <summary>Constructor.</summary>
        /// <param name="underlyingStream">The underlying stream on which all operations are to be performed.</param>
        public ControlCodedStream(PeekableStream underlyingStream)
        {
            _stream = underlyingStream;
        }

        /// <summary>Indicates whether the underlying stream, and hence this stream, supports writing.</summary>
        public override bool CanWrite { get { return _stream.CanWrite; } }
        /// <summary>Indicates whether the underlying stream, and hence this stream, supports reading.</summary>
        public override bool CanRead { get { return _stream.CanRead; } }
        /// <summary>Always returns false.</summary>
        public override bool CanSeek { get { return false; } }

        /// <summary>Flushes the underlying stream.</summary>
        public override void Flush() { _stream.Flush(); }

        /// <summary>Throws a <see cref="NotSupportedException"/>.</summary>
        public override long Length { get { throw new NotSupportedException(); } }
        /// <summary>Throws a <see cref="NotSupportedException"/>.</summary>
        public override void SetLength(long value) { throw new NotSupportedException(); }
        /// <summary>Throws a <see cref="NotSupportedException"/>.</summary>
        public override long Position { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }
        /// <summary>Throws a <see cref="NotSupportedException"/>.</summary>
        public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }

        /// <summary>Writes the specified control code to the stream.</summary>
        /// <param name="code">The code to write. Valid values are 0..254; 255 is reserved.</param>
        public virtual void WriteControlCode(byte code)
        {
            if (code == 255)
                throw new ArgumentOutOfRangeException("Control code 255 is reserved.");
            if (_buffer == null)
                _buffer = new byte[2];
            _buffer[0] = 255;
            _buffer[1] = code;
            _stream.Write(_buffer, 0, 2);
        }

        /// <summary>
        /// Reads a control code from the stream at the current position. If there is no control code at the current position,
        /// returns <c>-1</c>. Otherwise returns the code read. Throws <see cref="EndOfStreamException"/> if the stream ends in
        /// the middle of a control code.
        /// </summary>
        public virtual int ReadControlCode()
        {
            using (var peek = _stream.GetPeekStream())
            {
                byte[] read = peek.Read(2);
                if (read.Length == 0)
                    return -1;
                else if (read.Length == 1)
                    throw new EndOfStreamException("The end of the stream was encountered in the middle of an escape sequence.");
                if (read[0] != 255)
                    return -1;
                _stream.SkipExactly(2);
                return read[1];
            }
        }

        /// <summary>
        /// Determines if the stream has ended. Note that this method may block, potentially indefinitely, in cases where
        /// it isn't known yet if the stream has ended (for example, a NetworkStream for a socket that is idle but not closed).
        /// </summary>
        public bool IsEnded
        {
            get
            {
                using (var peek = _stream.GetPeekStream())
                {
                    byte[] read = peek.Read(1);
                    return read.Length == 0;
                }
            }
        }

        /// <summary>Writes the specified data to the underlying stream.</summary>
        public override void Write(byte[] buffer, int offset, int count)
        {
            // Just simply need to write every 255 byte twice.
            int from = offset;
            for (int i = offset; i < offset + count; i++)
            {
                if (buffer[i] == 255)
                {
                    _stream.Write(buffer, from, i - from + 1);
                    from = i;
                }
            }
            _stream.Write(buffer, from, offset + count - from);
        }

        /// <summary>
        /// Reads up to <paramref name="count"/> bytes into the buffer from the underlying stream. If a control code
        /// is encountered before any data has been read, will throw an <see cref="InvalidOperationException"/>. If some
        /// data has already been read upon encountering a control code, will stop and return all the data read up to the control code.
        /// </summary>
        /// <returns>The number of bytes actually read. Returns zero if the stream was ended when the read started, or if <paramref name="count"/> was zero.</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            using (var peek = _stream.GetPeekStream())
            {
                int read = peek.Read(buffer, offset, count);
                if (read == 0)
                    return 0;
                else if (read == 1)
                {
                    if (buffer[offset] != 255)
                    {
                        _stream.SkipExactly(1);
                        return 1;
                    }
                    read = peek.Read(buffer, offset, 1);
                    if (read == 0)
                        throw new EndOfStreamException("The stream ended in the middle of an escape sequence.");
                    if (buffer[offset] == 255)
                    {
                        _stream.SkipExactly(2);
                        return 1;
                    }
                    else
                        throw new InvalidOperationException("Attempted to read a control code using Read.");
                }
                else
                {
                    if (buffer[offset] == 255 && buffer[offset + 1] != 255)
                        throw new InvalidOperationException("Attempted to read a control code using Read.");
                    int offsetR = offset;
                    int offsetW = offset;
                    int offsetLast = offset + read - 1;
                    while (offsetR < offsetLast)
                    {
                        byte cur = buffer[offsetR];
                        offsetR++;
                        if (cur == 255)
                        {
                            cur = buffer[offsetR];
                            offsetR++;
                            if (cur != 255)
                            {
                                // Found a control code, but we've definitely read at least one byte.
                                _stream.SkipExactly(offsetR - 2 - offset);
                                return offsetW - offset;
                            }
                        }

                        buffer[offsetW] = cur;
                        offsetW++;
                    }
                    if (offsetR == offsetLast)
                    {
                        byte cur = buffer[offsetR];
                        if (cur != 255)
                        {
                            // Not the start of an escape sequence so consume it too
                            offsetR++;
                            buffer[offsetW] = cur;
                            offsetW++;
                        }
                    }
                    _stream.SkipExactly(offsetR - offset);
                    return offsetW - offset;
                }
            }
        }
    }
}
