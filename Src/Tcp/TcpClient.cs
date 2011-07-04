using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;

namespace RT.KitchenSink.Tcp
{
    /// <summary>Provides a TCP client that can monitor an existing TCP connection for incoming data
    /// and will raise events (callback functions) when data is received or the connection is closed.</summary>
    public sealed class TcpClientWithEvents
    {
        private Socket _socket;
        private Thread _readingThread;

        /// <summary>Constructs a TCP client based on an existing Socket.</summary>
        /// <param name="socket">The Socket to monitor for incoming data.</param>
        public TcpClientWithEvents(Socket socket)
        {
            _socket = socket;
            _readingThread = new Thread(readingThreadFunction);
            _readingThread.Start();
        }

        /// <summary>Specifies whether the TCP client is actively monitoring the Socket connection.</summary>
        public bool IsActive { get { return _readingThread != null && _readingThread.IsAlive; } }

        /// <summary>Event raised when data comes in.</summary>
        public event DataEventHandler IncomingData;
        
        /// <summary>Event raised when the connection is closed.</summary>
        public event EventHandler ConnectionClose;

        private void readingThreadFunction()
        {
            while (true)
            {
                byte[] buffer = new byte[65536];
                int bytesReceived = _socket.Receive(buffer);
                if (bytesReceived == 0)
                {
                    if (ConnectionClose != null)
                        ConnectionClose(this, new EventArgs());
                    return;
                }
                if (IncomingData != null)
                    IncomingData(this, buffer, bytesReceived);
            }
        }

        /// <summary>Closes the Socket connection and stops monitoring the connection.</summary>
        public void Close()
        {
            if (_readingThread != null && _readingThread.IsAlive)
                _readingThread.Abort();
            _socket.Close();
            _socket = null;
            _readingThread = null;
        }

        /// <summary>Method directly forwarded to the underlying socket.</summary>
        public int Send(byte[] buffer) { return _socket.Send(buffer); }
        /// <summary>Method directly forwarded to the underlying socket.</summary>
        public int Send(IList<ArraySegment<byte>> buffers) { return _socket.Send(buffers); }
        /// <summary>Method directly forwarded to the underlying socket.</summary>
        public int Send(byte[] buffer, SocketFlags socketFlags) { return _socket.Send(buffer, socketFlags); }
        /// <summary>Method directly forwarded to the underlying socket.</summary>
        public int Send(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags) { return _socket.Send(buffers, socketFlags); }
        /// <summary>Method directly forwarded to the underlying socket.</summary>
        public int Send(byte[] buffer, int size, SocketFlags socketFlags) { return _socket.Send(buffer, size, socketFlags); }
        /// <summary>Method directly forwarded to the underlying socket.</summary>
        public int Send(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, out SocketError errorCode) { return _socket.Send(buffers, socketFlags, out errorCode); }
        /// <summary>Method directly forwarded to the underlying socket.</summary>
        public int Send(byte[] buffer, int offset, int size, SocketFlags socketFlags) { return _socket.Send(buffer, offset, size, socketFlags); }
        /// <summary>Method directly forwarded to the underlying socket.</summary>
        public int Send(byte[] buffer, int offset, int size, SocketFlags socketFlags, out SocketError errorCode) { return _socket.Send(buffer, offset, size, socketFlags, out errorCode); }
    }
}
