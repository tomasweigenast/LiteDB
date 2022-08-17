using LiteDB.Server.Base.Protos;
using System.Reflection;

namespace LiteDB.Server.Base
{
    public class HandlerExecutor
    {
        #region Members

        private readonly Type m_HandlerType;
        private readonly MethodInfo m_HandlerHandleMethod;
        private readonly ICommandHandler m_Handler;

        #endregion

        public bool NeedsData => DataType != null;

        public Type? DataType { get; }

        public HandlerExecutor(ICommandHandler handler)
        {
            m_Handler = handler;
            m_HandlerType = handler.GetType();
            m_HandlerHandleMethod = m_HandlerType.GetMethod("Handle")!;
            try { DataType = m_HandlerType.BaseType!.GenericTypeArguments[0]; } catch { }
        }

        public CommandResult Handle(params object[] arguments)
            => (CommandResult)m_HandlerHandleMethod.Invoke(m_Handler, arguments)!;
    }
}