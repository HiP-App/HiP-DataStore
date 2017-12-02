using PaderbornUniversity.SILab.Hip.DataStore.Model;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using PaderbornUniversity.SILab.Hip.EventSourcing;
using System;
using System.Collections.Generic;
using static PaderbornUniversity.SILab.Hip.DataStore.Model.Rest.ReviewResult;

namespace PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel
{
    public class ReviewIndex : IDomainIndex
    {
        private readonly Dictionary<ResourceType, ReviewTypeInfo> _reviewDictionary = new Dictionary<ResourceType, ReviewTypeInfo>();

        public int NextId(ResourceType entityType)
        {
            var info = GetOrCreateReviewTypeInfo(entityType);
            return ++info.MaximumId;
        }

        public string Owner(ResourceType entityType, int id)
        {
            var info = GetOrCreateReviewTypeInfo(entityType);

            if (info.Reviews.TryGetValue(id, out var review))
                return review.UserId;

            return null;
        }

        public bool Approved(ResourceType entityType, int id)
        {
            var info = GetOrCreateReviewTypeInfo(entityType);

            if (info.Reviews.TryGetValue(id, out var review))
                return review.Approved;

            return false;
        }

        public string Description(ResourceType entityType, int id)
        {
            var info = GetOrCreateReviewTypeInfo(entityType);

            if (info.Reviews.TryGetValue(id, out var review))
                return review.Description;

            return null;
        }

        public List<string> Reviewers(ResourceType entityType, int id)
        {
            var info = GetOrCreateReviewTypeInfo(entityType);

            if (info.Reviews.TryGetValue(id, out var review))
                return review.Reviewers;

            return null;
        }

        public List<Comment> Comments(ResourceType entityType, int id)
        {
            var info = GetOrCreateReviewTypeInfo(entityType);

            if (info.Reviews.TryGetValue(id, out var review))
                return review.Comments;

            return null;
        }

        public DateTimeOffset Timestamp(ResourceType entityType, int id)
        {
            var info = GetOrCreateReviewTypeInfo(entityType);

            if (info.Reviews.TryGetValue(id, out var review))
                return review.Timestamp;

            return DateTimeOffset.MaxValue;
        }

        public bool Exists(ResourceType entityType, int id)
        {
            var info = GetOrCreateReviewTypeInfo(entityType);

            if (info.Reviews.ContainsKey(id))
                return true;

            return false;
        }

        public void ApplyEvent(IEvent e)
        {
            switch(e)
            {
                case ReviewCreated ev:
                    var infoCreate = GetOrCreateReviewTypeInfo(ev.ReviewType);
                    infoCreate.MaximumId = Math.Max(infoCreate.MaximumId, ev.Id);
                    infoCreate.Reviews.Add(ev.EntityId, new ReviewEntityInfo { UserId = ev.UserId, Description = ev.Description,
                        Reviewers = ev.Reviewers, Timestamp = ev.Timestamp});
                    break;
                case ReviewUpdated ev:
                    var infoUpdate = GetOrCreateReviewTypeInfo(ev.ReviewType);
                    if (infoUpdate.Reviews.TryGetValue(ev.EntityId, out var review))
                    {
                        review.Approved = ev.Approved;
                        review.Comments.Add(new Comment(ev.Comment, ev.Timestamp, ev.UserId));
                        if (ev.Reviewers != null)
                            review.Reviewers = ev.Reviewers;
                    }
                    break;
            }
        }

        private ReviewTypeInfo GetOrCreateReviewTypeInfo(ResourceType entityType)
        {
            if (_reviewDictionary.TryGetValue(entityType, out var info))
                return info;

            return _reviewDictionary[entityType] = new ReviewTypeInfo();
        }

        class ReviewTypeInfo
        {
            public int MaximumId { get; set; } = -1;

            public Dictionary<int, ReviewEntityInfo> Reviews { get; } = new Dictionary<int, ReviewEntityInfo>();
        }

        class ReviewEntityInfo
        {
            public string UserId { get; set; }

            public bool Approved { get; set; } = false;

            public string Description { get; set; }

            public DateTimeOffset Timestamp { get; set; }

            public List<string> Reviewers { get; set; }

            public List<Comment> Comments { get; set; } = new List<Comment>();
        }
    }
}
