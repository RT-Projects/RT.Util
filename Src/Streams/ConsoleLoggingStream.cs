using System;
using System.IO;
using RT.Util.ExtensionMethods;

#pragma warning disable 1591  // XML comment

namespace RT.Util.Streams
{
    /// <summary>
    /// Implements a stream that passes through all operations to the underlying stream, but also prints
    /// the bytes read or written to the console.
    /// </summary>
    public class ConsoleLoggingStream : Stream
    {
        private Stream _stream;
        private string _readPrefix, _writePrefix;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="underlyingStream">The underlying stream on which all operations are to be performed.</param>
        /// <param name="readPrefix">If not null, all calls to <see cref="Read"/> will print a single line to the console starting with this prefix and showing a hex dump of the bytes read.</param>
        /// <param name="writePrefix">If not null, all calls to <see cref="Read"/> will print a single line to the console starting with this prefix and showing a hex dump of the bytes written.</param>
        public ConsoleLoggingStream(Stream underlyingStream, string readPrefix, string writePrefix)
        {
            _stream = underlyingStream;
            _readPrefix = readPrefix;
            _writePrefix = writePrefix;
        }

        public override bool CanRead { get { return _stream.CanRead; } }
        public override bool CanSeek { get { return _stream.CanSeek; } }
        public override bool CanWrite { get { return _stream.CanWrite; } }
        public override void Flush() { _stream.Flush(); }
        public override long Length { get { return _stream.Length; } }

        public override long Position
        {
            get { return _stream.Position; }
            set { _stream.Position = value; }
        }

        public override long Seek(long offset, SeekOrigin origin) { return _stream.Seek(offset, origin); }
        public override void SetLength(long value) { _stream.SetLength(value); }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = _stream.Read(buffer, offset, count);
            if (_readPrefix != null)
            {
                var toprint = buffer.Subarray(offset, read);
                Console.WriteLine(_readPrefix + toprint.Length + " bytes: " + toprint.ToHex());
            }
            return read;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _stream.Write(buffer, offset, count);
            if (_writePrefix != null)
            {
                var toprint = buffer.Subarray(offset, count);
                Console.WriteLine(_writePrefix + toprint.Length + " bytes: " + toprint.ToHex());
            }
        }
    }
}
