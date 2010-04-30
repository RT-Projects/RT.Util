using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace RT.KitchenSink.Tcp
{
    /// <summary>
    /// Provides a TCP server which can listen on a TCP port and invoke callback functions when
    /// a new incoming connection is received or when data is received on any active connection.
    /// </summary>
    public sealed class TcpServer
    {
        /// <summary>Constructs a <see cref="TcpServer"/>. Use <see cref="StartListening"/> to activate the server.</summary>
        public TcpServer() { }

        /// <summary>Determines whether the server is currently listening for incoming connections.</summary>
        public bool IsListening { get { return _listeningThread != null && _listeningThread.IsAlive; } }

        /// <summary>Event raised when a new connection comes in.</summary>
        public event ConnectionEventHandler IncomingConnection;

        /// <summary>Event raised when any active connection receives data. Note that the 'sender' parameter is a <see cref="TcpClientWithEvents"/> that represents the active connection.</summary>
        public event DataEventHandler IncomingData;

        private TcpListener _listener;
        private Thread _listeningThread;

        /// <summary>Disables the server, but does not terminate active connections.</summary>
        public void StopListening()
        {
            if (!IsListening)
                return;
            _listeningThread.Abort();
            _listeningThread = null;
            _listener.Stop();
            _listener = null;
        }

        /// <summary>Activates the TCP server and starts listening on the specified port.</summary>
        /// <param name="port">TCP port to listen on.</param>
        /// <param name="blocking">If true, the method will continually wait for incoming connections and never return.
        /// If false, a separate thread is spawned in which the server will listen for incoming connections, 
        /// and control is returned immediately.</param>
        public void StartListening(int port, bool blocking)
        {
            if (IsListening && !blocking)
                return;
            if (IsListening && blocking)
                StopListening();
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            if (blocking)
            {
                listeningThreadFunction();
                _listener.Stop();
                _listener = null;
            }
            else
            {
                _listeningThread = new Thread(listeningThreadFunction);
                _listeningThread.Start();
            }
        }

        private void listeningThreadFunction()
        {
            while (true)
            {
                Socket socket = _listener.AcceptSocket();
                TcpClientWithEvents client = new TcpClientWithEvents(socket);
                if (IncomingData != null)
                    client.IncomingData += IncomingData;
                if (IncomingConnection != null)
                    new Thread(() => IncomingConnection(this, client)).Start();
            }
        }
    }
}
