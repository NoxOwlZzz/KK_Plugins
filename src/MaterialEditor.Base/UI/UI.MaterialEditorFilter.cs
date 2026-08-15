using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MaterialEditorAPI
{
    // A filter pattern belongs to one presentation rebuild. Keeping the Regex
    // here avoids rebuilding it for every renderer, material, or property while
    // also avoiding a process-wide cache that could grow with user input.
    internal sealed class MaterialEditorFilterPattern
    {
        private readonly Regex _regex;

        internal MaterialEditorFilterPattern(string filter)
        {
            MaterialEditorPerformance.Increment(
                MaterialEditorPerformanceMetric.FilterPatternBuilds);
            var pattern =
                "^.*"
                + Regex.Escape(filter).Replace("\\?", ".").Replace("\\*", ".*")
                + ".*$";
            _regex = new Regex(pattern, RegexOptions.IgnoreCase);
        }

        internal bool Matches(string text)
        {
            MaterialEditorPerformance.Increment(
                MaterialEditorPerformanceMetric.FilterMatchCalls);
            var performanceSample = MaterialEditorPerformance.Start(
                MaterialEditorPerformanceMetric.Search);
            try
            {
                return _regex.IsMatch(text);
            }
            finally
            {
                MaterialEditorPerformance.Stop(
                    MaterialEditorPerformanceMetric.Search,
                    performanceSample);
            }
        }
    }

    // Pure production filter shared by the Unity presenter and the standalone
    // harness. This preserves the legacy wildcard and tokenization behavior.
    internal static class MaterialEditorFilter
    {
        private static readonly MaterialEditorFilterPattern[] EmptyPatterns =
            new MaterialEditorFilterPattern[0];

        internal static bool Matches(string text, string filter)
        {
            return Prepare(filter).Matches(text);
        }

        internal static MaterialEditorFilterPattern Prepare(string filter)
        {
            return new MaterialEditorFilterPattern(filter);
        }

        internal static IList<MaterialEditorFilterPattern> Prepare(
            IEnumerable<string> filters)
        {
            var collection = filters as ICollection<string>;
            if (collection != null && collection.Count == 0)
                return EmptyPatterns;
            var result = new List<MaterialEditorFilterPattern>();
            foreach (var filter in filters)
                result.Add(Prepare(filter.Trim()));
            return result;
        }

        internal static void Parse(
            string filter,
            ICollection<string> rendererFilter,
            ICollection<string> propertyFilter)
        {
            if (string.IsNullOrEmpty(filter))
                return;

            var parts = filter.Split(',');
            for (var index = 0; index < parts.Length; index++)
            {
                var part = parts[index].Trim();
                if (string.IsNullOrEmpty(part))
                    continue;
                if (part.StartsWith("_"))
                {
                    var property = part.Trim('_');
                    if (!string.IsNullOrEmpty(property))
                        propertyFilter.Add(property);
                }
                else
                {
                    rendererFilter.Add(part);
                }
            }
        }
    }
}
