using LiteDB.Server.Base;
using LiteDB.Server.Base.Protos;

namespace LiteDB.Server.Handlers.Collections
{
    public class CreateCollectionHandler : CommandHandler
    {
        public override CommandResult Handle(CommandContext context)
        {
            string collectionName = context.GetParameterValue<string>("collectionName");

            return new();
        }
    }
}