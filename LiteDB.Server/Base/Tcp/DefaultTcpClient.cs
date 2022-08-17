using System.Net.Sockets;

namespace LiteDB.Server.Base.Tcp
{
    public class DefaultTcpClient : ITcpClient
    {
        private readonly TcpClient m_TcpClient;
        private readonly Stream m_NetworkStream;
        private readonly SemaphoreSlim m_SendLock = new(1, 1);

        public string Id { get; init; }

        public string Ip => m_TcpClient.Client.RemoteEndPoint!.ToString()!;

        public bool Connected => IsClientConnected();

        public Stream Stream => m_NetworkStream;

        public DefaultTcpClient(TcpClient client)
        {
            Id = Guid.NewGuid().ToString();
            m_TcpClient = client;
            m_NetworkStream = m_TcpClient.GetStream();
        }

        public void Close() => m_TcpClient.Close();

        public async Task SendBufferAsync(Stream stream, long contentLength, CancellationToken token)
        {
            try
            {
                long bytesRemaining = contentLength;
                int bytesRead = 0;
                byte[] buffer = new byte[2048];

                await m_SendLock.WaitAsync(token).ConfigureAwait(false);

                while (bytesRemaining > 0)
                {
                    bytesRead = await stream.ReadAsync(buffer, token).ConfigureAwait(false);
                    if (bytesRead > 0)
                    {
                        await m_NetworkStream.WriteAsync(buffer.AsMemory(0, bytesRead), token).ConfigureAwait(false);

                        bytesRemaining -= bytesRead;
                    }
                }

                await m_NetworkStream.FlushAsync(token).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {

            }
            catch (OperationCanceledException)
            {

            }
            finally
            {
                m_SendLock.Release();
            }
        }

        public async Task<byte[]> ReadBufferAsync(CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[2048];
            int read;

            using MemoryStream ms = new();
            while (true)
            {
                read = await m_NetworkStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);

                if (read > 0)
                {
                    await ms.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                    return ms.ToArray();
                }
                else
                    throw new SocketException();
            }
        }

        private bool IsClientConnected()
        {
            if (!m_TcpClient.Connected)
                return false;

            if (m_TcpClient.Client.Poll(0, SelectMode.SelectWrite) && !m_TcpClient.Client.Poll(0, SelectMode.SelectError))
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