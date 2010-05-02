﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using RT.Util.ExtensionMethods;

namespace RT.Util.Streams
{
    /// <summary>Provides functionality to replace the three different newlines (\r, \n, and \r\n) while reading from a stream containing textual data,
    /// without caring about the text encoding, as long as it is an encoding in which the newline characters are 8-bit ASCII (e.g. UTF-8).</summary>
    public sealed class NewlineNormalizerStream8bit : Stream
    {
        /// <summary>Returns false.</summary>
        public override bool CanSeek { get { return false; } }
        /// <summary>Returns false.</summary>
        public override bool CanWrite { get { return false; } }
        /// <summary>Does nothing.</summary>
        public override void Flush() { }
        /// <summary>Throws a <see cref="NotSupportedException"/>.</summary>
        public override long Length { get { throw new NotSupportedException(); } }
        /// <summary>Throws a <see cref="NotSupportedException"/>.</summary>
        public override long Position { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }
        /// <summary>Throws a <see cref="NotSupportedException"/>.</summary>
        public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
        /// <summary>Throws a <see cref="NotSupportedException"/>.</summary>
        public override void SetLength(long value) { throw new NotSupportedException(); }
        /// <summary>Throws a <see cref="NotSupportedException"/>.</summary>
        public override void Write(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }

        /// <summary>Return false.</summary>
        public override bool CanRead { get { return true; } }

        private byte[] _lastBuffer;
        private int _lastBufferIndex;
        private int _lastBufferLength;
        private Stream _underlyingStream;
        private byte[] _newline;
        private byte[] _stillToOutput;
        private int _stillToOutputIndex;
        private int _stillToOutputLength;
        private bool _ignoreOneLF = false;

        /// <summary>Constructs a <see cref="NewlineNormalizerStream8bit"/>.</summary>
        /// <param name="underlyingStream">Stream to read textual data from.</param>
        /// <param name="newline">Normalised newline to use.</param>
        public NewlineNormalizerStream8bit(Stream underlyingStream, byte[] newline = null)
        {
            _newline = newline ?? Environment.NewLine.ToUtf8();
            if (_newline.Length < 1)
                throw new ArgumentException("newline cannot be the empty array.", "newline");
            _underlyingStream = underlyingStream;
        }

        private int outputAsMuchAsPossible(ref byte[] fromBuffer, ref int fromOffset, ref int fromCount, byte[] intoBuffer, int intoOffset, int intoCount)
        {
            if (intoCount < fromCount)
            {
                Buffer.BlockCopy(fromBuffer, fromOffset, intoBuffer, intoOffset, intoCount);
                fromOffset += intoCount;
                fromCount -= intoCount;
                return intoCount;
            }
            else
            {
                Buffer.BlockCopy(fromBuffer, fromOffset, intoBuffer, intoOffset, fromCount);
                fromBuffer = null;
                return fromCount;
            }
        }

        /// <summary>
        /// Reads a sequence of bytes from the current stream and advances the position within the stream
        /// by the number of bytes read.
        /// </summary>
        /// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified
        ///     byte array with the values between offset and (offset + count - 1) replaced
        ///     by the bytes read from the current source.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read
        ///     from the current stream.</param>
        /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
        /// <returns>The total number of bytes read into the buffer. This can be less than the
        ///     number of bytes requested if that many bytes are not currently available,
        ///     or zero (0) if the end of the stream has been reached.</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (count < 1)
                throw new ArgumentOutOfRangeException("count", "count cannot be zero or negative.");

            if (_stillToOutput != null && _stillToOutputLength > 0)
                return outputAsMuchAsPossible(ref _stillToOutput, ref _stillToOutputIndex, ref _stillToOutputLength, buffer, offset, count);

