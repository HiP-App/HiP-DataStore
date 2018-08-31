using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using PaderbornUniversity.SILab.Hip.EventSourcing;
using System;
using System.Collections.Generic;
using System.Security.Principal;
using PaderbornUniversity.SILab.Hip.DataStore.Utility;

namespace PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel
{
    public class RatingIndex : IDomainIndex
    {
        private readonly Dictionary<ResourceType, RatingTypeInfo> _rateDictionary = new Dictionary<ResourceType, RatingTypeInfo>();

        /// <summary>
        /// Number of user rating`s
        /// </summary>
        public int Count(ResourceType res, int id)
        {
            return GetOrCreateRatingTypeInfo(res).Ratings.TryGetValue(id, out var t) ? t.NumberRates : 0;
        }
        public Dictionary<int, int> Table(ResourceType res, int id)
        {
            return GetOrCreateRatingTypeInfo(res).Ratings.TryGetValue(id, out var t) ? t.GetRatingTable() : null;
        }

        public byte? UserRating(ResourceType res, int id, IIdentity identity)
        {
            return GetOrCreateRatingTypeInfo(res).Ratings.TryGetValue(id, out var t) ? t.GetUserRating(identity.GetUserIdentity()) : 0;
        }

        public double Average(ResourceType res, int id)
        {
            return GetOrCreateRatingTypeInfo(res).Ratings.TryGetValue(id, out var t) ? t.AverageRate : 0;
        }

        public double LastMonthAverage(ResourceType res, int id)
        {
            return GetOrCreateRatingTypeInfo(res).Ratings.TryGetValue(id, out var t) ? t.CalculateLastMonthAverageRating() : 0;
        }

        public int NextId(ResourceType entityType)
        {
            var info = GetOrCreateRatingTypeInfo(entityType);
            return ++info.MaximumId;
        }

        public void ApplyEvent(IEvent e)
        {
            switch (e)
            {
                case RatingAdded ev:
                    var ratedType = GetOrCreateRatingTypeInfo(ev.RatedType);
                    ratedType.MaximumId = Math.Max(ratedType.MaximumId, ev.Id);

                    if (!ratedType.Ratings.ContainsKey(ev.EntityId))
                        ratedType.Ratings.Add(ev.EntityId, new RatingEntityInfo());

                    var ratedEntity = ratedType.Ratings[ev.EntityId];
                    ratedEntity.AddRating(ev.UserId, ev.Value, ev.Timestamp);
                    break;
            }
        }

        private RatingTypeInfo GetOrCreateRatingTypeInfo(ResourceType ratingType)
        {
            if (_rateDictionary.TryGetValue(ratingType, out var info))
                return info;

            return _rateDictionary[ratingType] = new RatingTypeInfo();
        }

    }

    class RatingTypeInfo
    {
        public int MaximumId { get; set; } = -1;

        //the key is the exhibit index, and the value is an object contains the rating info of the specified exhibit
        public Dictionary<int, RatingEntityInfo> Ratings { get; } = new Dictionary<int, RatingEntityInfo>();

    }

    class RatingEntityInfo
    {

        public double AverageRate { get { return (NumberRates != 0) ? ((double)_sumRate / NumberRates) : 0; } }
        public int NumberRates { get; private set; }

        // <UserId, RatingStruct contains the rate value and its date>
        private Dictionary<string, RatingDateStruct> _allRates = new Dictionary<string, RatingDateStruct>();
        private int _sumRate;

        public void AddRating(string userId, byte rate, DateTimeOffset rateDate)
        {
            if (_allRates.TryGetValue(userId, out var oldRate))
            {
                if (CalculateAverageRating(oldRate.Rate, rate))
                    _allRates[userId] = new RatingDateStruct { Rate = rate, RateDate = rateDate };
            }
            else
            {
                if (CalculateAverageRating(null, rate))
                    _allRates.Add(userId, new RatingDateStruct { Rate = rate, RateDate = rateDate });
            }
        }

        public byte? GetUserRating(string userId)
        {
            return _allRates.TryGetValue(userId, out var rating) ? (byte?)rating.Rate : null;
        }

        public Dictionary<int, int> GetRatingTable()
        {
            Dictionary<int, int> table = new Dictionary<int, int> { { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 0 }, { 5, 0 } };
            foreach (var rateStruct in _allRates)
            {
                table[rateStruct.Value.Rate]++;
            }
            return table;
        }

        public double CalculateLastMonthAverageRating()
        {
            var nowDate = DateTimeOffset.Now;
            double lastMonthTotalRating = 0, lastMonthAverage = 0;
            int counter = 0;
            foreach (var rateStruct in _allRates)
            {
                if (rateStruct.Value.RateDate.Month == (nowDate.Month - 1) && rateStruct.Value.RateDate.Year == nowDate.Year)
                {
                    counter++;
                    lastMonthTotalRating += rateStruct.Value.Rate;
                }
            }
            if (counter > 0)
                lastMonthAverage = lastMonthTotalRating / counter;
            return lastMonthAverage;
        }

        bool CalculateAverageRating(int? oldRate, int newRate)
        {
            if (!(IsOldRatingValid(oldRate) && CheckRatingRange(newRate)))
                return false;

            if (oldRate != null)
                _sumRate += (newRate - oldRate.GetValueOrDefault());
            else
            {
                _sumRate += newRate;
                NumberRates++;
            }

            return true;
        }
        bool IsOldRatingValid(int? oldRate)
        {
            return oldRate == null || CheckRatingRange(oldRate.GetValueOrDefault());
        }
        bool CheckRatingRange(int rating)
        {
            return (rating >= RatingArgs.MinRateValue && rating <= RatingArgs.MaxRateValue);
        }

    }

    struct RatingDateStruct
    {
        public byte Rate;
        public DateTimeOffset RateDate;
    }
}
