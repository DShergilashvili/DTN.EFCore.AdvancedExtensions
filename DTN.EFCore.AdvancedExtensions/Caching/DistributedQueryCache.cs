using DTN.EFCore.AdvancedExtensions.MachineLearning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Caching.Distributed;
using System.Linq.Expressions;
using System.Text.Json;

namespace DTN.EFCore.AdvancedExtensions.Caching
{
    public class DistributedQueryCache
    {
        private readonly IDistributedCache _distributedCache;
        private readonly CacheStrategyProvider _cacheStrategyProvider;
        private readonly QueryPredictionModel _queryPredictionModel;
        private readonly IDbContextFactory<DbContext> _contextFactory;

        public DistributedQueryCache(
            IDistributedCache distributedCache,
            CacheStrategyProvider cacheStrategyProvider,
            QueryPredictionModel queryPredictionModel,
            IDbContextFactory<DbContext> contextFactory)
        {
            _distributedCache = distributedCache;
            _cacheStrategyProvider = cacheStrategyProvider;
            _queryPredictionModel = queryPredictionModel;
            _contextFactory = contextFactory;
        }

        public async Task<List<T>> GetOrSetAsync<T>(IQueryable<T> query, TimeSpan? slidingExpiration = null, DateTimeOffset? absoluteExpiration = null) where T : class
        {
            var cacheKey = _cacheStrategyProvider.GenerateCacheKey(query);
            var cachedResult = await _distributedCache.GetStringAsync(cacheKey);
            if (cachedResult != null)
            {
                return JsonSerializer.Deserialize<List<T>>(cachedResult);
            }

            using var context = await _contextFactory.CreateDbContextAsync();
            var result = await query.ToListAsync();

            var options = new DistributedCacheEntryOptions();
            var prediction = await _queryPredictionModel.PredictQueryPerformanceAsync(query, context);

            if (slidingExpiration.HasValue)
                options.SlidingExpiration = slidingExpiration.Value;
            else if (absoluteExpiration.HasValue)
                options.AbsoluteExpiration = absoluteExpiration.Value;
            else
                options.SlidingExpiration = DetermineCacheDuration(prediction);

            await _distributedCache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result), options);
            return result;
        }

        public async Task InvalidateCacheAsync<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            var cacheKeys = await _cacheStrategyProvider.GetRelatedCacheKeysAsync(predicate);
            foreach (var key in cacheKeys)
            {
                await _distributedCache.RemoveAsync(key);
            }
        }

        private TimeSpan DetermineCacheDuration(QueryPerformancePrediction prediction)
        {
            if (prediction.EstimatedExecutionTime < TimeSpan.FromSeconds(1))
                return TimeSpan.FromMinutes(30);
            else if (prediction.EstimatedExecutionTime < TimeSpan.FromSeconds(5))
                return TimeSpan.FromMinutes(15);
            else
                return TimeSpan.FromMinutes(5);
        }

        public async Task InvalidateRelatedCacheEntriesAsync(ChangeTracker changeTracker)
        {
            var modifiedEntities = changeTracker.Entries()
                .Where(e => e.State == EntityState.Modified || e.State == EntityState.Deleted)
                .Select(e => e.Entity)
                .ToList();

            foreach (var entity in modifiedEntities)
            {
                var entityType = entity.GetType();
                var cacheKeys = await _cacheStrategyProvider.GetRelatedCacheKeysForEntityAsync(entityType, entity);
                foreach (var key in cacheKeys)
                {
                    await _distributedCache.RemoveAsync(key);
                }
            }
        }
    }
}