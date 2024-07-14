using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Linq.Expressions;
using System.Security.Claims;

namespace DTN.EFCore.AdvancedExtensions.Security
{
    public class AccessControlManager
    {
        private readonly ClaimsPrincipal _user;
        private readonly Dictionary<string, Func<ClaimsPrincipal, bool>> _entityAccessPolicies;

        public AccessControlManager(ClaimsPrincipal user)
        {
            _user = user;
            _entityAccessPolicies = new Dictionary<string, Func<ClaimsPrincipal, bool>>();
        }

        public void RegisterEntityAccessPolicy<TEntity>(Func<ClaimsPrincipal, bool> policy)
        {
            _entityAccessPolicies[typeof(TEntity).Name] = policy;
        }

        public void EnforceAccessControl(ChangeTracker changeTracker)
        {
            var entries = changeTracker.Entries().Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted);

            foreach (var entry in entries)
            {
                var entityName = entry.Entity.GetType().Name;
                if (_entityAccessPolicies.TryGetValue(entityName, out var policy))
                {
                    if (!policy(_user))
                    {
                        throw new UnauthorizedAccessException($"Access denied for {entry.State} operation on {entityName}");
                    }
                }
            }
        }

        public IQueryable<T> ApplyAccessControl<T>(IQueryable<T> query) where T : class
        {
            var entityName = typeof(T).Name;
            if (_entityAccessPolicies.TryGetValue(entityName, out var policy))
            {
                if (!policy(_user))
                {
                    // If the user doesn't have access, return an empty query
                    return query.Where(e => false);
                }
            }

            // Apply row-level security if applicable
            return ApplyRowLevelSecurity(query);
        }

        private IQueryable<T> ApplyRowLevelSecurity<T>(IQueryable<T> query) where T : class
        {
            // Example: Applying a tenant filter
            var tenantId = _user.FindFirst("TenantId")?.Value;
            if (!string.IsNullOrEmpty(tenantId) && typeof(T).GetProperty("TenantId") != null)
            {
                var parameter = Expression.Parameter(typeof(T), "e");
                var property = Expression.Property(parameter, "TenantId");
                var constant = Expression.Constant(tenantId);
                var equality = Expression.Equal(property, constant);
                var lambda = Expression.Lambda<Func<T, bool>>(equality, parameter);

                query = query.Where(lambda);
            }

            return query;
        }
    }
}
