using LiteDB.Server.Base;
using LiteDB.Server.Handlers.Collections;

namespace LiteDB.Server
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var server = new Server(9999, new List<PathHandlerBuilder>
            {
                new PathHandlerBuilder("collections/{collectionName}", new Dictionary<Operation, ICommandHandler>
                {
                    { Operation.Create, new CreateCollectionHandler() }
                })
            });

            server.Run().Wait();
        }
    }
}