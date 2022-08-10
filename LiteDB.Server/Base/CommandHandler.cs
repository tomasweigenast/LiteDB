using LiteDB.Server.Base.Protos;

namespace LiteDB.Server.Base
{
    /// <summary>
    /// The handler of a command operation
    /// </summary>
    internal class CommandHandler
    {
        private readonly Func<CommandContext, CommandResult> m_Handler;

        public CommandHandler(Func<CommandContext, CommandResult> handler)
        {
            m_Handler = handler;
        }
    }

    /// <summary>
    /// The handler of a command operation which handles data too
    /// </summary>
    /// <typeparam name="TData">The type of data the handler handles.</typeparam>
    internal class CommandHandler<TData> : CommandHandler
    {
        private readonly Func<CommandContext, TData, CommandResult> m_Handler;

        public CommandHandler(Func<CommandContext, TData, CommandResult> handler) : base(handler)
        {
            m_Handler = handler;
        }
    }
}