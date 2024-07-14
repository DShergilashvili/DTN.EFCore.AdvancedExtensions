using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace DTN.EFCore.AdvancedExtensions.Caching
{
    public class CacheStrategyProvider
    {
        private readonly DbContext _context;

        public CacheStrategyProvider(DbContext context)
        {
            _context = context;
        }

        public string GenerateCacheKey<T>(IQueryable<T> query)
        {
            var queryString = query.ToQueryString();
            return $"query_{ComputeHash(queryString)}";
        }

        public async Task<List<string>> GetRelatedCacheKeysAsync<T>(Expression<Func<T, bool>> predicate)
        {
            var affectedTables = GetAffectedTables(predicate);
            var relatedKeys = new List<string>();

            foreach (var table in affectedTables)
            {
                var cacheKeys = await _context.Set<CacheKeyMapping>()
                    .Where(ck => ck.TableName == table)
                    .Select(ck => ck.CacheKey)
                    .ToListAsync();

                relatedKeys.AddRange(cacheKeys);
            }

            return relatedKeys;
        }

        public async Task<List<string>> GetRelatedCacheKeysForEntityAsync(Type entityType, object entity)
        {
            var tableName = _context.Model.FindEntityType(entityType).GetTableName();
            var primaryKeyProperties = _context.Model.FindEntityType(entityType).FindPrimaryKey().Properties;
            var primaryKeyValues = primaryKeyProperties.Select(p => p.GetGetter().GetClrValue(entity)).ToArray();

            var cacheKeys = await _context.Set<CacheKeyMapping>()
                .Where(ck => ck.TableName == tableName && ck.EntityId == string.Join(",", primaryKeyValues))
                .Select(ck => ck.CacheKey)
                .ToListAsync();

            return cacheKeys;
        }

        private string ComputeHash(string input)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(input);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        private List<string> GetAffectedTables<T>(Expression<Func<T, bool>> predicate)
        {
            var visitor = new TableNameVisitor();
            visitor.Visit(predicate);
            return visitor.TableNames;
        }
    }

    public class TableNameVisitor : ExpressionVisitor
    {
        public List<string> TableNames { get; } = new List<string>();

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Expression != null && node.Expression.NodeType == ExpressionType.Parameter)
            {
                TableNames.Add(node.Member.DeclaringType.Name);
            }
            return base.VisitMember(node);
        }
    }

    public class CacheKeyMapping
    {
        public int Id { get; set; }
        public string TableName { get; set; }
        public string EntityId { get; set; }
        public string CacheKey { get; set; }
    }
}
