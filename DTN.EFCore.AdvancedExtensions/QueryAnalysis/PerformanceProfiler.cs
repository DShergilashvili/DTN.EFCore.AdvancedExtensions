using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Diagnostics;

namespace DTN.EFCore.AdvancedExtensions.QueryAnalysis
{
    public class PerformanceProfiler : IInterceptor
    {
        private readonly ConcurrentDictionary<Guid, Stopwatch> _queryTimers = new ConcurrentDictionary<Guid, Stopwatch>();
        private readonly ConcurrentBag<QueryPerformanceInfo> _queryPerformanceLog = new ConcurrentBag<QueryPerformanceInfo>();

        public async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            var timer = new Stopwatch();
            timer.Start();
            _queryTimers[eventData.CommandId] = timer;
            return result;
        }

        public async ValueTask<DbDataReader> ReaderExecutedAsync(
            DbCommand command,
            CommandExecutedEventData eventData,
            DbDataReader result,
            CancellationToken cancellationToken = default)
        {
            if (_queryTimers.TryRemove(eventData.CommandId, out var timer))
            {
                timer.Stop();
                _queryPerformanceLog.Add(new QueryPerformanceInfo
                {
                    CommandId = eventData.CommandId,
                    Sql = command.CommandText,
                    Parameters = command.Parameters.Cast<DbParameter>().Select(p => (object)new { p.ParameterName, p.Value }).ToList(),
                    ExecutionTime = timer.Elapsed
                });
            }
            return result;
        }

        public void LogLongRunningQuery(IQueryable query, TimeSpan estimatedExecutionTime)
        {
            _queryPerformanceLog.Add(new QueryPerformanceInfo
            {
                CommandId = Guid.NewGuid(),
                Sql = query.ToQueryString(),
                EstimatedExecutionTime = estimatedExecutionTime,
                IsLongRunning = true
            });
        }

        public void LogException(Exception ex)
        {
            _queryPerformanceLog.Add(new QueryPerformanceInfo
            {
                CommandId = Guid.NewGuid(),
                Exception = ex
            });
        }

        public PerformanceReport GenerateReport()
        {
            return new PerformanceReport
            {
                Queries = _queryPerformanceLog.ToList(),
                TotalQueries = _queryPerformanceLog.Count,
                AverageExecutionTime = TimeSpan.FromMilliseconds(_queryPerformanceLog.Average(q => q.ExecutionTime?.TotalMilliseconds ?? 0)),
                LongestQuery = _queryPerformanceLog.OrderByDescending(q => q.ExecutionTime).FirstOrDefault(),
                ExceptionCount = _queryPerformanceLog.Count(q => q.Exception != null)
            };
        }
    }

    public class QueryPerformanceInfo
    {
        public Guid CommandId { get; set; }
        public string Sql { get; set; }
        public List<object> Parameters { get; set; }
        public TimeSpan? ExecutionTime { get; set; }
        public TimeSpan? EstimatedExecutionTime { get; set; }
        public bool IsLongRunning { get; set; }
        public Exception Exception { get; set; }
    }

    public class PerformanceReport
    {
        public List<QueryPerformanceInfo> Queries { get; set; }
        public int TotalQueries { get; set; }
        public TimeSpan AverageExecutionTime { get; set; }
        public QueryPerformanceInfo LongestQuery { get; set; }
        public int ExceptionCount { get; set; }
    }
}