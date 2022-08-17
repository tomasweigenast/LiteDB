using Google.Protobuf;
using LiteDB.Server.Base;
using LiteDB.Server.Base.Protos;
using LiteDB.Server.Base.Tcp;
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

        private readonly ITcpServer m_Listener;
        private readonly ConcurrentDictionary<RouteDefinition, Dictionary<Operation, HandlerExecutor>> m_PathHandlers;

        public Server(int port, List<PathHandlerBuilder> handlers) : this(new DefaultTcpServer(port), handlers) { }

        public Server(ITcpServer tcpServer, List<PathHandlerBuilder> handlers)
        {
            m_Listener = tcpServer;
            m_PathHandlers = new();

            foreach (var handlerBuilder in handlers)
            {
                m_PathHandlers[handlerBuilder.Route] = handlerBuilder.Handlers
                    .Select(x => KeyValuePair.Create(x.Key, new HandlerExecutor(x.Value)))
                    .ToDictionary(k => k.Key, v => v.Value);
            }

            m_Listener.OnBufferReceived += OnBufferReceived;
        }

        #region Methods

        /// <summary>
        /// Starts the server and wait for incoming requests
        /// </summary>
        public Task RunAsync()
        {
            return m_Listener.StartAsync();
        }

        /// <summary>
        /// Stops the listener
        /// </summary>
        public Task StopAsync()
        {
            return m_Listener.StopAsync();
        }

        #endregion

        #region Private Methods

        private async Task OnBufferReceived(ITcpClient client, byte[] buffer)
        {
            var cmd = Command.Parser.ParseFrom(buffer);
            HandlerExecutor? handlerExecutor = null;
            CommandContext? context = null;

            foreach (var handlerEntry in m_PathHandlers)
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

            try
            {
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
            }
            catch(Exception ex)
            {
                Logger.Log($"An error occured trying to execute command '{cmd.Path}'. Exception:\n{ex}");

                commandResult = new CommandResult
                {
                    Success = false,
                    ErrorCode = "internal-server-error"
                };
            }

            // Send response
            using MemoryStream ms = new();
            commandResult.WriteTo(ms);

            ms.Seek(0, SeekOrigin.Begin);

            await client.SendBufferAsync(ms, ms.Length, CancellationToken.None);
        }

        #endregion
    }
}