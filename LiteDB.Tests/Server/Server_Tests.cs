using FluentAssertions;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using LiteDB.Server.Base;
using LiteDB.Server.Base.Protos;
using LiteDB.Tests.Utils.Mock;
using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace LiteDB.Tests.Server
{
    public class Server_Tests
    {
        private class DataCommandHandler : CommandHandler<Collection>
        {
            public override CommandResult Handle(CommandContext context, Collection data)
            {
                if (data.Name == "crash")
                    throw new Exception("crashed");
                return new CommandResult { Success = true};
            }
        }

        [Fact]
        public void Test_Send_Message_Receive_Success()
        {
            var (server, client) = GetServerAndClient();

            // Create message and get buffer
            var messageBuffer = new Command
            {
                Path = "collections/users:create",
                Data = Any.Pack(new Collection { Name = "users" })
            }.ToByteArray();

            // send message
            server.SetIncomingBuffer(client.Id, messageBuffer);

            // read response
            var buffer = client.ReadBufferAsync(CancellationToken.None).Result;
            var result = CommandResult.Parser.ParseFrom(buffer);

            buffer.Should().NotBeEmpty();
            result.Success.Should().Be(true);
            result.ErrorCode.Should().BeNullOrWhiteSpace();

            client.Close();
        }

        [Fact]
        public void Test_Send_Message_Receive_Fail()
        {
            var (server, client) = GetServerAndClient();

            // Create message and get buffer
            var messageBuffer = new Command
            {
                Path = "collections/users:create",
                Data = Any.Pack(new Collection { Name = "crash" })
            }.ToByteArray();

            // send message
            server.SetIncomingBuffer(client.Id, messageBuffer);

            // read response
            var buffer = client.ReadBufferAsync(CancellationToken.None).Result;
            var result = CommandResult.Parser.ParseFrom(buffer);

            buffer.Should().NotBeEmpty();
            result.Success.Should().Be(false);
            result.ErrorCode.Should().Be("internal-server-error");

            client.Close();
        }

        private static (MockTcpServer server, MockTcpClient client) GetServerAndClient()
        {
            var tcpServer = new MockTcpServer();
            var server = new LiteDB.Server.Server(tcpServer, new List<PathHandlerBuilder>
                {
                    new PathHandlerBuilder("collections/{collectionName}", new Dictionary<Operation, ICommandHandler>
                    {
                        { Operation.Create, new DataCommandHandler() }
                    })
                });
            server.RunAsync().Wait();

            // Create a mock client
            var client = new MockTcpClient();
            tcpServer.ConnectClient(client);

            return (tcpServer, client);
        }
    }
}