            if (_lastBuffer != null && _lastBufferLength > 0)
            {
                var index = _lastBufferIndex;
                while (index < _lastBufferIndex + _lastBufferLength && _lastBuffer[index] != 10 && _lastBuffer[index] != 13)
                    index++;

                if (index == _lastBufferIndex + _lastBufferLength)
                    return outputAsMuchAsPossible(ref _lastBuffer, ref _lastBufferIndex, ref _lastBufferLength, buffer, offset, count);

                int nlCount = index - _lastBufferIndex;
                if (nlCount == 0)
                {
                    // Output just the newline
                    _stillToOutput = new byte[_newline.Length];
                    Buffer.BlockCopy(_newline, 0, _stillToOutput, 0, _newline.Length);
                    _stillToOutputIndex = 0;
                    _stillToOutputLength = _newline.Length;
                    var result = outputAsMuchAsPossible(ref _stillToOutput, ref _stillToOutputIndex, ref _stillToOutputLength, buffer, offset, count);

                    byte b = _lastBuffer[_lastBufferIndex];
                    _lastBufferIndex++;
                    _lastBufferLength--;

                    // \n is always a single-character newline, but...
                    if (b == 13)
                    {
                        // ... if it's a \r, look at the next byte after it
                        if (_lastBufferLength == 0)
                        {
                            // Can't look at the next byte yet, remember for later that we need to ignore a \n
                            _ignoreOneLF = true;
                        }
                        else if (_lastBuffer[_lastBufferIndex] == 10)
                        {
                            _lastBufferIndex++;
                            _lastBufferLength--;
                        }
                    }

                    return result;
                }
                else if (nlCount < count)
                {
                    // Output everything up to the newline
                    Buffer.BlockCopy(_lastBuffer, _lastBufferIndex, buffer, offset, nlCount);
                    _lastBufferIndex += nlCount;
                    _lastBufferLength -= nlCount;
                    return nlCount;
                }
                else
                {
                    // Output everything we can
                    return outputAsMuchAsPossible(ref _lastBuffer, ref _lastBufferIndex, ref _lastBufferLength, buffer, offset, count);
                }
            }

            if (_lastBuffer == null)
                _lastBuffer = new byte[65536];
            _lastBufferIndex = 0;
            _lastBufferLength = _underlyingStream.Read(_lastBuffer, 0, 65536);
            if (_lastBufferLength == 0)
                return 0;

            if (_ignoreOneLF)
            {
                if (_lastBuffer[_lastBufferIndex] == 10)
                {
                    // Note that if _lastBufferLength is 1 and we're taking it down to 0 here, the following
                    // recursive call will still work correctly by reading from the underlying stream again
                    _lastBufferIndex++;
                    _lastBufferLength--;
                }
                _ignoreOneLF = false;
            }

