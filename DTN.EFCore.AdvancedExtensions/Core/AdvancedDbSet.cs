using DTN.EFCore.AdvancedExtensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Diagnostics;
using System.Linq.Expressions;

namespace DTN.EFCore.AdvancedExtensions.Core
{
    public class AdvancedDbSet<TEntity> : DbSet<TEntity> where TEntity : class
    {
        private readonly DbSet<TEntity> _dbSet;
        private readonly AdvancedQueryLogger _logger;

        public AdvancedDbSet(DbSet<TEntity> dbSet, AdvancedQueryLogger logger)
        {
            _dbSet = dbSet;
            _logger = logger;
        }

        public override IEntityType EntityType => _dbSet.EntityType;

        public override IQueryable<TEntity> AsQueryable()
        {
            return new AdvancedQueryable<TEntity>(_dbSet.AsQueryable(), _logger);
        }

        public override EntityEntry<TEntity> Add(TEntity entity)
        {
            _logger.LogEntityOperation(nameof(Add), typeof(TEntity).Name);
            return _dbSet.Add(entity);
        }

        public override ValueTask<EntityEntry<TEntity>> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            _logger.LogEntityOperation(nameof(AddAsync), typeof(TEntity).Name);
            return _dbSet.AddAsync(entity, cancellationToken);
        }

        public override EntityEntry<TEntity> Remove(TEntity entity)
        {
            _logger.LogEntityOperation(nameof(Remove), typeof(TEntity).Name);
            return _dbSet.Remove(entity);
        }

        public override EntityEntry<TEntity> Update(TEntity entity)
        {
            _logger.LogEntityOperation(nameof(Update), typeof(TEntity).Name);
            return _dbSet.Update(entity);
        }
    }

    public class AdvancedQueryable<TEntity> : IOrderedQueryable<TEntity>
    {
        private readonly IQueryable<TEntity> _inner;
        private readonly AdvancedQueryLogger _logger;

        public AdvancedQueryable(IQueryable<TEntity> inner, AdvancedQueryLogger logger)
        {
            _inner = inner;
            _logger = logger;
        }

        public Type ElementType => _inner.ElementType;
        public Expression Expression => _inner.Expression;
        public IQueryProvider Provider => new AdvancedQueryProvider<TEntity>(_inner.Provider, _logger);

        public IEnumerator<TEntity> GetEnumerator()
        {
            var stopwatch = Stopwatch.StartNew();
            var enumerator = _inner.GetEnumerator();
            stopwatch.Stop();
            _logger.LogQuery(_inner.Expression.ToString(), stopwatch.Elapsed);
            return enumerator;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class AdvancedQueryProvider<TEntity> : IQueryProvider
    {
        private readonly IQueryProvider _inner;
        private readonly AdvancedQueryLogger _logger;

        public AdvancedQueryProvider(IQueryProvider inner, AdvancedQueryLogger logger)
        {
            _inner = inner;
            _logger = logger;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            return new AdvancedQueryable<TEntity>(_inner.CreateQuery<TEntity>(expression), _logger);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new AdvancedQueryable<TElement>(_inner.CreateQuery<TElement>(expression), _logger);
        }

        public object Execute(Expression expression)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = _inner.Execute(expression);
            stopwatch.Stop();
            _logger.LogQuery(expression.ToString(), stopwatch.Elapsed);
            return result;
        }

        public TResult Execute<TResult>(Expression expression)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = _inner.Execute<TResult>(expression);
            stopwatch.Stop();
            _logger.LogQuery(expression.ToString(), stopwatch.Elapsed);
            return result;
        }
    }
}
