using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using RT.Util.ExtensionMethods;

namespace RT.Util.Streams
{
    /// <summary>Provides functionality to replace the three different newlines (\r, \n, and \r\n) while reading from a stream containing textual data,
    /// without caring about the text encoding (as long as the newlines are single bytes).</summary>
    public class NormalizeNewlinesStream : Stream
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

        public NormalizeNewlinesStream(Stream underlyingStream, byte[] newline = null)
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
}