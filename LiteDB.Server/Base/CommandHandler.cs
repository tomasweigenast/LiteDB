using LiteDB.Server.Base.Protos;

namespace LiteDB.Server.Base
{
    public interface ICommandHandler {}

    /// <summary>
    /// The handler of a command operation
    /// </summary>
    public abstract class CommandHandler : ICommandHandler
    {
        public abstract CommandResult Handle(CommandContext context);
    }

    /// <summary>
    /// The handler of a command operation which handles data too
    /// </summary>
    /// <typeparam name="TData">The type of data the handler handles.</typeparam>
    public abstract class CommandHandler<TData> : ICommandHandler
    {
        public abstract CommandResult Handle(CommandContext context, TData data);
    }
}