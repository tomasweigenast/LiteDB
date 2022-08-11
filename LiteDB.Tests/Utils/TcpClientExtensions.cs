using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace LiteDB.Tests.Utils
{
    public static class TcpClientExtensions
    {
        public static async Task<byte[]> DataReadAsync(this TcpClient client, CancellationToken token)
        {
            byte[] buffer = new byte[2048];
            int read;

            try
            {
                read = await client.GetStream().ReadAsync(buffer, token).ConfigureAwait(false);

                if (read > 0)
                {
                    using MemoryStream ms = new MemoryStream();
                    ms.Write(buffer, 0, read);
                    return ms.ToArray();
                }
                else
                    throw new SocketException();
            }
            catch (IOException)
            {
                // thrown if ReadTimeout (ms) is exceeded
                // see https://docs.microsoft.com/en-us/dotnet/api/system.net.sockets.networkstream.readtimeout?view=net-6.0
                // and https://github.com/dotnet/runtime/issues/24093
                return null;
            }
        }
    }
}