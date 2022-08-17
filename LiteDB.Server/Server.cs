using Google.Protobuf;
using LiteDB.Server.Base;
using LiteDB.Server.Base.Protos;
using LiteDB.Server.Logging;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using WellKnown = Google.Protobuf.WellKnownTypes;

namespace LiteDB.Server
{
    /// <summary>
    /// Contains the definition to create a LiteDB Server.
    /// 
    /// After connecting, how to send a command:
    /// resource-name/parameter1/[subresource-name/parameter2]/...:command-name
    /// 
    /// Command names:
    /// - create
    /// - delete
    /// - read
    /// - write
    /// - update
    /// </summary>
    public class Server
    {
        private static readonly MethodInfo m_AnyUnpackMethod = typeof(WellKnown.Any).GetMethod("Unpack")!;

        private readonly TcpListener m_Listener;
        private readonly ConcurrentDictionary<string, Client> m_Clients;
        private readonly ConcurrentDictionary<RouteDefinition, Dictionary<Operation, HandlerExecutor>> m_PathHandlers;

        private Task? _startTask;
        private CancellationTokenSource? m_ListenerCancellationTokenSource;
        private bool m_IsRunning;

        public Server(int port, List<PathHandlerBuilder> handlers)
        {
            m_Listener = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
            m_Clients = new();
            m_PathHandlers = new();

            foreach(var handlerBuilder in handlers)
            {
                m_PathHandlers[handlerBuilder.Route] = handlerBuilder.Handlers
                    .Select(x => KeyValuePair.Create(x.Key, new HandlerExecutor(x.Value)))
                    .ToDictionary(k => k.Key, v => v.Value); 
            }
        }

        #region Methods

        /// <summary>
        /// Starts the server and wait for incoming requests
        /// </summary>
        public Task Run()
        {
            if (m_IsRunning)
                return _startTask!;

            Logger.Log("Starting server...");
            Logger.Log($"Loaded {m_PathHandlers.Count} handler(s).");

            m_IsRunning = true;
            m_Listener.Start();

            m_ListenerCancellationTokenSource = new();
            _startTask = Task.Run(() => AcceptConnections(), m_ListenerCancellationTokenSource.Token);

            Logger.Log("Server started and listening for connections...");

            return _startTask;
        }

        /// <summary>
        /// Stops the listener
        /// </summary>
        public async Task Stop()
        {
            if (_startTask != null) await _startTask;

            foreach (var client in m_Clients)
                client.Value.Close();

            m_ListenerCancellationTokenSource?.Cancel();
            m_ListenerCancellationTokenSource = null;
            m_Clients.Clear();
            m_Listener.Stop();
            m_IsRunning = false;
        }

        /// <summary>
        /// Send data to the specified client by id asynchronously.
        /// </summary>
        /// <param name="id">The client id.</param>
        /// <param name="contentLength">The number of bytes to read from the source stream to send.</param>
        /// <param name="buffer">Stream containing the data to send.</param>
        /// <param name="token">Cancellation token for canceling the request.</param>
        public async Task SendAsync(string id, byte[] buffer, CancellationToken token = default)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            using MemoryStream ms = new();
            await ms.WriteAsync(buffer, token).ConfigureAwait(false);
            ms.Seek(0, SeekOrigin.Begin);
            await SendInternalAsync(id, buffer.Length, ms, token).ConfigureAwait(false);
        }

        #endregion

        #region Private Methods

