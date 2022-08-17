namespace LiteDB.Server.Base.Tcp
{
    public static class ServerEvents
    {
        /// <summary>
        /// Method declaration for event when a buffer is received from a client
        /// </summary>
        /// <param name="client">The reference to the client.</param>
        /// <param name="buffer">The buffer received.</param>
        public delegate Task BufferReceived(ITcpClient client, byte[] buffer);
    }
}