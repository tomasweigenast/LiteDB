namespace LiteDB.Server.Base
{
    /// <summary>
    /// A class that holds information of incoming TcpClient messages
    /// </summary>
    internal class MessageReceivedContext
    {
        private readonly byte[] m_Buffer;

        private string m_MessagePath;

        public MessageReceivedContext(byte[] buffer)
        {
            m_Buffer = buffer;
        }
    }
}