        private async void OnDataReady(Client client, byte[] buffer)
        {
            var cmd = Command.Parser.ParseFrom(buffer);
            HandlerExecutor? handlerExecutor = null;
            CommandContext? context = null;

            foreach(var handlerEntry in m_PathHandlers)
            {
                var result = handlerEntry.Key.ParseRouteInstance(cmd.Path);
                if (result == null)
                    continue;

                handlerExecutor = handlerEntry.Value[result.Operation];
                context = new CommandContext(cmd, result.Parameters.ToDictionary(x => x.Key, x => x.Value));
                Logger.Log($"Handler found for path {cmd.Path}. Parameters: [{string.Join(",", result.Parameters.Select(b => $"{b.Key}: ${b.Value}"))}]");
            }

            if (handlerExecutor == null)
                throw new Exception("Unknown path: " + cmd.Path);

            CommandResult commandResult;

            // Needs data
            if (handlerExecutor.NeedsData)
            {
                if (cmd!.Data == null)
                    throw new Exception("Expected data, received null.");

                var unpackMethod = m_AnyUnpackMethod.MakeGenericMethod(new Type[] { handlerExecutor.DataType! });

                try
                {
                    // Unpack data to given type
                    var unpackedData = unpackMethod.Invoke(cmd.Data, null)!;

                    // Call handler
                    commandResult = handlerExecutor.Handle(context!, unpackedData);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Expected data be of type {handlerExecutor.DataType!.Name}, given object with TypeUrl: {cmd.Data.TypeUrl}", ex);
                }
            }

            // Skips data
            else
            {
                // Call handler
                commandResult = handlerExecutor.Handle(context!);
            }

            // Send response
            var responseBuffer = commandResult.ToByteArray();
            await SendAsync(client.Id, responseBuffer);
        }

        private async Task AcceptConnections()
        {
            while (!m_ListenerCancellationTokenSource!.IsCancellationRequested)
            {
                TcpClient tcpClient = await m_Listener.AcceptTcpClientAsync().ConfigureAwait(false);
                var client = new Client(tcpClient);

                bool added = m_Clients.TryAdd(client.Id, client);
                if(added)
                {
                    Logger.Log($"Client {client.Ip} connected. Id [{client.Id}]");
                    Task unawaited = Task.Run(() => DataReceiver(client), m_ListenerCancellationTokenSource.Token);
                }
            }
        }

        private async Task DataReceiver(Client client)
        {
            while(true)
            {
                try
                {
                    if (!client.Connected)
                        break;

                    // TODO: Handle cancellation token

                    Logger.Log($"Reading incoming message from client {client.Id}");

                    // Read buffer
                    var buffer = await ReadBuffer(client, m_ListenerCancellationTokenSource!.Token);
                    if (buffer == null)
                    {
                        await Task.Delay(10, m_ListenerCancellationTokenSource!.Token).ConfigureAwait(false);
                        continue;
                    }

                    _ = Task.Run(() => OnDataReady(client, buffer));
                }
                catch
                {
                    break;
                }
            }
        }

        private static async Task<byte[]> ReadBuffer(Client client, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[2048];
            int read;

            using MemoryStream ms = new();
            while (true)
            {
                read = await client.NetworkStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);

                if (read > 0)
                {
                    await ms.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                    return ms.ToArray();
                }
                else
                    throw new SocketException();
            }
        }

        private async Task SendInternalAsync(string clientId, long contentLength, Stream stream, CancellationToken token)
        {
            Client? client = null;
            try
            {
                if (!m_Clients.TryGetValue(clientId, out client)) return;
                if (client == null) return;

                long bytesRemaining = contentLength;
                int bytesRead = 0;
                byte[] buffer = new byte[2048];

                await client.SendLock.WaitAsync(token).ConfigureAwait(false);

                while (bytesRemaining > 0)
                {
                    bytesRead = await stream.ReadAsync(buffer, token).ConfigureAwait(false);
                    if (bytesRead > 0)
                    {
                        await client.NetworkStream.WriteAsync(buffer, 0, bytesRead, token).ConfigureAwait(false);

                        bytesRemaining -= bytesRead;
                    }
                }

                await client.NetworkStream.FlushAsync(token).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {

            }
            catch (OperationCanceledException)
            {

            }
            finally
            {
                if (client != null) 
                    client.SendLock.Release();
            }
        }

        #endregion
    }
}