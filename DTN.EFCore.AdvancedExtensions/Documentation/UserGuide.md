## Introduction

DTN.EFCore.AdvancedExtensions is a powerful SDK that extends the capabilities of Entity Framework Core. It provides additional tools for query optimization, analysis, caching and security.

## start

1. Install the NuGet package:
 ```
 Install-Package DTN.EFCore.AdvancedExtensions
 ```

2. Change your DbContext to inherit from AdvancedDbContext:
 ```csharp
 public class YourDbContext : AdvancedDbContext
 {
 public YourDbContext(DbContextOptions<YourDbContext> options) : base(options) { }

 // Your DbSets and other configurations
 }
 ```

3. Register the required services in Startup.cs:
 ```csharp
 public void ConfigureServices(IServiceCollection services)
 {
 services.AddAdvancedEFExtensions();
 // Other service configurations
 }
 ```

## Main functions and usage examples

### 1. Generation of complex queries

The ComplexQueryBuilder class allows you to build complex queries using the Fluent interface:

```csharp
var queryBuilder = new ComplexQueryBuilder<User>(_context.Users)
 .Where(u => u.Age > 18)
 .OrderBy(u => u.LastName)
 .Include(u => u.Orders)
 .Take(10);

var users = await queryBuilder.ExecuteAsync();
```

### 2. Query optimization

The AdvancedQueryOptimizer class automatically optimizes queries:

```csharp
var optimizer = new AdvancedQueryOptimizer();
var optimizedQuery = await optimizer.OptimizeAsync(query, _context);
var result = await optimizedQuery.ToListAsync();
```

### 3. Query analysis

The QueryPlanAnalyzer class allows you to analyze the query execution plan:

```csharp
var analyzer = new QueryPlanAnalyzer();
var analysis = await analyzer.AnalyzeQueryPlanAsync(_context, query);
foreach (var bottleneck in analysis.Bottlenecks)
{
 Console.WriteLine($"Bottleneck: {bottleneck}");
}
```

### 4. Distributed caching

The DistributedQueryCache class provides efficient caching:

```csharp
var cache = new DistributedQueryCache();
var result = await cache.GetOrSetAsync(query, TimeSpan.FromMinutes(10));
```

### 5. Access Control

The AccessControlManager class allows you to apply access control rules:

```csharp
var accessManager = new AccessControlManager(currentUser);
var query = accessManager.ApplyAccessControl(_context.Users);
var result = await query.ToListAsync();
```

### 6. Prediction of query execution time

The QueryPredictionModel class uses machine learning to predict query execution time:

```csharp
var predictionModel = new QueryPredictionModel();
var prediction = await predictionModel.PredictQueryPerformanceAsync(query, _context);
Console.WriteLine($"Estimated execution time: {prediction.EstimatedExecutionTime}");
```

### 7. Management of transactions

The TransactionManager class provides advanced transaction management:

```csharp
var transactionManager = new TransactionManager(_context);
await transactionManager.ExecuteInTransactionAsync(async () =>
{
 // Your transactional operations here
});
```

### 8. Audit log

The AuditTrailManager class automatically records changes:

```csharp
var auditManager = new AuditTrailManager();
auditManager.CaptureChanges(_context.ChangeTracker);
await _context.SaveChangesAsync();
await auditManager.SaveAuditTrailAsync(_context);
```

## Best practices

1. Always use a query optimizer for complex queries.
2. Use caching for frequently used queries.
3. Regularly analyze query execution plans to detect performance issues.
4. Use access control to ensure data security.
5. Use the audit log to record significant changes.

## Frequently asked questions

1. **Q: What is the advantage of using AdvancedDbContext over regular DbContext?**
 A: AdvancedDbContext offers additional functionality such as automatic query optimization, caching, and audit logging.

2. **Q: Can I use this SDK in an existing project?**
 A: Yes, you can integrate into an existing project with minimal changes.

3. **Q: How can I improve the performance of my queries using this SDK?**
 A: Use the AdvancedQueryOptimizer class to automatically optimize queries and the QueryPlanAnalyzer class to identify performance issues.

If you have any further questions, please contact us or check out our documentation on GitHub.