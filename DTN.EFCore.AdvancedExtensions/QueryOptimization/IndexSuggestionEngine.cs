using DTN.EFCore.AdvancedExtensions.QueryAnalysis;
using System.Text.RegularExpressions;

namespace DTN.EFCore.AdvancedExtensions.QueryOptimization
{
    public class IndexSuggestionEngine
    {
        private readonly Dictionary<string, HashSet<string>> _tableColumns;

        public IndexSuggestionEngine()
        {
        }
        public IndexSuggestionEngine(Dictionary<string, HashSet<string>> tableColumns)
        {
            _tableColumns = tableColumns;
        }

        public IEnumerable<string> SuggestIndexes(QueryPlanAnalysis queryPlan)
        {
            var suggestions = new List<string>();

            foreach (var step in queryPlan.Steps)
            {
                suggestions.AddRange(AnalyzeStep(step));
            }

            return suggestions.Distinct();
        }

        private IEnumerable<string> AnalyzeStep(QueryPlanStep step)
        {
            if (step.Detail.Contains("SCAN TABLE") && !step.Detail.Contains("COVERING INDEX"))
            {
                var tableMatch = Regex.Match(step.Detail, @"SCAN TABLE (\w+)");
                if (tableMatch.Success)
                {
                    string tableName = tableMatch.Groups[1].Value;
                    yield return $"Consider adding an index for the scan on table {tableName}";

                    var columnMatches = Regex.Matches(step.Detail, @"(\w+)\s*=\s*\?");
                    if (columnMatches.Count > 0)
                    {
                        var columns = columnMatches.Select(m => m.Groups[1].Value).ToList();
                        yield return $"Suggested index for table {tableName}: ({string.Join(", ", columns)})";
                    }
                }
            }

            if (step.Detail.Contains("SEARCH TABLE"))
            {
                var tableMatch = Regex.Match(step.Detail, @"SEARCH TABLE (\w+)");
                if (tableMatch.Success)
                {
                    string tableName = tableMatch.Groups[1].Value;
                    var columnMatches = Regex.Matches(step.Detail, @"(\w+)\s*=\s*\?");
                    if (columnMatches.Count > 0)
                    {
                        var columns = columnMatches.Select(m => m.Groups[1].Value).ToList();
                        if (columns.Count > 1)
                        {
                            yield return $"Consider a composite index on table {tableName}: ({string.Join(", ", columns)})";
                        }
                    }
                }
            }

            if (step.Detail.Contains("ORDER BY"))
            {
                var tableMatch = Regex.Match(step.Detail, @"ORDER BY .*? ON (\w+)");
                if (tableMatch.Success)
                {
                    string tableName = tableMatch.Groups[1].Value;
                    var columnMatches = Regex.Matches(step.Detail, @"ORDER BY ([\w\s,]+)");
                    if (columnMatches.Count > 0)
                    {
                        var columns = columnMatches[0].Groups[1].Value.Split(',').Select(c => c.Trim()).ToList();
                        yield return $"Consider an index for ORDER BY on table {tableName}: ({string.Join(", ", columns)})";
                    }
                }
            }

            if (step.Detail.Contains("GROUP BY"))
            {
                var tableMatch = Regex.Match(step.Detail, @"GROUP BY .*? ON (\w+)");
                if (tableMatch.Success)
                {
                    string tableName = tableMatch.Groups[1].Value;
                    var columnMatches = Regex.Matches(step.Detail, @"GROUP BY ([\w\s,]+)");
                    if (columnMatches.Count > 0)
                    {
                        var columns = columnMatches[0].Groups[1].Value.Split(',').Select(c => c.Trim()).ToList();
                        yield return $"Consider an index for GROUP BY on table {tableName}: ({string.Join(", ", columns)})";
                    }
                }
            }

            if (step.Detail.Contains("TEMP B-TREE"))
            {
                yield return "Consider optimizing the query to avoid temporary B-tree creation";
            }

            if (step.Detail.Contains("JOIN"))
            {
                var tableMatches = Regex.Matches(step.Detail, @"JOIN (\w+)");
                foreach (Match match in tableMatches)
                {
                    string tableName = match.Groups[1].Value;
                    if (_tableColumns.TryGetValue(tableName, out var columns))
                    {
                        var foreignKeyColumns = columns.Where(c => c.EndsWith("Id") || c.EndsWith("_id")).ToList();
                        foreach (var fkColumn in foreignKeyColumns)
                        {
                            yield return $"Consider an index on the foreign key column {tableName}.{fkColumn}";
                        }
                    }
                }
            }
        }
    }
}
