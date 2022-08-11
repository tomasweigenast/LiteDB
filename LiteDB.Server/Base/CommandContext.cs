using LiteDB.Server.Base.Protos;

namespace LiteDB.Server.Base
{
    /// <summary>
    /// A class that holds information of incoming TcpClient messages
    /// </summary>
    public class CommandContext
    {
        private static readonly Type m_StringType = typeof(string);
        private static readonly Type m_DoubleType = typeof(double);
        private static readonly Type m_FloatType = typeof(float);
        private static readonly Type m_LongType = typeof(long);
        private static readonly Type m_IntType = typeof(int);
        private static readonly Type m_ByteType = typeof(byte);
        private static readonly Type m_BoolType = typeof(bool);

        private readonly Command m_Command;
        private readonly Dictionary<string, string> m_Parameters;

        public string CommandName { get; }

        public CommandContext(Command command, string commandName, Dictionary<string, string> parameters)
        {
            m_Command = command;
            m_Parameters = parameters;

            CommandName = commandName;
        }

        public T GetParameterValue<T>(string parameterName) where T : class, IComparable
        {
            var parameterValue = m_Parameters[parameterName];
            if (parameterValue == null)
                throw new Exception($"Parameter named {parameterName} not found.");

            var type = typeof(T);
            try
            {
                if (type == m_StringType) return (parameterValue as T)!;
                else if (type == m_DoubleType) return (double.Parse(parameterValue) as T)!;
                else if (type == m_FloatType) return (float.Parse(parameterValue) as T)!;
                else if (type == m_IntType) return (int.Parse(parameterValue) as T)!;
                else if (type == m_LongType) return (long.Parse(parameterValue) as T)!;
                else if (type == m_ByteType) return (byte.Parse(parameterValue) as T)!;
                else if (type == m_BoolType) return (parameterValue.ToLower() == "true" ? true as T : false as T)!;
            } 
            catch(Exception ex)
            {
                throw new Exception($"Cannot convert a string to {type}", ex);
            }
            
            throw new Exception($"Don't know how to convert a string to {type}.");
        }
    }
}