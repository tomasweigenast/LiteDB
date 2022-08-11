using FluentAssertions;
using LiteDB.Server.Base;
using System.Collections.Generic;
using Xunit;

namespace LiteDB.Tests.Server
{
    public class RouteParser_Tests
    {
        [Fact]
        public void Parse_command()
        {
            RouteDefinition template = "collection/{collectionName}/indexes/{indexName}";
            string commandPath = "collection/users/indexes/Idx_Name:delete";

            var result = template.ParseRouteInstance(commandPath);
            
            result.Should().NotBeNull();
            result.Command.Should().Be("delete");
            result.Parameters.Should().Contain(new Dictionary<string, string>
            {
                { "collectionName", "users" },
                { "indexName", "Idx_Name" }
            });
        }

        [Fact]
        public void Parse_command_invalid()
        {
            RouteDefinition template = "collection/{collectionName}/indexes/{indexName}";
            string commandPath = "collection/users/indexes/:delete";

            var result = template.ParseRouteInstance(commandPath);

            result.Should().BeNull();
        }

        [Fact]
        public void Parse_command_missing_command_name()
        {
            RouteDefinition template = "collection/{collectionName}/indexes/{indexName}";
            string commandPath = "collection/users/indexes/Idx_Name";

            var result = template.ParseRouteInstance(commandPath);
            result.Should().BeNull();
        }
    }
}