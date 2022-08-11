using System.Net.Sockets;

namespace LiteDB.Server.Base
{
    public class Client
    {
        private readonly TcpClient m_TcpClient;

        public string Id { get; init; }

        public string Ip => m_TcpClient.Client.RemoteEndPoint!.ToString()!;

        public bool Connected => IsClientConnected();

        public Socket Socket => m_TcpClient.Client;

        public Stream ReadStream => m_TcpClient.GetStream();

        public Client(TcpClient client)
        {
            Id = Guid.NewGuid().ToString();
            m_TcpClient = client;
        }

        public void Close() => m_TcpClient.Close();

        private bool IsClientConnected()
        {
            if (!m_TcpClient.Connected)
                return false;

            if (m_TcpClient.Client.Poll(0, SelectMode.SelectWrite) && (!m_TcpClient.Client.Poll(0, SelectMode.SelectError)))
            {
                byte[] buffer = new byte[1];
                if (m_TcpClient.Client.Receive(buffer, SocketFlags.Peek) == 0)
                    return false;

                return true;
            }
            else
                return false;
        }
    }
}