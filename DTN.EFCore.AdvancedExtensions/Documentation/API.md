# DTN.EFCore.AdvancedExtensions API Documentation

## AdvancedDbContext

`AdvancedDbContext` is an extension of `DbContext` that provides additional functionality.

### methods

#### ExecuteOptimizedQueryAsync<T>

Executes an optimized query and returns results.

```csharp
public async Task<List<T>> ExecuteOptimizedQueryAsync<T>(IQueryable<T> query, bool useCache = true, TimeSpan? cacheDuration = null)
```

Options:
- `query`: the query to be executed.
- `useCache`: whether to use cache or not (default true).
- `cacheDuration`: cache duration (default null, which means auto-determination).

#### ApplyAccessControl<T>

Applies access control rules to the given query.

```csharp
public IQueryable<T> ApplyAccessControl<T>(IQueryable<T> query) where T : class
```

Options:
- `query`: the query to which access control should be applied.

## ComplexQueryBuilder<TEntity>

`ComplexQueryBuilder<TEntity>` allows you to build complex queries using the Fluent interface.

### methods

#### Where

Adds a WHERE clause to the query.

```csharp
public ComplexQueryBuilder<TEntity> Where(Expression<Func<TEntity, bool>> predicate)
public ComplexQueryBuilder<TEntity> Where(string propertyName, string operation, object value)
```

#### OrderBy

Adds an ORDER BY clause to the query.

```csharp
public ComplexQueryBuilder<TEntity> OrderBy(string propertyName, bool ascending = true)
```

#### Include

Adds an INCLUDE condition to the query.

```csharp
public ComplexQueryBuilder<TEntity> Include(string navigationPropertyPath)
```

#### Build

Returns the constructed IQueryable<TEntity> object.

```csharp
public IQueryable<TEntity> Build()
```

## AdvancedQueryOptimizer

``AdvancedQueryOptimizer'' provides query optimization methods.

### methods

#### OptimizeAsync<T>

Optimizes the given query.

```csharp
public async Task<IQueryable<T>> OptimizeAsync<T>(IQueryable<T> query, DbContext context) where T : class
```

## DistributedQueryCache

`DistributedQueryCache` provides distributed caching for queries.

### methods

#### GetOrSetAsync<T>

Returns a cached result or executes a query and stores the result in the cache.

```csharp
public async Task<List<T>> GetOrSetAsync<T>(IQueryable<T> query, TimeSpan? slidingExpiration = null, DateTimeOffset? absoluteExpiration = null)
```

#### InvalidateCacheAsync<T>

Invalidates the cache based on the given predicate.

```csharp
public async Task InvalidateCacheAsync<T>(Expression<Func<T, bool>> predicate)
```

## QueryPredictionModel

`QueryPredictionModel` uses machine learning to predict query execution time.

### methods

#### PredictQueryPerformanceAsync<T>

Predicts query execution time.

```csharp
public async Task<QueryPerformancePrediction> PredictQueryPerformanceAsync<T>(IQueryable<T> query, DbContext context)
```

This is an overview of the main APIs. For more detailed information about each class and method, please see the corresponding code files and XML documentation.