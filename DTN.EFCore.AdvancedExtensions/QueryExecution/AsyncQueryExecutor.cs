using DTN.EFCore.AdvancedExtensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace DTN.EFCore.AdvancedExtensions.QueryExecution
{
    public class AsyncQueryExecutor
    {
        private readonly AdvancedQueryLogger _logger;

        public AsyncQueryExecutor(AdvancedQueryLogger logger)
        {
            _logger = logger;
        }

        public async Task<List<T>> ExecuteAsync<T>(IQueryable<T> query)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = await query.ToListAsync();
            stopwatch.Stop();

            _logger.LogQuery(query.ToString(), stopwatch.Elapsed);
            return result;
        }

        public async Task<T> ExecuteSingleAsync<T>(IQueryable<T> query)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = await query.SingleOrDefaultAsync();
            stopwatch.Stop();

            _logger.LogQuery(query.ToString(), stopwatch.Elapsed);
            return result;
        }

        public async Task<int> ExecuteCountAsync<T>(IQueryable<T> query)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = await query.CountAsync();
            stopwatch.Stop();

            _logger.LogQuery(query.ToString(), stopwatch.Elapsed);
            return result;
        }

        public async Task<bool> ExecuteAnyAsync<T>(IQueryable<T> query)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = await query.AnyAsync();
            stopwatch.Stop();

            _logger.LogQuery(query.ToString(), stopwatch.Elapsed);
            return result;
        }

        public async Task<T> ExecuteFirstOrDefaultAsync<T>(IQueryable<T> query)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = await query.FirstOrDefaultAsync();
            stopwatch.Stop();

            _logger.LogQuery(query.ToString(), stopwatch.Elapsed);
            return result;
        }
    }
}
