using LiteDB.Server.Base.Protos;
using System.Reflection;

namespace LiteDB.Server.Base
{
    public class PathHandler
    {
        #region Members

        private readonly Type m_HandlerType;
        private readonly MethodInfo m_HandlerHandleMethod;

        #endregion

        #region Properties

        public RouteDefinition Path { get; }

        public ICommandHandler Handler { get; }

        public bool NeedsData => DataType != null;

        public Type? DataType { get; }

        #endregion

        public PathHandler(RouteDefinition path, ICommandHandler handler)
        {
            Path = path;
            Handler = handler;

            m_HandlerType = handler.GetType();
            m_HandlerHandleMethod = m_HandlerType.GetMethod("Handle")!;
            DataType = m_HandlerType.BaseType!.GenericTypeArguments[0];
        }

        public CommandResult Handle(params object[] arguments) => (CommandResult)m_HandlerHandleMethod.Invoke(Handler, arguments)!;
    }
}