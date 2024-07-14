using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace DTN.EFCore.AdvancedExtensions.Logging
{
    public class AdvancedQueryLogger
    {
        private readonly ILogger<AdvancedQueryLogger> _logger;
        private readonly ConcurrentDictionary<string, QueryStatistics> _queryStatistics = new ConcurrentDictionary<string, QueryStatistics>();

        public AdvancedQueryLogger(ILogger<AdvancedQueryLogger> logger)
        {
            _logger = logger;
        }

        public void LogQuery(string sql, TimeSpan executionTime)
        {
            _logger.LogInformation($"Query executed in {executionTime.TotalMilliseconds}ms: {sql}");
            UpdateQueryStatistics(sql, executionTime);
        }

        public void LogSlowQuery(string sql, TimeSpan executionTime)
        {
            _logger.LogWarning($"Slow query executed in {executionTime.TotalMilliseconds}ms: {sql}");
            UpdateQueryStatistics(sql, executionTime);
        }

        public void LogEntityOperation(string operation, string entityName)
        {
            _logger.LogInformation($"{operation} operation performed on entity: {entityName}");
        }

        private void UpdateQueryStatistics(string sql, TimeSpan executionTime)
        {
            _queryStatistics.AddOrUpdate(
                sql,
                new QueryStatistics { ExecutionCount = 1, TotalExecutionTime = executionTime },
                (key, existingStats) =>
                {
                    existingStats.ExecutionCount++;
                    existingStats.TotalExecutionTime += executionTime;
                    return existingStats;
                });
        }

        public IEnumerable<QueryStatisticsReport> GetQueryStatistics()
        {
            return _queryStatistics.Select(kvp => new QueryStatisticsReport
            {
                Sql = kvp.Key,
                ExecutionCount = kvp.Value.ExecutionCount,
                AverageExecutionTime = kvp.Value.TotalExecutionTime / kvp.Value.ExecutionCount,
                TotalExecutionTime = kvp.Value.TotalExecutionTime
            });
        }
    }

    public class QueryStatistics
    {
        public int ExecutionCount { get; set; }
        public TimeSpan TotalExecutionTime { get; set; }
    }

    public class QueryStatisticsReport
    {
        public string Sql { get; set; }
        public int ExecutionCount { get; set; }
        public TimeSpan AverageExecutionTime { get; set; }
        public TimeSpan TotalExecutionTime { get; set; }
    }
}
