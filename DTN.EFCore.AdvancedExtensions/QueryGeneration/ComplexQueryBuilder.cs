using DTN.EFCore.AdvancedExtensions.Utilities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace DTN.EFCore.AdvancedExtensions.QueryGeneration
{
    public class ComplexQueryBuilder<TEntity> where TEntity : class
    {
        private IQueryable<TEntity> _query;
        private readonly ExpressionBuilder _expressionBuilder;
        private readonly List<string> _includePaths = new List<string>();
        private Expression<Func<TEntity, object>> _groupByExpression;
        private List<(string PropertyName, bool Ascending)> _orderByExpressions = new List<(string, bool)>();
        private Expression<Func<TEntity, TEntity>> _selectExpression;

        public ComplexQueryBuilder(IQueryable<TEntity> baseQuery)
        {
            _query = baseQuery;
            _expressionBuilder = new ExpressionBuilder();
        }

        public ComplexQueryBuilder<TEntity> Where(Expression<Func<TEntity, bool>> predicate)
        {
            _query = _query.Where(predicate);
            return this;
        }

        public ComplexQueryBuilder<TEntity> Where(string propertyName, string operation, object value)
        {
            var predicate = _expressionBuilder.BuildPredicate<TEntity>(propertyName, operation, value);
            _query = _query.Where(predicate);
            return this;
        }

        public ComplexQueryBuilder<TEntity> OrderBy(string propertyName, bool ascending = true)
        {
            _orderByExpressions.Add((propertyName, ascending));
            return this;
        }

        public ComplexQueryBuilder<TEntity> Include(string navigationPropertyPath)
        {
            _includePaths.Add(navigationPropertyPath);
            return this;
        }

        public ComplexQueryBuilder<TEntity> GroupBy(Expression<Func<TEntity, object>> groupExpression)
        {
            _groupByExpression = groupExpression;
            return this;
        }

        public ComplexQueryBuilder<TEntity> Select(Expression<Func<TEntity, TEntity>> selectExpression)
        {
            _selectExpression = selectExpression;
            return this;
        }

        public ComplexQueryBuilder<TEntity> Take(int count)
        {
            _query = _query.Take(count);
            return this;
        }

        public ComplexQueryBuilder<TEntity> Skip(int count)
        {
            _query = _query.Skip(count);
            return this;
        }

        public ComplexQueryBuilder<TEntity> Distinct()
        {
            _query = _query.Distinct();
            return this;
        }

        public IQueryable<TEntity> Build()
        {
            foreach (var includePath in _includePaths)
            {
                _query = _query.Include(includePath);
            }

            if (_groupByExpression != null)
            {
                _query = _query.GroupBy(_groupByExpression).SelectMany(g => g);
            }

            foreach (var (propertyName, ascending) in _orderByExpressions)
            {
                var orderExpression = _expressionBuilder.BuildOrderExpression<TEntity>(propertyName);
                _query = ascending ? _query.OrderBy(orderExpression) : _query.OrderByDescending(orderExpression);
            }

            if (_selectExpression != null)
            {
                _query = _query.Select(_selectExpression);
            }

            return _query;
        }

        public async Task<List<TEntity>> ExecuteAsync()
        {
            return await Build().ToListAsync();
        }

        public async Task<int> CountAsync()
        {
            return await Build().CountAsync();
        }

        public async Task<bool> AnyAsync()
        {
            return await Build().AnyAsync();
        }

        public async Task<TEntity> FirstOrDefaultAsync()
        {
            return await Build().FirstOrDefaultAsync();
        }
    }
}