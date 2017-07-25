﻿using PaderbornUniversity.SILab.Hip.DataStore.Model;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;

using System.Collections.Generic;
using System;

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

        public double Average(ResourceType res,int id)
        {
            return GetOrCreateRatingTypeInfo(res).Ratings.TryGetValue(id, out var t) ? t.AverageRate : 0;
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
                    ratedEntity.AddRating(ev.UserId, ev.Value);
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

        public Dictionary<int, RatingEntityInfo> Ratings { get; } = new Dictionary<int, RatingEntityInfo>();

    }
    class RatingEntityInfo
    {

        public double AverageRate { get { return (NumberRates != 0) ? ((double)_sumRate / NumberRates) : 0; }  }
        public int NumberRates { get; private set; }

        // <UserId, Rating>
        private Dictionary<int, byte> _allRates = new Dictionary<int, byte>();
        private int _sumRate;

        public void AddRating(int userId, byte rate)
        {
            if (_allRates.TryGetValue(userId, out var oldRate))
            {
                if(CalculateAverageRating(oldRate, rate))
                    _allRates[userId] = rate;
            }
            else
            {
                if(CalculateAverageRating(null, rate))
                    _allRates.Add(userId, rate);
            }
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
}