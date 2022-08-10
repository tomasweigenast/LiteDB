using Google.Protobuf.WellKnownTypes;
using LiteDB.Server.Base.Protos;

namespace LiteDB.Server.Base
{
    /// <summary>
    /// A class that holds information of incoming TcpClient messages
    /// </summary>
    internal class CommandContext
    {
        private readonly byte[] m_Buffer;
        private readonly Command m_Command;

        public CommandReceivedContext(byte[] buffer)
        {
            m_Buffer = buffer;
            m_Command = Command.Parser.ParseFrom(buffer);
        }
    }
}