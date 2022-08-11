using LiteDB.Server.Base;
using LiteDB.Server.Base.Protos;
using LiteDB.Server.Handlers.Collections;

namespace LiteDB.Server
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var server = new Server(9999, new List<PathHandler>
            {
                new PathHandler("collections/{collectionName}", new Dictionary<Operation, ICommandHandler>
                {
                    { Operation.Create, new CreateCollectionHandler() }
                })
            });

            server.Run().Wait();
        }
    }
}