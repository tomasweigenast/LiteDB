using FluentAssertions;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using LiteDB.Server.Base;
using LiteDB.Server.Base.Protos;
using LiteDB.Tests.Utils;
using System.Collections.Generic;
using System.Net.Sockets;
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
                return new CommandResult { Success = true};
            }
        }

        [Fact]
        public void Test_Connect_Client_And_Receive_Message()
        {
            var exception = Record.Exception(() =>
            {
                var server = new LiteDB.Server.Server(9999, new List<PathHandlerBuilder>
                {
                    new PathHandlerBuilder("collections/{collectionName}", new Dictionary<Operation, ICommandHandler>
                    {
                        { Operation.Create, new DataCommandHandler() }
                    })
                });

                server.Run();

                var client = new TcpClient("127.0.0.1", 9999);

                var message = new Command
                {
                    Path = "collections/users:create",
                    Data = Any.Pack(new Collection { Name = "users" })
                }.ToByteArray();

                // send message
                client.GetStream().Write(message);

                // read response
                var buffer = client.DataReadAsync(CancellationToken.None).Result;
                var result = CommandResult.Parser.ParseFrom(buffer);

                buffer.Should().NotBeEmpty();
                result.Success.Should().Be(true);

                client.Close();

            });

            exception.Should().BeNull();
        }
    }
}
