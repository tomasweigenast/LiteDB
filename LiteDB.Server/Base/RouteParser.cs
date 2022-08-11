using System.Text.RegularExpressions;

namespace LiteDB.Server.Base
{
    public class RouteParser
    {
        private const string RouteTokenPattern = @"[{0}].+?[{1}]"; //the 0 and 1 are used by the string.format function, they are the start and end characters.
        private const string VariableTokenPattern = "(?<{0}>[^,]*)"; //the <>'s denote the group name; this is used for reference for the variables later.

        /// <summary>
        /// This is the route template that values are extracted based on.
        /// </summary>
        /// <value>
        /// A string containing variables denoted by the <c>VariableStartChar</c> and the <c>VariableEndChar</c>
        /// </value>
        public string RouteFormat { get; }

        /// <summary>
        /// A hash set of all variable names parsed from the <c>RouteFormat</c>.
        /// </summary>
        public HashSet<string> Variables { get; }

        public RouteParser(string route)
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
        public IEnumerable<KeyValuePair<string, string>> ParseRouteInstance(string routeInstance)
        {
            var inputValues = new Dictionary<string, string>();
            var formatUrl = new string(RouteFormat.ToArray());
            foreach (var variable in Variables)
                formatUrl = formatUrl.Replace(WrapWithVariableChars(variable), string.Format(VariableTokenPattern, variable));

            var regex = new Regex(formatUrl, RegexOptions.IgnoreCase);
            var matchCollection = regex.Match(routeInstance);

            foreach (var variable in Variables)
            {
                var value = matchCollection.Groups[variable].Value;
                inputValues.Add(variable, value);
            }

            return inputValues;
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
    }
}