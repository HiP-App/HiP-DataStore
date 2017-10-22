using PaderbornUniversity.SILab.Hip.DataStore.Model;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using System.Security.Principal;
using PaderbornUniversity.SILab.Hip.DataStore.Utility;

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

        public static IQueryable<T> FilterByStatus<T>(this IQueryable<T> query, ContentStatus status, IIdentity User) where T : ContentBase
        {
            return query.FilterIf(status != ContentStatus.All, x => x.Status == status)
                        .FilterIf(status == ContentStatus.All && !UserPermissions.IsAllowedToGetDeleted(User),
                                                                  x => x.Status != ContentStatus.Deleted);
        }

        public static IQueryable<T> FilterByUsage<T>(this IQueryable<T> query, bool? used) where T : ContentBase
        {
            switch (used)
            {
                case true: return query.Where(x => x.Referencers.Count > 0);
                case false: return query.Where(x => x.Referencers.Count == 0);
                default: return query;
            }
        }

        public static IQueryable<T> FilterByTimestamp<T>(this IQueryable<T> query, DateTimeOffset? timestamp) where T : ContentBase
        {
            return (timestamp == null)
                ? query
                : query.Where(x => x.Timestamp > timestamp.Value);
        }

        public static IQueryable<T> FilterByUser<T>(this IQueryable<T> query, ContentStatus status, IIdentity User) where T : ContentBase
        {
            bool isAllowedGetAll = UserPermissions.IsAllowedToGetAll(User, status);
            return query.FilterIf(!isAllowedGetAll, x =>
                        ((status == ContentStatus.All) && (x.Status == ContentStatus.Published)) || (x.UserId == User.GetUserIdentity()));
        }
        /// <summary>
        /// Returns all entries that are located within the radius, defined within the entry itself, around the given latitude and longitude.
        /// If none or only one coordinate is given, all entries are returned.
        /// </summary>
        public static IQueryable<T> FilterByLocation<T>(this IQueryable<T> query, float? latitude, float? longitude) where T : ContentBase
        {
            List<int> excludedIds = new List<int>();

            if (typeof(Exhibit) == typeof(T))
            {
                List<Exhibit> exhibits = query.ToList().Cast<Exhibit>().ToList();

                if (latitude != null && longitude != null)
                {
                    foreach (var entry in exhibits)
                    {

                        if (GetDistanceFromLatLonInKm(entry.Latitude, entry.Longitude, latitude, longitude) > entry.AccessRadius)
                        {
                            excludedIds.Add(entry.Id);
                        }
                    }
                    return FilterByIds(query, excludedIds, null);
                }

            }
            return query;
        }

        /// <summary>
        /// Calculates the distance between two points of latitude and longitude in km.
        /// </summary>
        private static double GetDistanceFromLatLonInKm(float? lat1, float? lon1, float? lat2, float? lon2)
        {
            float? dLat = (lat2 - lat1) * (float)Math.PI / 180;
            float? dLon = (lon2 - lon1) * (float)Math.PI / 180;

            double a = Math.Sin((double)dLat / 2) * Math.Sin((double)dLat / 2) 
                + Math.Cos((double)lat1 * Math.PI / 180) * Math.Cos((double)lat2 * Math.PI / 180) 
                * Math.Sin((double)dLon / 2) * Math.Sin((double)dLon / 2);
            return 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1-a)) * 6378;
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
        public static AllItemsResult<TResult> PaginateAndSelect<T, TResult>(this IQueryable<T> query, int? page, int? pageSize, Func<T, TResult> resultSelector)
        {
            // Note: this method executes the incoming query twice (once to determine total count, a second time to
            // retrieve only the items of the current page). While this is not optimal, the alternative would be to
            // retrieve ALL items and then count them, which might have an even more negative performance impact.

            var actualPage = page.GetValueOrDefault(1);

            var actualPageSize = page.HasValue
                ? pageSize.GetValueOrDefault(10) // if page is specified, pageSize defaults to 10
                : pageSize.GetValueOrDefault(int.MaxValue); // otherwise, all items should be returned (max. page size)

            var totalCount = query.Count();

            var itemsInPage = (actualPage < 1 || actualPageSize <= 0)
                ? Enumerable.Empty<T>().AsQueryable()
                : query.Skip((actualPage - 1) * actualPageSize).Take(actualPageSize);

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
