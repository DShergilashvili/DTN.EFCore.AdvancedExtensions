using DTN.EFCore.AdvancedExtensions.Caching;
using DTN.EFCore.AdvancedExtensions.Logging;
using DTN.EFCore.AdvancedExtensions.MachineLearning;
using DTN.EFCore.AdvancedExtensions.QueryAnalysis;
using DTN.EFCore.AdvancedExtensions.QueryExecution;
using DTN.EFCore.AdvancedExtensions.QueryOptimization;
using DTN.EFCore.AdvancedExtensions.Security;
using DTN.EFCore.AdvancedExtensions.Transactions;
using Microsoft.EntityFrameworkCore;

namespace DTN.EFCore.AdvancedExtensions.Core
{
    public class AdvancedDbContext : DbContext
    {
        private readonly BatchQueryExecutor _batchQueryExecutor;
        private readonly PerformanceProfiler _performanceProfiler;
        private readonly AuditTrailManager _auditTrailManager;
        private readonly TransactionManager _transactionManager;
        private readonly QuerySanitizer _querySanitizer;
        private readonly DistributedQueryCache _queryCache;
        private readonly AccessControlManager _accessControlManager;
        private readonly QueryPredictionModel _queryPredictionModel;
        private readonly AdvancedQueryOptimizer _advancedQueryOptimizer;

        public AdvancedDbContext(
            DbContextOptions<AdvancedDbContext> options,
            BatchQueryExecutor batchQueryExecutor,
            PerformanceProfiler performanceProfiler,
            AuditTrailManager auditTrailManager,
            TransactionManager transactionManager,
            QuerySanitizer querySanitizer,
            DistributedQueryCache queryCache,
            AccessControlManager accessControlManager,
            QueryPredictionModel queryPredictionModel,
            AdvancedQueryOptimizer advancedQueryOptimizer) : base(options)
        {
            _batchQueryExecutor = batchQueryExecutor;
            _performanceProfiler = performanceProfiler;
            _auditTrailManager = auditTrailManager;
            _transactionManager = transactionManager;
            _querySanitizer = querySanitizer;
            _queryCache = queryCache;
            _accessControlManager = accessControlManager;
            _queryPredictionModel = queryPredictionModel;
            _advancedQueryOptimizer = advancedQueryOptimizer;
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            using var transaction = await _transactionManager.BeginTransactionAsync();
            try
            {
                _auditTrailManager.CaptureChanges(ChangeTracker);
                _querySanitizer.SanitizeEntities(ChangeTracker);
                _accessControlManager.EnforceAccessControl(ChangeTracker);

                var result = await base.SaveChangesAsync(cancellationToken);

                await _auditTrailManager.SaveAuditTrailAsync(this);
                await _queryCache.InvalidateRelatedCacheEntriesAsync(ChangeTracker);
                await transaction.CommitAsync(cancellationToken);

                return result;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _performanceProfiler.LogException(ex);
                throw;
            }
        }

        public async Task<List<T>> ExecuteBatchQueryAsync<T>(IEnumerable<IQueryable<T>> queries)
        {
            return await _batchQueryExecutor.ExecuteQueriesAsync(queries);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.AddInterceptors(_performanceProfiler);
            base.OnConfiguring(optionsBuilder);
        }

        public PerformanceReport GetPerformanceReport()
        {
            return _performanceProfiler.GenerateReport();
        }

        public async Task<List<T>> ExecuteOptimizedQueryAsync<T>(IQueryable<T> query, bool useCache = true, TimeSpan? cacheDuration = null) where T : class
        {
            var optimizedQuery = await _advancedQueryOptimizer.OptimizeAsync(query, this);
            var prediction = await _queryPredictionModel.PredictQueryPerformanceAsync(optimizedQuery, this);

            if (prediction.EstimatedExecutionTime > TimeSpan.FromSeconds(5))
            {
                _performanceProfiler.LogLongRunningQuery(optimizedQuery, prediction.EstimatedExecutionTime);
            }

            if (useCache)
            {
                return await _queryCache.GetOrSetAsync(optimizedQuery, cacheDuration ?? TimeSpan.FromMinutes(10));
            }
            else
            {
                return await optimizedQuery.ToListAsync();
            }
        }

        public IQueryable<T> ApplyAccessControl<T>(IQueryable<T> query) where T : class
        {
            return _accessControlManager.ApplyAccessControl(query);
        }
    }
}