using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace DTN.EFCore.AdvancedExtensions.Security
{
    public class QuerySanitizer
    {
        public void SanitizeEntities(ChangeTracker changeTracker)
        {
            var entries = changeTracker.Entries().Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                foreach (var property in entry.Properties)
                {
                    if (property.CurrentValue is string stringValue)
                    {
                        property.CurrentValue = SanitizeString(stringValue);
                    }
                }
            }
        }

        private string SanitizeString(string input)
        {
            // Remove potential SQL injection patterns
            input = Regex.Replace(input, @"[-;']", "");

            // Remove any HTML tags
            input = Regex.Replace(input, "<.*?>", string.Empty);

            // Encode special characters
            input = System.Web.HttpUtility.HtmlEncode(input);

            // Trim whitespace
            input = input.Trim();

            return input;
        }

        public IQueryable<T> SanitizeQuery<T>(IQueryable<T> query)
        {
            var expression = query.Expression;
            var sanitizedExpression = new QuerySanitizerVisitor().Visit(expression);
            return query.Provider.CreateQuery<T>(sanitizedExpression);
        }
    }

    public class QuerySanitizerVisitor : ExpressionVisitor
    {
        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (node.Value is string stringValue)
            {
                return Expression.Constant(SanitizeString(stringValue), typeof(string));
            }
            return base.VisitConstant(node);
        }

        private string SanitizeString(string input)
        {
            // Remove potential SQL injection patterns
            input = Regex.Replace(input, @"[-;']", "");

            // Remove any HTML tags
            input = Regex.Replace(input, "<.*?>", string.Empty);

            // Encode special characters
            input = System.Web.HttpUtility.HtmlEncode(input);

            // Trim whitespace
            input = input.Trim();

            return input;
        }
    }
}
