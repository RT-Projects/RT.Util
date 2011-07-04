using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace RT.Util.Streams
{
    /// <summary>
    /// Use this if you need to write to a <see cref="Stream"/> but actually want the output sent to a <see cref="System.Net.Sockets.Socket"/>.
    /// </summary>
    public class SocketWriterStream : Stream
    {
        /// <summary>Contains the underlying socket.</summary>
        protected Socket Socket;

        /// <summary>
        /// Constructs a <see cref="SocketWriterStream"/> object that can send output to a <see cref="System.Net.Sockets.Socket"/>.
        /// </summary>
        /// <param name="socket">The socket to write all the output to.</param>
        public SocketWriterStream(Socket socket)
        {
            this.Socket = socket;
        }

        /// <summary>
        /// Writes the specified data to the underlying <see cref="Socket"/>.
        /// </summary>
        /// <param name="buffer">Buffer containing the data to be written.</param>
        /// <param name="offset">Buffer offset starting at which data is obtained.</param>
        /// <param name="count">Number of bytes to read from buffer and send to the socket.</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            Socket.Send(buffer, offset, count, SocketFlags.None);
        }

#pragma warning disable 1591    // Missing XML comment for publicly visible type or member

        // Stuff you can't do
        public override bool CanRead { get { return false; } }
        public override bool CanSeek { get { return false; } }
        public override bool CanWrite { get { return true; } }
        public override void Flush() { }

        public override long Length { get { throw new System.NotSupportedException(); } }
        public override long Position
        {
            get { throw new System.NotSupportedException(); }
            set { throw new System.NotSupportedException(); }
        }
        public override int Read(byte[] buffer, int offset, int count) { throw new System.NotSupportedException(); }
        public override long Seek(long offset, SeekOrigin origin) { throw new System.NotSupportedException(); }
        public override void SetLength(long value) { throw new System.NotSupportedException(); }

#pragma warning restore 1591    // Missing XML comment for publicly visible type or member

    }

    /// <summary>
    /// Same as <see cref="SocketWriterStream"/>, but performs the HTTP Transfer-Encoding: chunked.
    /// </summary>
    public sealed class ChunkedSocketWriterStream : SocketWriterStream
    {
        /// <summary>
        /// Constructs a <see cref="ChunkedSocketWriterStream"/> object that can send output to a
        /// <see cref="Socket"/> in HTTP "chunked" Transfer-Encoding.
        /// </summary>
        /// <param name="socket">The socket to write all the output to.</param>
        public ChunkedSocketWriterStream(Socket socket)
            : base(socket)
        {
            Socket = socket;
        }

        /// <summary>
        /// Writes the specified data to the underlying <see cref="Socket"/> as a single chunk.
        /// </summary>
        /// <param name="buffer">Buffer containing the data to be written.</param>
        /// <param name="offset">Buffer offset starting at which data is obtained.</param>
        /// <param name="count">Number of bytes to read from buffer and send to the socket.</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            Socket.Send(Encoding.UTF8.GetBytes(count.ToString("X") + "\r\n"));
            Socket.Send(buffer, offset, count, SocketFlags.None);
            Socket.Send(new byte[] { 13, 10 }); // "\r\n"
        }

        /// <summary>
        /// Closes this <see cref="ChunkedSocketWriterStream"/>. It is important that this is called
        /// because it outputs the trailing null chunk to the socket, indicating the end of the data.
        /// </summary>
        public override void Close()
        {
            Socket.Send(new byte[] { (byte) '0', 13, 10, 13, 10 }); // "0\r\n\r\n"
        }
    }

    /// <summary>
    /// Use this if you need to read from a <see cref="Stream"/> but actually want the input read from a <see cref="System.Net.Sockets.Socket"/>.
    /// </summary>
    public sealed class SocketReaderStream : Stream
    {
        private Socket _socket;
        private long _maxBytesToRead;
        private byte[] _lastRead;
        private int _lastReadOffset;
        private int _lastReadCount;
        private bool _myBuffer;

        /// <summary>
        /// Constructs a <see cref="SocketReaderStream"/> object that can read input from a <see cref="System.Net.Sockets.Socket"/>.
        /// </summary>
        /// <param name="socket">The socket to read the input from.</param>
        /// <param name="maxBytesToRead">Maximum number of bytes to read from the socket. After this, the stream pretends to have reached the end.</param>
        public SocketReaderStream(Socket socket, long maxBytesToRead)
        {
            _socket = socket;
            _maxBytesToRead = maxBytesToRead;
            _lastRead = null;
            _lastReadOffset = 0;
            _lastReadCount = 0;
            _myBuffer = false;
        }

        /// <summary>
        /// Constructs a <see cref="SocketReaderStream"/> object that reads from a given bit of initial data, and then continues on to read from a <see cref="System.Net.Sockets.Socket"/>.
        /// </summary>
        /// <param name="socket">The socket to read the input from.</param>
        /// <param name="maxBytesToRead">Maximum number of bytes to read from the initial data plus the socket. After this, the stream pretends to have reached the end.</param>
        /// <param name="initialBuffer">Buffer containing the initial data. The buffer is not copied, so make sure you don't modify the contents of the buffer before it is consumed by reading.</param>
        /// <param name="initialBufferOffset">Offset into the buffer where the initial data starts.</param>
        /// <param name="initialBufferCount">Number of bytes of initial data in the buffer.</param>
        public SocketReaderStream(Socket socket, long maxBytesToRead, byte[] initialBuffer, int initialBufferOffset, int initialBufferCount)
        {
            _socket = socket;
            _myBuffer = false;

            if (initialBufferCount <= 0)
            {
                _lastRead = null;
                _lastReadOffset = 0;
                _lastReadCount = 0;
                _maxBytesToRead = maxBytesToRead;
            }
            else if (initialBufferCount > maxBytesToRead)
            {
                _lastRead = initialBuffer;
                _lastReadOffset = initialBufferOffset;
                // The conversion to int here is safe because we know it's smaller than initialBufferCount, which is an int
                _lastReadCount = (int) maxBytesToRead;
                _maxBytesToRead = 0;
            }
            else
            {
                _lastRead = initialBuffer;
                _lastReadOffset = initialBufferOffset;
                _lastReadCount = initialBufferCount;
                _maxBytesToRead = maxBytesToRead - initialBufferCount;
            }
        }

#pragma warning disable 1591    // Missing XML comment for publicly visible type or member
        public override bool CanRead { get { return true; } }
        public override bool CanSeek { get { return false; } }
        public override bool CanWrite { get { return false; } }
        public override void Flush() { }
        public override long Length { get { throw new NotSupportedException(); } }
        public override long Position { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }
        public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
        public override void SetLength(long value) { throw new NotSupportedException(); }
        public override void Write(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }
#pragma warning restore 1591    // Missing XML comment for publicly visible type or member

        /// <summary>Reads up to the specified number of bytes from the underlying socket.</summary>
        /// <param name="buffer">Buffer to write received data into.</param>
        /// <param name="offset">Offset into the buffer where to start writing.</param>
        /// <param name="count">Maximum number of bytes in the buffer to write to.</param>
        /// <returns>Number of bytes actually written to the buffer.</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            // If we have something left from the last socket-receive operation, return as much of that as possible
            if (_lastRead != null && _lastReadCount > 0)
            {
                if (count >= _lastReadCount)
                {
                    Buffer.BlockCopy(_lastRead, _lastReadOffset, buffer, offset, _lastReadCount);
                    if (_myBuffer)
                    {
                        var tmp = _lastReadCount;
                        _lastReadCount = 0;
                        return tmp;
                    }
                    _lastRead = null;
                    return _lastReadCount;
                }
                else
                {
                    Buffer.BlockCopy(_lastRead, _lastReadOffset, buffer, offset, count);
                    _lastReadOffset += count;
                    _lastReadCount -= count;
                    return count;
                }
            }
            else
            {
                // If we have read as many bytes as we are supposed to, simulate the end of the stream
                if (_maxBytesToRead <= 0)
                    return 0;

                if (!_myBuffer)
                {
                    // Read at most _maxBytesToRead bytes from the socket
                    _lastRead = new byte[(int) Math.Min(65536, _maxBytesToRead)];
                    _myBuffer = true;
                }
                _lastReadOffset = 0;
                _lastReadCount = _socket.Receive(_lastRead, _lastReadOffset, (int) Math.Min(_lastRead.Length, _maxBytesToRead), SocketFlags.None);
                _maxBytesToRead -= _lastReadCount;

                // Socket error?
                if (_lastReadCount == 0)
                {
                    _lastRead = null;
                    _maxBytesToRead = 0;
                    return 0;
                }

                // We’ve populated _lastRead; use a tail-recursive call to actually return the data
                return Read(buffer, offset, count);
            }
        }
    }
}
