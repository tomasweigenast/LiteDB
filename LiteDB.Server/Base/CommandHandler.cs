using LiteDB.Server.Base.Protos;

namespace LiteDB.Server.Base
{
    public interface ICommandHandler { }

    /// <summary>
    /// The handler of a command operation
    /// </summary>
    public class CommandHandler : ICommandHandler
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
    public class CommandHandler<TData> : ICommandHandler
    {
        private readonly Func<CommandContext, TData, CommandResult> m_Handler;

        public CommandHandler(Func<CommandContext, TData, CommandResult> handler)
        {
            m_Handler = handler;
        }
    }
}