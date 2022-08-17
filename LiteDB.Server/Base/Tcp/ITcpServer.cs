using static LiteDB.Server.Base.Tcp.ServerEvents;

namespace LiteDB.Server.Base.Tcp
{
    /// <summary>
    /// Represents a base abstraction for a TCP Server
    /// </summary>
    public interface ITcpServer
    {
        #region Properties

        /// <summary>
        /// Returns a flag that indicates if the server is running or not
        /// </summary>
        public bool IsRunning { get; }

        #endregion

        #region Events

        /// <summary>
        /// Method called when a buffer is received from a client.
        /// </summary>
        public event BufferReceived OnBufferReceived;

        #endregion

        #region Methods

        /// <summary>
        /// Starts the server
        /// </summary>
        public Task StartAsync();

        /// <summary>
        /// Stops the server
        /// </summary>
        public Task StopAsync();

        /// <summary>
        /// Gets a connected client by its id
        /// </summary>
        /// <param name="clientId">The id of the client.</param>
        public ITcpClient? GetClient(string clientId);

        /// <summary>
        /// Sends a buffer to a client
        /// </summary>
        public Task SendBufferAsync(ITcpClient client, Stream stream, long contentLength, CancellationToken cancellationToken); 

        #endregion
    }
}