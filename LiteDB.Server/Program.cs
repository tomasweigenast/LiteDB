using LiteDB;
using LiteDB.Engine;
using LiteDB.Server.Base;

namespace LiteDB.Server
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var parser = new RouteParser("/account/{version}/{userId}");
            var info = parser.ParseRouteInstance("/account/v1/1231564");
            foreach(var pair in info)
            {
                Console.WriteLine($"Key: {pair.Key} Value: {pair.Value}");
            }
        }
    }
}