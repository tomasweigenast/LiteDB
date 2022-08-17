using LiteDB.Server.Base.Tcp;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LiteDB.Tests.Utils.Mock
{
    public class MockTcpServer : ITcpServer
    {
        private bool _running;
        private ConcurrentDictionary<string, ITcpClient> _clients;

        public bool IsRunning => _running;

        public event ServerEvents.BufferReceived OnBufferReceived;

        public MockTcpServer()
        {
            _running = false;
            _clients = new();
        }

        public ITcpClient GetClient(string clientId)
            => _clients[clientId];

        public Task SendBufferAsync(ITcpClient client, Stream stream, long contentLength, CancellationToken cancellationToken)
        {
            return client.SendBufferAsync(stream, contentLength, cancellationToken); 
        }

        public Task StartAsync()
        {
            _running = true;
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            _running = false;
            _clients.Clear();
            return Task.CompletedTask;
        }

        public void SetIncomingBuffer(string clientId, byte[] buffer)
        {
            OnBufferReceived?.Invoke(_clients[clientId], buffer);
        }

        public void ConnectClient(ITcpClient client)
        {
            _clients[client.Id] = client;
        }
    }
}