namespace LiteDB.Server.Base
{
    public class PathHandler
    {
        public RouteDefinition Path { get; init; }

        public ICommandHandler Handler { get; init; }

        public PathHandler(RouteDefinition path, ICommandHandler handler)
        {
            Path = path;
            Handler = handler;
        }
    }
}