using LiteDB.Server.Base.Tcp;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LiteDB.Tests.Utils.Mock
{
    public class MockTcpClient : ITcpClient
    {
        private MemoryStream _stream;
        private bool _connected;

        public string Id { get; }

        public string Ip { get; }

        public bool Connected => _connected;

        public Stream Stream => _stream;

        public MockTcpClient()
        {
            Id = Guid.NewGuid().ToString("n");
            Ip = Guid.NewGuid().ToString("n");
            _stream = new MemoryStream();
            _connected = true;
        }

        public void Close()
        {
            _stream = null;
            _connected = false;
        }

        public Task<byte[]> ReadBufferAsync(CancellationToken cancellationToken)
        {
            _stream.Position = 0;
            return Task.FromResult(_stream.ToArray());
        }

        public Task SendBufferAsync(Stream stream, long contentLength, CancellationToken token)
        {
            _stream.SetLength(0);
            _stream.Position = 0;
            return stream.CopyToAsync(_stream, token);
        }

        public void SetConnected(bool connected)
        {
            _connected = connected;
        }

        public void SetStreamContent(Stream stream)
        {
            _stream.Position = 0;
            _stream.SetLength(0);
            stream.CopyTo(_stream);
        }
    }
}
