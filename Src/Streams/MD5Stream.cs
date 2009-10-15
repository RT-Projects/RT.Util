using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace RT.Util.Streams
{
    /// <summary>
    /// Calculates MD5 checksum of all values that are read/written via this stream.
    /// </summary>
    public class MD5Stream : Stream
    {
        private Stream _stream = null;
        private System.Security.Cryptography.MD5 _md5;
        private byte[] _result;

        /// <summary>
        /// This is the underlying stream. All reads/writes and most other operations
        /// on this class are performed on this underlying stream.
        /// </summary>
        public virtual Stream BaseStream { get { return _stream; } }

        private MD5Stream() { }

        /// <summary>
        /// Initialises an MD5 calculation stream, with the specified stream as the
        /// underlying stream.
        /// </summary>
        public MD5Stream(Stream stream)
        {
            _stream = stream;
            _md5 = System.Security.Cryptography.MD5.Create("MD5");
            _result = null;
        }

#pragma warning disable 1591    // Missing XML comment for publicly visible type or member
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

        /// <summary>
        /// Seeking is ignored (but propagated to the underlying stream).
        /// All the bytes seeked over will be ignored.
        /// </summary>
        public override long Seek(long offset, SeekOrigin origin)
        {
            return _stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _stream.SetLength(value);
        }
#pragma warning restore 1591    // Missing XML comment for publicly visible type or member

        /// <summary>
        /// Reads data from the underlying stream. Updates the MD5 with the bytes read.
        /// </summary>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_md5 == null)
                throw new InvalidOperationException("MD5 stream cannot hash further data since the value of the hash has been read already.");

            int numread = _stream.Read(buffer, offset, count);
            _md5.TransformBlock(buffer, offset, numread, buffer, offset);

            return numread;
        }

        /// <summary>
        /// Writes data to the underlying stream. Updates the MD5 with the bytes written.
        /// </summary>
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_md5 == null)
                throw new InvalidOperationException("MD5 stream cannot hash further data since the value of the hash has been read already.");

            _stream.Write(buffer, offset, count);
            _md5.TransformBlock(buffer, offset, count, buffer, offset);
        }

        /// <summary>
        /// Retrieves the MD5 hash of all data that has been read/written through this
        /// stream so far. Due to the implementation of the underlying MD5 algorithm this
        /// must be called only after all data has been hashed. No further calls to Read/Write
        /// are allowed after a single call to this.
        /// </summary>
        public byte[] MD5
        {
            get
            {
                if (_md5 != null)
                {
                    _md5.TransformFinalBlock(new byte[] { }, 0, 0);
                    _result = _md5.Hash;
                }

                return _result;
            }
        }
    }

}
