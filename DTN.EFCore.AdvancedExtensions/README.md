# DTN.EFCore.AdvancedExtensions

DTN.EFCore.AdvancedExtensions is a powerful SDK for Entity Framework Core that provides additional functionality for query optimization, analysis, caching, and security.

## Key features

- Generation of complex queries
- Automatic query optimization
- Analysis of query execution plan
- Distributed caching
- Prediction of query performance based on machine learning
- Advanced security and access control
- Detailed audit log

## Installation

Use the NuGet package manager to install DTN.EFCore.AdvancedExtensions:

```
Install-Package DTN.EFCore.AdvancedExtensions
```

## usage

1. Change your DbContext to inherit from AdvancedDbContext:

```csharp
public class YourDbContext : AdvancedDbContext
{
 public YourDbContext(DbContextOptions<YourDbContext> options) : base(options) { }

 // Your DbSets and other configurations
}
```

2. Register the required services in Dependency Injection:

```csharp
services.AddAdvancedEFExtensions();
```

3. Use advanced functionality:

```csharp
public class YourService
{
 private readonly YourDbContext _context;

 public YourService(YourDbContext context)
 {
 _context = context;
 }

 public async Task<List<User>> GetUsersAsync()
 {
 var query = new ComplexQueryBuilder<User>(_context.Users)
 .Where(u => u.Age > 18)
 .OrderBy(u => u.LastName)
 .Include(u => u.Orders)
 .Build();

 return await _context.ExecuteOptimizedQueryAsync(query);
 }
}
```

## Documentation

Detailed documentation is available in the [API Documentation](Documentation/API.md) and [User Guide](Documentation/UserGuide.md) files.