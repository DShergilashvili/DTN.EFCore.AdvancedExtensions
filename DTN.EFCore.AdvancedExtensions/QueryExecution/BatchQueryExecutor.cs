using DTN.EFCore.AdvancedExtensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace DTN.EFCore.AdvancedExtensions.QueryExecution
{
    public class BatchQueryExecutor
    {
        private readonly DbContext _context;
        private readonly AdvancedQueryLogger _logger;

        public BatchQueryExecutor(DbContext context, AdvancedQueryLogger logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<TResult>> ExecuteQueriesAsync<TResult>(IEnumerable<IQueryable<TResult>> queries)
        {
            var results = new List<TResult>();
            var stopwatch = new Stopwatch();

            foreach (var query in queries)
            {
                stopwatch.Restart();
                var queryResults = await query.ToListAsync();
                stopwatch.Stop();

                results.AddRange(queryResults);
                _logger.LogQuery(query.ToString(), stopwatch.Elapsed);
            }

            return results;
        }

        public async Task<Dictionary<string, List<TResult>>> ExecuteNamedQueriesAsync<TResult>(Dictionary<string, IQueryable<TResult>> namedQueries)
        {
            var results = new Dictionary<string, List<TResult>>();
            var stopwatch = new Stopwatch();

            foreach (var (name, query) in namedQueries)
            {
                stopwatch.Restart();
                var queryResults = await query.ToListAsync();
                stopwatch.Stop();

                results[name] = queryResults;
                _logger.LogQuery($"{name}: {query}", stopwatch.Elapsed);
            }

            return results;
        }

        public async Task ExecuteNonQueryAsync(IEnumerable<Func<DbContext, Task>> operations)
        {
            var stopwatch = new Stopwatch();

            foreach (var operation in operations)
            {
                stopwatch.Restart();
                await operation(_context);
                stopwatch.Stop();

                _logger.LogSlowQuery(operation.Method.Name, stopwatch.Elapsed);
            }

            await _context.SaveChangesAsync();
        }
    }
}
