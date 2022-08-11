using LiteDB.Server.Base.Protos;
using System.Text.RegularExpressions;

namespace LiteDB.Server.Base
{
    public class RouteDefinition
    {
        private const string RouteTokenPattern = @"[{0}].+?[{1}]"; //the 0 and 1 are used by the string.format function, they are the start and end characters.
        private const string VariableTokenPattern = "(?<{0}>[^,]*)"; //the <>'s denote the group name; this is used for reference for the variables later.

        /// <summary>
        /// This is the route template that values are extracted based on.
        /// </summary>
        public string RouteFormat { get; }

        /// <summary>
        /// A hash set of all variable names parsed from the <c>RouteFormat</c>.
        /// </summary>
        public HashSet<string> Variables { get; }

        public RouteDefinition(string route)
        {
            RouteFormat = route;

            var variableList = new List<string>();
            var matchCollection = Regex.Matches(RouteFormat, string.Format(RouteTokenPattern, '{', '}'), RegexOptions.IgnoreCase);

            foreach (var match in matchCollection)
                variableList.Add(RemoteVariableChars(match.ToString()!));

            Variables = new HashSet<string>(variableList);
        }

        /// <summary>
        /// Extract variable values from a given instance of the route you're trying to parse.
        /// </summary>
        /// <param name="routeInstance">The route instance.</param>
        /// <returns>A dictionary of Variable names mapped to values.</returns>
        public RouteParseResult? ParseRouteInstance(string routeInstance)
        {
            var parts = routeInstance.Split(':', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
                return null;
            
            var command = parts[1];
            routeInstance = parts[0];
            
            var inputValues = new Dictionary<string, string>();
            var formatUrl = new string(RouteFormat.ToArray());
            foreach (var variable in Variables)
                formatUrl = formatUrl.Replace(WrapWithVariableChars(variable), string.Format(VariableTokenPattern, variable));

            var regex = new Regex(formatUrl, RegexOptions.IgnoreCase);
            var matchCollection = regex.Match(routeInstance);

            if (!matchCollection.Success)
                return null;

            foreach (var variable in Variables)
            {
                var value = matchCollection.Groups[variable].Value;
                if (string.IsNullOrWhiteSpace(value))
                    return null;

                inputValues.Add(variable, value);
            }

            return new RouteParseResult(inputValues, Operation.Parse(command));
        }
        
        #region Private Helper Methods

        private static string RemoteVariableChars(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            string result = new(input.ToArray());
            result = result.Replace("{", string.Empty).Replace("}", string.Empty);
            return result;
        }

        private static string WrapWithVariableChars(string input) => $"{{{input}}}";

        #endregion

        public static implicit operator RouteDefinition(string route) => new(route);

        public override string ToString() => RouteFormat;

        public override int GetHashCode() => RouteFormat.GetHashCode();

        public override bool Equals(object? obj) => RouteFormat.Equals(obj);
    }

    public class RouteParseResult
    {
        public Dictionary<string, string> Parameters { get; }

        public Operation Operation { get; }

        public RouteParseResult(Dictionary<string, string> parameters, Operation operation)
        {
            Operation = operation;
            Parameters = parameters;
        }
    }
}