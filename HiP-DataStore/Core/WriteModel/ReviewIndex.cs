using PaderbornUniversity.SILab.Hip.DataStore.Model;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using PaderbornUniversity.SILab.Hip.EventSourcing;
using PaderbornUniversity.SILab.Hip.EventSourcing.Events;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel
{
    public class ReviewIndex : IDomainIndex
    {
        private readonly Dictionary<int, ReviewEntityInfo> _reviews = new Dictionary<int, ReviewEntityInfo>();
        private readonly object _lockObject = new object();

        private int _maximumId = -1;

        public int NextId(ResourceType entityType)
        {
            lock (_lockObject)
            {
                return ++_maximumId;
            }
        }

        public string Owner(int reviewId)
        {
            lock (_lockObject)
            {
                if (_reviews.TryGetValue(reviewId, out var review))
                    return review.UserId;

                return null;
            }
        }

        public bool Exists(string entityType, int entityId)
        {
            lock (_lockObject)
            {
                var review = _reviews.FirstOrDefault(x => x.Value.EntityId == entityId && x.Value.EntityType == entityType);
                if (review.Value != null)
                    return true;
                return false;
            }
        }

        /// <summary>
        /// Returns the id of the review that belongs to the entity specified by the given ID and type.
        /// </summary>
        public int GetReviewId(string entityType, int entityId)
        {
            lock (_lockObject)
            {
                return _reviews.FirstOrDefault(x => x.Value.EntityId == entityId && x.Value.EntityType == entityType).Key;
            }
        }

        public void ApplyEvent(IEvent e)
        {
            switch (e)
            {
                case CreatedEvent ev:
                    if (ev.GetEntityType() == ResourceTypes.Review)
                    {
                        lock (_lockObject)
                        {
                            _maximumId = Math.Max(_maximumId, ev.Id);
                            _reviews.Add(ev.Id, new ReviewEntityInfo { UserId = ev.UserId });
                        }
                    }
                    break;

                case PropertyChangedEvent ev:
                    if (ev.GetEntityType() == ResourceTypes.Review)
                    {
                        lock (_lockObject)
                        {
                            if (_reviews.TryGetValue(ev.Id, out var review))
                            {
                                if (ev.PropertyName == nameof(Review.EntityType) && ev.Value is string entityType)
                                {
                                    review.EntityType = entityType;
                                }
                                else if (ev.PropertyName == nameof(Review.EntityId) && ev.Value is int entityId)
                                {
                                    review.EntityId = entityId;
                                }
                            }
                        }
                    }
                    break;

                case DeletedEvent ev:
                    if (ev.GetEntityType() == ResourceTypes.Review)
                    {
                        lock (_lockObject)
                        {
                            _reviews.Remove(ev.Id);
                        }
                    }
                    break;
            }
        }

        class ReviewEntityInfo
        {
            public string EntityType { get; set; }

            public int EntityId { get; set; }

            public String UserId { get; set; }
        }
    }
}
