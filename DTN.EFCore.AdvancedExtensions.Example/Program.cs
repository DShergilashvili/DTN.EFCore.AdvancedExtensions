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

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<BatchQueryExecutor>();
builder.Services.AddSingleton<PerformanceProfiler>();
builder.Services.AddSingleton<AuditTrailManager>();
builder.Services.AddSingleton<TransactionManager>();
builder.Services.AddSingleton<QuerySanitizer>();
builder.Services.AddSingleton<CacheStrategyProvider>();
builder.Services.AddSingleton<QueryPredictionModel>();
builder.Services.AddSingleton<DistributedQueryCache>();
builder.Services.AddSingleton<AccessControlManager>();
builder.Services.AddSingleton<AdvancedQueryOptimizer>();
builder.Services.AddSingleton<QueryPlanAnalyzer>();
builder.Services.AddSingleton<IndexSuggestionEngine>();
builder.Services.AddSingleton<AdvancedQueryLogger>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddDbContext<AdvancedDbContext>((serviceProvider, options) =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));

    // Use the internal service provider
    options.UseInternalServiceProvider(serviceProvider);
});

builder.Services.AddDbContextFactory<AdvancedDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
