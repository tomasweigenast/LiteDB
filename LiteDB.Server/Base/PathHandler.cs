namespace LiteDB.Server.Base
{
    public class PathHandler
    {
        public string Path { get; init; }

        public ICommandHandler Handler { get; init; }

        public PathHandler(string path, ICommandHandler handler)
        {
            Path = path;
            Handler = handler;
        }
    }
}