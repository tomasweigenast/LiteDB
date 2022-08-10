using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace LiteDB.Server
{
    public class Server
    {
        private readonly TcpListener m_Listener;
        private readonly ConcurrentDictionary<string, TcpClient> m_Clients;

        private Task? _startTask;
        private CancellationTokenSource? m_ListenerCancellationTokenSource;
        private bool m_IsRunning;

        public Server(int port)
        {
            m_Listener = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
            m_Clients = new ConcurrentDictionary<string, TcpClient>();
        }

        /// <summary>
        /// Starts the server and wait for incoming requests
        /// </summary>
        public async Task Run()
        {
            if (m_IsRunning)
                return;

            m_IsRunning = true;
            m_Listener.Start();

            m_ListenerCancellationTokenSource = new();
            _startTask = Task.Run(() => AcceptConnections(), m_ListenerCancellationTokenSource.Token);

            // Reading buffer
            var buffer = new byte[512];

            // Start listening for clients
            while (true)
            {
                var client = await m_Listener.AcceptTcpClientAsync().ConfigureAwait(false);
                var stream = client.GetStream();

                // Read message
                int i;
                while((i = stream.Read(buffer, 0, buffer.Length)) != 0)
                {

                }
            }
        }

        /// <summary>
        /// Stops the listener
        /// </summary>
        public async Task Stop()
        {
            if(_startTask != null) await _startTask;

            foreach (var client in m_Clients)
                client.Value.Close();

            m_ListenerCancellationTokenSource?.Cancel();
            m_ListenerCancellationTokenSource = null;
            m_Clients.Clear();
            m_Listener.Stop();
            m_IsRunning = false;
        }

        #region Private Methods

        private async Task AcceptConnections()
        {
            while (!m_ListenerCancellationTokenSource!.IsCancellationRequested)
            {
                TcpClient tcpClient = await m_Listener.AcceptTcpClientAsync().ConfigureAwait(false);
                string clientIp = tcpClient.Client.RemoteEndPoint!.ToString()!;

                m_Clients.TryAdd(clientIp, tcpClient);

                Task unawaited = Task.Run(() => DataReceiver(tcpClient), m_ListenerCancellationTokenSource.Token);
            }
        }

        private async Task DataReceiver(TcpClient client)
        {
            while(true)
            {
                try
                {
                    if (!IsClientConnected(client))
                        break;

                    // TODO: Handle cancellation token

                    // Read buffer
                    var buffer = await ReadBuffer(client, m_ListenerCancellationTokenSource!.Token);
                    if (buffer == null)
                    {
                        await Task.Delay(10, m_ListenerCancellationTokenSource!.Token).ConfigureAwait(false);
                        continue;
                    }
                }
                catch
                {
                    break;
                }
            }
        }

        private static async Task<byte[]> ReadBuffer(TcpClient client, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[2048];
            int read;

            using MemoryStream ms = new();
            while (true)
            {
                read = await client.GetStream().ReadAsync(buffer, cancellationToken).ConfigureAwait(false);

                if (read > 0)
                {
                    await ms.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                    return ms.ToArray();
                }
                else
                    throw new SocketException();
            }
        }

        private static bool IsClientConnected(TcpClient client)
        {
            if (!client.Connected)
                return false;

            if (client.Client.Poll(0, SelectMode.SelectWrite) && (!client.Client.Poll(0, SelectMode.SelectError)))
            {
                byte[] buffer = new byte[1];
                if (client.Client.Receive(buffer, SocketFlags.Peek) == 0)
                    return false;

                return true;
            }
            else
                return false;
        }

        #endregion
    }
}