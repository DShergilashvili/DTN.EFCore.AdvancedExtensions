using System.Linq.Dynamic.Core;
using System.Linq.Expressions;

namespace DTN.EFCore.AdvancedExtensions.QueryGeneration
{
    public class DynamicLinqProvider
    {
        private readonly ParsingConfig _parsingConfig;

        public DynamicLinqProvider()
        {
            _parsingConfig = new ParsingConfig();
        }

        public IQueryable<T> CreateQuery<T>(IQueryable<T> source, string queryString)
        {
            return source.Where(queryString);
        }

        public IQueryable<T> OrderBy<T>(IQueryable<T> source, string orderByString)
        {
            return source.OrderBy(orderByString);
        }

        public IQueryable<T> Select<T>(IQueryable<T> source, string selectString)
        {
            return source.Select<T>(selectString);
        }

        public IQueryable<TResult> GroupBy<TSource, TResult>(IQueryable<TSource> source, string keySelector, string elementSelector, string resultSelector)
        {
            return source.GroupBy(keySelector, elementSelector).Select<TResult>(resultSelector);
        }

        public IQueryable<T> Join<T, TInner, TKey, TResult>(
            IQueryable<T> outer,
            IEnumerable<TInner> inner,
            string outerKeySelector,
            string innerKeySelector,
            string resultSelector)
        {
            var outerKeyLambda = DynamicExpressionParser.ParseLambda(_parsingConfig, false, typeof(T), typeof(TKey), outerKeySelector);
            var innerKeyLambda = DynamicExpressionParser.ParseLambda(_parsingConfig, false, typeof(TInner), typeof(TKey), innerKeySelector);
            var resultLambda = DynamicExpressionParser.ParseLambda(_parsingConfig, false, typeof(T), nameof(TInner), typeof(TResult), resultSelector);

            return outer
                .Join(inner,
                    (Expression<Func<T, TKey>>)outerKeyLambda,
                    (Expression<Func<TInner, TKey>>)innerKeyLambda,
                    (Expression<Func<T, TInner, TResult>>)resultLambda)
                .Cast<T>();
        }

        public Expression<Func<T, bool>> CreatePredicate<T>(string predicateString)
        {
            return DynamicExpressionParser.ParseLambda<T, bool>(_parsingConfig, false, predicateString);
        }
    }
}