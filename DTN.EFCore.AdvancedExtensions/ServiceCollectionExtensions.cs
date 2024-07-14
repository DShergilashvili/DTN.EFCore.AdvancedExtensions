using DTN.EFCore.AdvancedExtensions.Caching;
using DTN.EFCore.AdvancedExtensions.Core;
using DTN.EFCore.AdvancedExtensions.Logging;
using DTN.EFCore.AdvancedExtensions.MachineLearning;
using DTN.EFCore.AdvancedExtensions.QueryAnalysis;
using DTN.EFCore.AdvancedExtensions.QueryExecution;
using DTN.EFCore.AdvancedExtensions.QueryOptimization;
using DTN.EFCore.AdvancedExtensions.Security;
using DTN.EFCore.AdvancedExtensions.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DTN.EFCore.AdvancedExtensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAdvancedEFExtensions(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddPooledDbContextFactory<AdvancedDbContext>((sp, options) =>
            {
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
            }, poolSize: 128);

            services.AddScoped<DbContext>(sp => sp.GetRequiredService<IDbContextFactory<AdvancedDbContext>>().CreateDbContext());

            services.AddScoped<BatchQueryExecutor>();
            services.AddScoped<AuditTrailManager>();
            services.AddScoped<TransactionManager>();
            services.AddScoped<DistributedQueryCache>();
            services.AddScoped<AccessControlManager>();
            services.AddScoped<AdvancedQueryOptimizer>();
            services.AddScoped<QueryPlanAnalyzer>();
            services.AddScoped<CacheStrategyProvider>();

            services.AddSingleton<PerformanceProfiler>();
            services.AddSingleton<QuerySanitizer>();
            services.AddSingleton<IndexSuggestionEngine>();
            services.AddSingleton<QueryPredictionModel>();

            services.AddHttpContextAccessor();
            services.AddDistributedMemoryCache();
            services.AddSingleton<AdvancedQueryLogger>();

            return services;
        }
    }
}
