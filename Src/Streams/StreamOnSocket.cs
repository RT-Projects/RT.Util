using System.IO;
using System.Net.Sockets;
using System.Text;

namespace RT.Util.Streams
{
    /// <summary>
    /// Use this if you need to write to a <see cref="Stream"/> but actually want the output sent to a <see cref="System.Net.Sockets.Socket"/>.
    /// </summary>
    public class StreamOnSocket : Stream
    {
        /// <summary>Contains the underlying socket.</summary>
        protected Socket Socket;

        /// <summary>
        /// Constructs a <see cref="StreamOnSocket"/> object that can send output to a <see cref="System.Net.Sockets.Socket"/>.
        /// </summary>
        /// <param name="socket">The socket to write all the output to.</param>
        public StreamOnSocket(Socket socket)
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
    /// Same as <see cref="StreamOnSocket"/>, but performs the HTTP Transfer-Encoding: chunked.
    /// </summary>
    public class StreamOnSocketChunked : StreamOnSocket
    {
        /// <summary>
        /// Constructs a <see cref="StreamOnSocketChunked"/> object that can send output to a
        /// <see cref="Socket"/> in HTTP "chunked" Transfer-Encoding.
        /// </summary>
        /// <param name="socket">The socket to write all the output to.</param>
        public StreamOnSocketChunked(Socket socket)
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
        /// Closes this <see cref="StreamOnSocketChunked"/>. It is important that this is called
        /// because it outputs the trailing null chunk to the socket, indicating the end of the data.
        /// </summary>
        public override void Close()
        {
            Socket.Send(new byte[] { (byte) '0', 13, 10, 13, 10 }); // "0\r\n\r\n"
        }
    }
}
