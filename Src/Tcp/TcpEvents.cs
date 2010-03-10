
namespace RT.KitchenSink.Tcp
{
    /// <summary>
    /// Provides a callback function to call when a <see cref="TcpClientWithEvents"/> or a <see cref="TcpServer"/> receives data.
    /// </summary>
    /// <param name="sender">The <see cref="TcpClientWithEvents"/> that received the data. Note that <see cref="TcpServer"/> passes the <see cref="TcpServer.IncomingData"/> event
    /// to each individual connection, which is represented as a <see cref="TcpClientWithEvents"/>.</param>
    /// <param name="data">A buffer containing the data received. The buffer may be larger than the received data.</param>
    /// <param name="bytesReceived">The number of bytes received. The data is located at the beginning of the <paramref name="data"/> array.</param>
    public delegate void DataEventHandler(object sender, byte[] data, int bytesReceived);

    /// <summary>
    /// Provides a callback function to call when a <see cref="TcpServer"/> receives a new incoming TCP connection.
    /// </summary>
    /// <param name="sender">The <see cref="TcpServer"/> that received the incoming connection.</param>
    /// <param name="socket">An object encapsulating the new connection.</param>
    public delegate void ConnectionEventHandler(object sender, TcpClientWithEvents socket);
}
