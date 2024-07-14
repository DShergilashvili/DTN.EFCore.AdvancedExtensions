using System.Linq.Expressions;

namespace DTN.EFCore.AdvancedExtensions.Utilities
{
    public class ExpressionBuilder
    {
        public Expression<Func<T, bool>> BuildPredicate<T>(string propertyName, string operation, object value)
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            var left = Expression.Property(parameter, propertyName);
            var right = Expression.Constant(value);

            Expression body;
            switch (operation.ToLower())
            {
                case "eq":
                case "==":
                    body = Expression.Equal(left, right);
                    break;
                case "neq":
                case "!=":
                    body = Expression.NotEqual(left, right);
                    break;
                case "gt":
                case ">":
                    body = Expression.GreaterThan(left, right);
                    break;
                case "gte":
                case ">=":
                    body = Expression.GreaterThanOrEqual(left, right);
                    break;
                case "lt":
                case "<":
                    body = Expression.LessThan(left, right);
                    break;
                case "lte":
                case "<=":
                    body = Expression.LessThanOrEqual(left, right);
                    break;
                case "contains":
                    body = Expression.Call(left, typeof(string).GetMethod("Contains", new[] { typeof(string) }), right);
                    break;
                case "startswith":
                    body = Expression.Call(left, typeof(string).GetMethod("StartsWith", new[] { typeof(string) }), right);
                    break;
                case "endswith":
                    body = Expression.Call(left, typeof(string).GetMethod("EndsWith", new[] { typeof(string) }), right);
                    break;
                default:
                    throw new NotSupportedException($"Operation {operation} is not supported.");
            }

            return Expression.Lambda<Func<T, bool>>(body, parameter);
        }

        public Expression<Func<T, object>> BuildOrderExpression<T>(string propertyName)
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            var property = Expression.Property(parameter, propertyName);
            var conversion = Expression.Convert(property, typeof(object));
            return Expression.Lambda<Func<T, object>>(conversion, parameter);
        }

        public Expression<Func<T, IGrouping<TKey, T>>> BuildGroupExpression<T, TKey>(string propertyName)
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            var property = Expression.Property(parameter, propertyName);
            var conversion = Expression.Convert(property, typeof(TKey));
            var grouping = typeof(IGrouping<,>).MakeGenericType(typeof(TKey), typeof(T));
            return Expression.Lambda<Func<T, IGrouping<TKey, T>>>(Expression.New(grouping), parameter);
        }

        public Expression<Func<T, object>> BuildSelectExpression<T>(string[] propertyNames)
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            var properties = propertyNames.Select(name => Expression.Property(parameter, name));
            var newExpression = Expression.New(typeof(object));
            var initializers = properties.Select(prop => Expression.Bind(prop.Member, prop));
            var memberInit = Expression.MemberInit(newExpression, initializers);
            return Expression.Lambda<Func<T, object>>(memberInit, parameter);
        }
    }
}
