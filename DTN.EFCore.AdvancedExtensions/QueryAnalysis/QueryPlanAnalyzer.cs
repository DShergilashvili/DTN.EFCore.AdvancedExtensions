using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace DTN.EFCore.AdvancedExtensions.QueryAnalysis
{
    public class QueryPlanAnalyzer
    {
        private readonly SqlFormatter _sqlFormatter;

        public QueryPlanAnalyzer()
        {
            _sqlFormatter = new SqlFormatter();
        }

        public async Task<QueryPlanAnalysis> AnalyzeQueryPlanAsync(DbContext context, IQueryable query)
        {
            var sql = query.ToQueryString();
            var formattedSql = _sqlFormatter.Format(sql);
            var connection = context.Database.GetDbConnection();
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = $"EXPLAIN QUERY PLAN {sql}";

            var analysis = new QueryPlanAnalysis { OriginalSql = formattedSql };

            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var planStep = new QueryPlanStep
                    {
                        Id = reader.GetInt32(0),
                        Parent = reader.GetInt32(1),
                        NotUsed = reader.GetInt32(2),
                        Detail = reader.GetString(3)
                    };
                    analysis.Steps.Add(planStep);
                }
            }

            analysis.IdentifyBottlenecks();
            analysis.SuggestOptimizations();
            analysis.EstimateComplexity();

            return analysis;
        }
    }

    public class QueryPlanAnalysis
    {
        public string OriginalSql { get; set; }
        public List<QueryPlanStep> Steps { get; set; } = new List<QueryPlanStep>();
        public List<string> Bottlenecks { get; set; } = new List<string>();
        public List<string> OptimizationSuggestions { get; set; } = new List<string>();
        public int EstimatedComplexity { get; set; }

        public void IdentifyBottlenecks()
        {
            foreach (var step in Steps)
            {
                if (step.Detail.Contains("SCAN TABLE"))
                {
                    Bottlenecks.Add($"Full table scan detected on step {step.Id}");
                }
                if (step.Detail.Contains("TEMP B-TREE"))
                {
                    Bottlenecks.Add($"Temporary B-tree creation detected on step {step.Id}");
                }
                if (step.Detail.Contains("USE TEMP B-TREE"))
                {
                    Bottlenecks.Add($"Usage of temporary B-tree detected on step {step.Id}");
                }
                if (step.Detail.Contains("SUBQUERY"))
                {
                    Bottlenecks.Add($"Subquery detected on step {step.Id}");
                }
            }
        }

        public void SuggestOptimizations()
        {
            foreach (var bottleneck in Bottlenecks)
            {
                if (bottleneck.Contains("Full table scan"))
                {
                    OptimizationSuggestions.Add("Consider adding an index to avoid full table scan");
                }
                if (bottleneck.Contains("Temporary B-tree"))
                {
                    OptimizationSuggestions.Add("Consider optimizing the query to avoid temporary B-tree creation");
                }
                if (bottleneck.Contains("Subquery"))
                {
                    OptimizationSuggestions.Add("Consider rewriting the query to avoid subqueries if possible");
                }
            }

            if (Steps.Any(s => s.Detail.Contains("CROSS JOIN")))
            {
                OptimizationSuggestions.Add("Potential missing JOIN condition detected. Verify all JOINs have proper conditions.");
            }

            if (Steps.Any(s => s.Detail.Contains("INDEX")) && Steps.Any(s => s.Detail.Contains("SEARCH")))
            {
                OptimizationSuggestions.Add("Consider creating a covering index to include all required columns");
            }
        }

        public void EstimateComplexity()
        {
            EstimatedComplexity = Steps.Count;
            EstimatedComplexity += Steps.Count(s => s.Detail.Contains("SCAN TABLE")) * 10;
            EstimatedComplexity += Steps.Count(s => s.Detail.Contains("SEARCH TABLE")) * 2;
            EstimatedComplexity += Steps.Count(s => s.Detail.Contains("SUBQUERY")) * 5;
            EstimatedComplexity += Steps.Count(s => s.Detail.Contains("TEMP B-TREE")) * 3;
        }
    }

    public class QueryPlanStep
    {
        public int Id { get; set; }
        public int Parent { get; set; }
        public int NotUsed { get; set; }
        public string Detail { get; set; }
    }

    public class SqlFormatter
    {
        public string Format(string sql)
        {
            // Basic SQL formatting
            sql = Regex.Replace(sql, @"\s+", " ");
            sql = Regex.Replace(sql, @"\s*(,)\s*", "$1 ");
            sql = Regex.Replace(sql, @"\s*(=)\s*", " $1 ");
            sql = Regex.Replace(sql, @"\s*(AND|OR|ON)\s*", "\n    $1 ");
            sql = Regex.Replace(sql, @"\s*(FROM|WHERE|ORDER BY|GROUP BY)\s*", "\n$1 ");

            return sql;
        }
    }
}
