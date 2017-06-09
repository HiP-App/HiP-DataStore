﻿using PaderbornUniversity.SILab.Hip.DataStore.Model;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;

namespace PaderbornUniversity.SILab.Hip.DataStore.Controllers
{
    /// <summary>
    /// Provides some useful filtering and sorting extension methods to reduce boilerplate code in controllers.
    /// </summary>
    public static class QueryHelper
    {
        /// <remarks>
        /// If both, excluded and included IDs are specified, a collection item must match both conditions.
        /// That is, if the same ID is excluded and also included, the item with that ID is not part of the result.
        /// </remarks>
        public static IQueryable<T> FilterByIds<T, TKey>(this IQueryable<T> query, IReadOnlyCollection<TKey> excludedIds, IReadOnlyCollection<TKey> includedIds)
            where T : IEntity<TKey>
        {
            return query
                .FilterIf(excludedIds != null, x => !excludedIds.Contains(x.Id))
                .FilterIf(includedIds != null, x => includedIds.Contains(x.Id));
        }

        public static IQueryable<T> FilterByStatus<T>(this IQueryable<T> query, ContentStatus status) where T : ContentBase
        {
            return query.FilterIf(status != ContentStatus.All, x => x.Status == status);
        }

        public static IQueryable<T> FilterByUsage<T>(this IQueryable<T> query, bool? used) where T : ContentBase
        {
            switch (used)
            {
                case true: return query.Where(x => x.Referencees.Count > 0);
                case false: return query.Where(x => x.Referencees.Count == 0);
                default: return query;
            }
        }

        /// <summary>
        /// Executes the query to determine the number of results, then retrieves a subset of the results
        /// (determined by <paramref name="page"/> and <paramref name="pageSize"/>) and projects them to objects of
        /// a result type.
        /// </summary>
        /// <returns>
        /// An instance of <see cref="AllItemsResult{T}"/> containing total number of results and the results of the
        /// query that belong to the specified page.  
        /// </returns>
        public static AllItemsResult<TResult> PaginateAndSelect<T, TResult>(this IQueryable<T> query, int page, int pageSize, Func<T, TResult> resultSelector)
        {
            // Note: this method executes the incoming query twice (once to determine total count, a second time to
            // retrieve only the items of the current page). While this is not optimal, the alternative would be to
            // retrieve ALL items and then count them, which might have an even more negative performance impact.
            var totalCount = query.Count();

            var itemsInPage = (page < 1 || pageSize <= 0)
                ? Enumerable.Empty<T>().AsQueryable()
                : query.Skip((page-1) * pageSize).Take(pageSize);

            return new AllItemsResult<TResult>
            {
                Total = totalCount,
                Items = itemsInPage.Select(resultSelector).ToList()
            };
        }

        /// <summary>
        /// Applies a WHERE filter to a query if the specified condition is true.
        /// </summary>
        public static IQueryable<T> FilterIf<T>(this IQueryable<T> query, bool condition, Expression<Func<T, bool>> predicate)
        {
            return condition ? query.Where(predicate) : query;
        }

        /// <summary>
        /// Applies exactly one of multiple sorting rules, if <paramref name="sortKey"/> matches the name of a rule.
        /// Returns the collection unsorted if <paramref name="sortKey"/> is null or empty.
        /// Throws an exception if <paramref name="sortKey"/> is not empty but does not match the name of a rule.
        /// </summary>
        /// <example>
        /// The following call would return customers ordered by age:
        /// <code>
        /// customers.Sort("age",
        ///     ("name", x => x.FirstName),
        ///     ("age", x => x.Age),
        ///     ("address", x => x.Address));
        /// </code>
        /// </example>
        /// <exception cref="InvalidSortKeyException"/>
        public static IQueryable<T> Sort<T>(this IQueryable<T> query, string sortKey,
            params (string Key, Expression<Func<T, object>> Expression)[] sortRules)
        {
            if (sortKey == null)
                return query;

            var expression = sortRules.FirstOrDefault(c => c.Key == sortKey).Expression;

            return (expression == null)
                ? throw new InvalidSortKeyException(sortKey)
                : query.OrderBy(expression);
        }
    }

    [Serializable]
    public class InvalidSortKeyException : Exception
    {
        public InvalidSortKeyException(string providedSortKey)
            : base($"The collection does not support sorting by '{providedSortKey}'")
        {
        }
    }
}
