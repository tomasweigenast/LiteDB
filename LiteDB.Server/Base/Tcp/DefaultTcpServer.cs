using LiteDB.Server.Logging;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace LiteDB.Server.Base.Tcp
{
    public class DefaultTcpServer : ITcpServer
    {
        #region Members

        private readonly TcpListener m_Listener;
        private readonly ConcurrentDictionary<string, ITcpClient> m_Clients;

        private bool m_IsRunning = false;
        private Task? m_StartTask;
        private CancellationTokenSource? m_ListenerCancellationTokenSource;

        #endregion

        #region Properties

        public bool IsRunning => m_IsRunning;

        #endregion

        #region Events

        public event ServerEvents.BufferReceived OnBufferReceived;

        #endregion

        #region Constructor

        public DefaultTcpServer(int port)
        {
            m_Listener = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
            m_Clients = new ConcurrentDictionary<string, ITcpClient>();
        }

        #endregion

        #region Methods

        public ITcpClient? GetClient(string clientId)
            => m_Clients[clientId];

        public async Task SendBufferAsync(ITcpClient client, Stream stream, long contentLength, CancellationToken cancellationToken)
        {
            await client.SendBufferAsync(stream, contentLength, cancellationToken);
        }

        public Task StartAsync()
        {
            if (m_IsRunning)
                return m_StartTask;

            m_Listener.Start();
            m_IsRunning = true;

            m_ListenerCancellationTokenSource = new();
            m_StartTask = Task.Run(() => AcceptConnections(), m_ListenerCancellationTokenSource.Token);
            return m_StartTask;
        }

        public async Task StopAsync()
        {
            if (!m_IsRunning)
                return;

            if (m_StartTask != null) 
                await m_StartTask;

            foreach (var client in m_Clients)
                client.Value.Close();

            m_ListenerCancellationTokenSource?.Cancel();
            m_ListenerCancellationTokenSource = null;
            m_Clients.Clear();
            m_Listener.Stop();
            m_IsRunning = false;
        }

        #endregion

        #region Helper Methods

        private async Task AcceptConnections()
        {
            while (!m_ListenerCancellationTokenSource!.IsCancellationRequested)
            {
                TcpClient tcpClient = await m_Listener.AcceptTcpClientAsync().ConfigureAwait(false);
                var client = new DefaultTcpClient(tcpClient);

                bool added = m_Clients.TryAdd(client.Id, client);
                if (added)
                {
                    Logger.Log($"Client {client.Ip} connected. Id [{client.Id}]");
                    Task unawaited = Task.Run(() => DataReceiver(client), m_ListenerCancellationTokenSource.Token);
                }
            }
        }

        private async Task DataReceiver(DefaultTcpClient client)
        {
            while (true)
            {
                try
                {
                    if (!client.Connected)
                        break;

                    // TODO: Handle cancellation token

                    Logger.Log($"Reading incoming message from client {client.Id}");

                    // Read buffer
                    var buffer = await client.ReadBufferAsync(m_ListenerCancellationTokenSource!.Token);
                    if (buffer == null)
                    {
                        await Task.Delay(10, m_ListenerCancellationTokenSource!.Token).ConfigureAwait(false);
                        continue;
                    }

                    OnBufferReceived?.Invoke(client, buffer);
                }
                catch
                {
                    break;
                }
            }
        }

        #endregion
    }
}