namespace LiteDB.Server.Base
{
    public class Operation
    {
        private readonly string m_Name;

        private Operation(string name)
        {
            m_Name = name;
        }

        public static readonly Operation Create = new("create");
        public static readonly Operation Delete = new("delete");
        public static readonly Operation Write = new("write");
        public static readonly Operation Update = new("update");
        public static readonly Operation Read = new("read");
        private static readonly Dictionary<string, Operation> m_Operations = new()
        {
            { "create", Create }
        };

        public static Operation Parse(string operation)
        {

        }

        public override string ToString() => m_Name;
    }
}