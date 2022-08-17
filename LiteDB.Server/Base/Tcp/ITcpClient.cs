namespace LiteDB.Server.Base.Tcp
{
    /// <summary>
    /// Base abstract class for representing a TCP Client
    /// </summary>
    public interface ITcpClient
    {
        #region Properties

        /// <summary>
        /// The id of the client.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// The ip of the client.
        /// </summary>
        public string Ip { get; }

        /// <summary>
        /// A flag that indicates if the client is connected.
        /// </summary>
        public bool Connected { get; }

        /// <summary>
        /// An stream used to read and write messages.
        /// </summary>
        public Stream Stream { get; }

        #endregion

        #region Methods

        /// <summary>
        /// Closes the connection to a client
        /// </summary>
        public void Close();

        /// <summary>
        /// Sends a buffer to the client
        /// </summary>
        /// <param name="stream">The stream to send.</param>
        /// <param name="contentLength">The content length of the stream.</param>
        public Task SendBufferAsync(Stream stream, long contentLength, CancellationToken token);

        /// <summary>
        /// Reads an incoming buffer from the client.
        /// </summary>
        public Task<byte[]> ReadBufferAsync(CancellationToken cancellationToken);

        #endregion
    }
}