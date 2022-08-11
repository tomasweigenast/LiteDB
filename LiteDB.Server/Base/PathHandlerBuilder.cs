namespace LiteDB.Server.Base
{
    public class PathHandlerBuilder
    {
        private readonly RouteDefinition m_Route;
        private readonly Dictionary<Operation, ICommandHandler> m_Handlers;

        public RouteDefinition Route => m_Route;
        public Dictionary<Operation, ICommandHandler> Handlers => m_Handlers;

        public PathHandlerBuilder(RouteDefinition route, Dictionary<Operation, ICommandHandler> handlers)
        {
            m_Route = route;
            m_Handlers = handlers;
        }
    }
}