            // Now that we have populated _lastBuffer, use a tail-recursive call.
            return Read(buffer, offset, count);
        }
    }

    /// <summary>Provides functionality to replace the three different newlines (\r, \n, and \r\n) while reading from a stream containing textual data,
    /// without caring about the text encoding, as long as it is an encoding in which the newline characters are made of 16-bit characters (e.g. UTF-16).</summary>
    public sealed class NewlineNormalizerStream16bit : Stream
    {
        /// <summary>Returns false.</summary>
        public override bool CanSeek { get { return false; } }
        /// <summary>Returns false.</summary>
        public override bool CanWrite { get { return false; } }
        /// <summary>Does nothing.</summary>
        public override void Flush() { }
        /// <summary>Throws a <see cref="NotSupportedException"/>.</summary>
        public override long Length { get { throw new NotSupportedException(); } }
        /// <summary>Throws a <see cref="NotSupportedException"/>.</summary>
        public override long Position { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }
        /// <summary>Throws a <see cref="NotSupportedException"/>.</summary>
        public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
        /// <summary>Throws a <see cref="NotSupportedException"/>.</summary>
        public override void SetLength(long value) { throw new NotSupportedException(); }
        /// <summary>Throws a <see cref="NotSupportedException"/>.</summary>
        public override void Write(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }

        /// <summary>Return false.</summary>
        public override bool CanRead { get { return true; } }

        private byte[] _lastBuffer;
        private int _lastBufferIndex;
        private int _lastBufferLength;
        private Stream _underlyingStream;
        private byte[] _newline;
        private byte[] _stillToOutput;
        private int _stillToOutputIndex;
        private int _stillToOutputLength;
        private bool _ignoreOneLF = false;
        private byte? _singleByteToOutput = null;
        private bool _bigEndian;
        private byte? _lastByteToOutput = null;

        /// <summary>Constructs a <see cref="NewlineNormalizerStream16bit"/>.</summary>
        /// <param name="underlyingStream">Stream to read textual data from.</param>
        /// <param name="newline">Normalised newline to use.</param>
        /// <param name="bigEndian">Specifies whether the byte order is big-endian (true) or little-endian (false).</param>
        public NewlineNormalizerStream16bit(Stream underlyingStream, byte[] newline = null, bool bigEndian = false)
        {
            _newline = newline ?? Environment.NewLine.ToUtf16();
            if (_newline.Length < 1)
                throw new ArgumentException("newline cannot be the empty array.", "newline");
            if (_newline.Length % 2 != 0)
                throw new ArgumentException("newline must have an even number of bytes.", "newline");
            _underlyingStream = underlyingStream;
            _bigEndian = bigEndian;
        }

        private int outputAsMuchAsPossible(ref byte[] fromBuffer, ref int fromOffset, ref int fromCount, byte[] intoBuffer, int intoOffset, int intoCount)
        {
            // Whoa, someone requested a single byte!
            if (intoCount == 1)
            {
                intoBuffer[intoOffset] = fromBuffer[fromOffset];
                _singleByteToOutput = fromBuffer[fromOffset + 1];
                fromOffset += 2;
                fromCount -= 2;
                return 1;
            }

            // Make sure that we play with even numbers only
            if (intoCount % 2 == 1)
                intoCount--;

            if (intoCount < fromCount)
            {
                Buffer.BlockCopy(fromBuffer, fromOffset, intoBuffer, intoOffset, intoCount);
                fromOffset += intoCount;
                fromCount -= intoCount;
                return intoCount;
            }
            else
            {
                Buffer.BlockCopy(fromBuffer, fromOffset, intoBuffer, intoOffset, fromCount);
                fromBuffer = null;
                return fromCount;
            }
        }

        /// <summary>
        /// Reads a sequence of bytes from the current stream and advances the position within the stream
        /// by the number of bytes read.
        /// </summary>
        /// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified
        ///     byte array with the values between offset and (offset + count - 1) replaced
        ///     by the bytes read from the current source.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read
        ///     from the current stream.</param>
        /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
        /// <returns>The total number of bytes read into the buffer. This can be less than the
        ///     number of bytes requested if that many bytes are not currently available,
        ///     or zero (0) if the end of the stream has been reached.</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (count < 1)
                throw new ArgumentOutOfRangeException("count", "count cannot be zero or negative.");

            if (_singleByteToOutput != null)
            {
                buffer[offset] = _singleByteToOutput.Value;
                _singleByteToOutput = null;
                return 1;
            }

            if (_stillToOutput != null && _stillToOutputLength > 0)
                return outputAsMuchAsPossible(ref _stillToOutput, ref _stillToOutputIndex, ref _stillToOutputLength, buffer, offset, count);

            if (_lastBuffer != null && _lastBufferLength > 0)
            {
                var index = _lastBufferIndex;
                if (_bigEndian)
                {
                    while (index < _lastBufferIndex + _lastBufferLength && (_lastBuffer[index] != 0 || _lastBuffer[index + 1] != 10) && (_lastBuffer[index] != 0 || _lastBuffer[index + 1] != 13))
                        index += 2;
                }
                else
                {
                    while (index < _lastBufferIndex + _lastBufferLength && (_lastBuffer[index] != 10 || _lastBuffer[index + 1] != 0) && (_lastBuffer[index] != 13 || _lastBuffer[index + 1] != 0))
                        index += 2;
                }

                if (index == _lastBufferIndex + _lastBufferLength)
                    return outputAsMuchAsPossible(ref _lastBuffer, ref _lastBufferIndex, ref _lastBufferLength, buffer, offset, count);

                int nlCount = index - _lastBufferIndex;
                if (nlCount == 0)
                {
                    // Output just the newline
                    _stillToOutput = new byte[_newline.Length];
                    Buffer.BlockCopy(_newline, 0, _stillToOutput, 0, _newline.Length);
                    _stillToOutputIndex = 0;
                    _stillToOutputLength = _newline.Length;
                    var result = outputAsMuchAsPossible(ref _stillToOutput, ref _stillToOutputIndex, ref _stillToOutputLength, buffer, offset, count);

                    byte b = _bigEndian ? _lastBuffer[_lastBufferIndex + 1] : _lastBuffer[_lastBufferIndex];
                    _lastBufferIndex += 2;
                    _lastBufferLength -= 2;

                    // \n is always a single-character newline, but...
                    if (b == 13)
                    {
                        // ... if it's a \r, look at the next character after it
                        if (_lastBufferLength == 0)
                        {
                            // Can't look at the next character yet, remember for later that we need to ignore a \n
                            _ignoreOneLF = true;
                        }
                        else if (_lastBuffer[_lastBufferIndex] == (_bigEndian ? 0 : 10) && _lastBuffer[_lastBufferIndex + 1] == (_bigEndian ? 10 : 0))
                        {
                            _lastBufferIndex += 2;
                            _lastBufferLength -= 2;
                        }
                    }

                    return result;
                }
                else if (nlCount < count)
                {
                    // Output everything up to the newline
                    Buffer.BlockCopy(_lastBuffer, _lastBufferIndex, buffer, offset, nlCount);
                    _lastBufferIndex += nlCount;
                    _lastBufferLength -= nlCount;
                    return nlCount;
                }
                else
                {
                    // Output everything we can
                    return outputAsMuchAsPossible(ref _lastBuffer, ref _lastBufferIndex, ref _lastBufferLength, buffer, offset, count);
                }
            }

            if (_lastBuffer == null)
                _lastBuffer = new byte[65536];
            _lastBufferIndex = 0;
            _lastBufferLength = _underlyingStream.Read(_lastBuffer, 0, 65536);
            if (_lastBufferLength == 0)
            {
                if (_lastByteToOutput != null)
                {
                    buffer[offset] = _lastByteToOutput.Value;
                    _lastByteToOutput = null;
                    return 1;
                }
                return 0;
            }
            while (_lastBufferLength % 2 == 1)
            {
                var bytesRead = _underlyingStream.Read(_lastBuffer, _lastBufferLength, 65536 - _lastBufferLength);
                if (bytesRead == 0)
                    break;
                _lastBufferLength += bytesRead;
            }

            // Handle the case where the input stream contains an odd number of bytes
            if (_lastBufferLength == 1)
            {
                buffer[offset] = _lastBuffer[0];
                _lastBufferLength = 0;
                return 1;
            }
            else if (_lastBufferLength % 2 == 1)
            {
                _lastByteToOutput = _lastBuffer[_lastBufferLength - 1];
                _lastBufferLength--;
            }

            if (_ignoreOneLF)
            {
                if (_lastBuffer[_lastBufferIndex] == (_bigEndian ? 0 : 10) && _lastBuffer[_lastBufferIndex + 1] == (_bigEndian ? 10 : 0))
                {
                    // Note that if _lastBufferLength is 2 and we're taking it down to 0 here, the following
                    // recursive call will still work correctly by reading from the underlying stream again
                    _lastBufferIndex += 2;
                    _lastBufferLength -= 2;
                }
                _ignoreOneLF = false;
            }

            // Now that we have populated _lastBuffer, use a tail-recursive call.
            return Read(buffer, offset, count);
        }
    }
}