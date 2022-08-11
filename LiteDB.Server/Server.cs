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
    /// resource-name:[parameter1,parameter2,parameter3]:command-name
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
        private static readonly MethodInfo m_HandleDatalessMethod = typeof(CommandHandler).GetMethod("Handle")!;
        private static readonly MethodInfo m_HandleDataMethod = typeof(CommandHandler<>).GetMethod("Handle")!;

        private readonly TcpListener m_Listener;
        private readonly ConcurrentDictionary<string, Client> m_Clients;
        private readonly ConcurrentDictionary<string, PathHandler> m_PathHandlers;

        private Task? _startTask;
        private CancellationTokenSource? m_ListenerCancellationTokenSource;
        private bool m_IsRunning;

        public Server(int port, List<PathHandler> handlers)
        {
            m_Listener = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
            m_Clients = new ConcurrentDictionary<string, Client>();
            m_PathHandlers = new ConcurrentDictionary<string, PathHandler>(handlers.Select(x => KeyValuePair.Create(x.Path.ToString(), x)));
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
        
        #endregion

        #region Private Methods

        private void OnDataReady(Client _, byte[] buffer)
        {
            var cmd = Command.Parser.ParseFrom(buffer);
            ICommandHandler? commandHandler = null;
            CommandContext? context = null;

            foreach(var handler in m_PathHandlers.Values)
            {
                var parameters = handler.Path.ParseRouteInstance(cmd.Path);
                if (parameters == null)
                    continue;

                commandHandler = handler.Handler;
                context = new CommandContext(cmd, parameters.ToDictionary(x => x.Key, x => x.Value));
                Logger.Log($"Handler found for path {cmd.Path}. Parameters: [{string.Join(",", parameters.Select(b => $"{b.Key}: ${b.Value}"))}]");
            }

            if (commandHandler == null)
                throw new Exception("Unknown path: " + cmd.Path);

            var commandHandlerType = commandHandler.GetType();

            // Skips data
            if (!commandHandlerType.ContainsGenericParameters)
            {
                // Call handler
                var commandResult = m_HandleDatalessMethod.Invoke(commandHandler, new object[] { context! });
            }

            // Needs data
            else
            {
                var dataType = commandHandlerType.GenericTypeArguments[0]!;

                if (cmd!.Data == null)
                    throw new Exception("Expected data, received null.");

                var unpackMethod = m_AnyUnpackMethod.MakeGenericMethod(new Type[] {dataType});

                try
                {
                    // Unpack data to given type
                    var unpackedData = unpackMethod.Invoke(cmd.Data, null)!;

                    // Call handler
                    var commandResult = m_HandleDataMethod.Invoke(commandHandler, new object[] { context!, unpackedData });
                }
                catch(Exception ex)
                {
                    throw new Exception($"Expected data be of type {dataType.Name}, given object with TypeUrl: {cmd.Data.TypeUrl}", ex);
                }
            }
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
                read = await client.ReadStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);

                if (read > 0)
                {
                    await ms.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                    return ms.ToArray();
                }
                else
                    throw new SocketException();
            }
        }

        #endregion
    }
}