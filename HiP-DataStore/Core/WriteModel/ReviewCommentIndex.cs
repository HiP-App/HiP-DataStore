using PaderbornUniversity.SILab.Hip.DataStore.Model;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using PaderbornUniversity.SILab.Hip.EventSourcing;
using PaderbornUniversity.SILab.Hip.EventSourcing.Events;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel
{
    public class ReviewCommentIndex : IDomainIndex
    {
        private readonly Dictionary<int, ReviewCommentEntityInfo> _reviewComments = new Dictionary<int, ReviewCommentEntityInfo>();
        private readonly object _lockObject = new object();

        private int _maximumId = -1;

        public int NextId()
        {
            lock (_lockObject)
            {
                return ++_maximumId;
            }
        }

        public bool Approved(int id)
        {
            lock (_lockObject)
            {
                if (_reviewComments.TryGetValue(id, out var reviewComment))
                    return reviewComment.Approved;
                return false;
            }
        }

        public string Owner(int reviewCommentId)
        {
            lock (_lockObject)
            {
                if (_reviewComments.TryGetValue(reviewCommentId, out var reviewComment))
                    return reviewComment.UserId;

                return null;
            }
        }

        public bool Exists(int reviewId)
        {
            lock (_lockObject)
            {
                if (_reviewComments.TryGetValue(reviewId, out var reviewComment))
                    return true;
                return false;
            }
        }

        /// <summary>
        /// Returns all the IDs of comments that belong to the specified review
        /// </summary>
        public IReadOnlyCollection<int> GetReviewCommentIds(int reviewId)
        {
            lock (_lockObject)
            {
                return _reviewComments.AsQueryable()
                    .Where(x => x.Value.ReviewId == reviewId)
                    .Select(x => x.Key)
                    .ToList();
            }
        }

        public void ApplyEvent(IEvent e)
        {
            switch (e)
            {
                case CreatedEvent ev:
                    if (ev.GetEntityType() == ResourceTypes.ReviewComment)
                    {
                        lock (_lockObject)
                        {
                            _maximumId = Math.Max(_maximumId, ev.Id);
                            _reviewComments.Add(ev.Id, new ReviewCommentEntityInfo { UserId = ev.UserId });
                        }
                    }
                    break;

                case PropertyChangedEvent ev:
                    if (ev.GetEntityType() == ResourceTypes.ReviewComment)
                    {
                        lock (_lockObject)
                        {
                            if (_reviewComments.TryGetValue(ev.Id, out var reviewComment))
                            {
                                if (ev.PropertyName == nameof(ReviewComment.Approved) && ev.Value is bool approved)
                                {
                                    reviewComment.Approved = approved;
                                } else if (ev.PropertyName == nameof(ReviewComment.ReviewId) && ev.Value is int id)
                                {
                                    reviewComment.ReviewId = id;
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
                            _reviewComments.Remove(ev.Id);
                        }
                    }
                    break;
            }
        }

        class ReviewCommentEntityInfo
        {
            public bool Approved { get; set; }

            public int ReviewId { get; set; }

            public String UserId { get; set; }
        }
    }
}
