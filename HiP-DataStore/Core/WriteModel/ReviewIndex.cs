using PaderbornUniversity.SILab.Hip.DataStore.Model;
using PaderbornUniversity.SILab.Hip.DataStore.Utility;
using PaderbornUniversity.SILab.Hip.EventSourcing;
using PaderbornUniversity.SILab.Hip.EventSourcing.Events;
using System;
using System.Collections.Generic;
using System.Security.Principal;
using static PaderbornUniversity.SILab.Hip.DataStore.Model.Rest.ReviewResult;

namespace PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel
{
    public class ReviewIndex : IDomainIndex
    {
        private readonly Dictionary<ResourceType, ReviewTypeInfo> _reviewDictionary = new Dictionary<ResourceType, ReviewTypeInfo>();
        private readonly object _lockObject = new object();

        public int NextId(ResourceType entityType)
        {
            lock (_lockObject)
            {
                var info = GetOrCreateReviewTypeInfo(entityType);
                return ++info.MaximumId;
            }
        }

        public string Owner(ResourceType entityType, int id)
        {
            lock (_lockObject)
            {
                var info = GetOrCreateReviewTypeInfo(entityType);

                if (info.Reviews.TryGetValue(id, out var review))
                    return review.UserId;

                return null;
            }
        }

        public bool Approved(ResourceType entityType, int id)
        {
            lock (_lockObject)
            {
                var info = GetOrCreateReviewTypeInfo(entityType);

                if (info.Reviews.TryGetValue(id, out var review))
                    return review.Approved;

                return false;
            }
        }

        public bool ReviewApproved(ResourceType entityType, int id, IIdentity identity, bool approve)
        {
            lock (_lockObject)
            {
                var info = GetOrCreateReviewTypeInfo(entityType);

                if (info.Reviews.TryGetValue(id, out var review))
                {
                    var count = 0;
                    foreach (Comment comment in review.Comments)
                    {
                        if (comment.Approved)
                            count++;
                    }

                    if (approve)
                        count++;

                    var studentsToApprove = StudentsToApprove(entityType, id);
                    // amount of students (if permitted) or one supervisor need to approve, to approve the review
                    if ((count >= studentsToApprove && studentsToApprove > 0)
                        || (approve && UserPermissions.IsSupervisorOrAdmin(identity)))
                        return true;
                }
                return false;
            }
        }

        public string Description(ResourceType entityType, int id)
        {
            lock (_lockObject)
            {
                var info = GetOrCreateReviewTypeInfo(entityType);

                if (info.Reviews.TryGetValue(id, out var review))
                    return review.Description;

                return null;
            }
        }

        public List<string> Reviewers(ResourceType entityType, int id)
        {
            lock (_lockObject)
            {
                var info = GetOrCreateReviewTypeInfo(entityType);

                if (info.Reviews.TryGetValue(id, out var review))
                    return review.Reviewers;

                return null;
            }
        }

        public List<Comment> Comments(ResourceType entityType, int id)
        {
            lock (_lockObject)
            {
                var info = GetOrCreateReviewTypeInfo(entityType);

                if (info.Reviews.TryGetValue(id, out var review))
                    return review.Comments;

                return null;
            }
        }

        public DateTimeOffset Timestamp(ResourceType entityType, int id)
        {
            lock (_lockObject)
            {
                var info = GetOrCreateReviewTypeInfo(entityType);

                if (info.Reviews.TryGetValue(id, out var review))
                    return review.Timestamp;

                return DateTimeOffset.MaxValue;
            }
        }

        public bool Exists(ResourceType entityType, int id)
        {
            lock (_lockObject)
            {
                var info = GetOrCreateReviewTypeInfo(entityType);

                if (info.Reviews.ContainsKey(id))
                    return true;

                return false;
            }
        }

        public int StudentsToApprove(ResourceType entityType, int id)
        {
            lock (_lockObject)
            {
                var info = GetOrCreateReviewTypeInfo(entityType);

                if (info.Reviews.TryGetValue(id, out var review))
                    return review.StudentsToApprove;

                return -1;
            }
        }

        public bool ReviewableByStudents(ResourceType entityType, int id)
        {
            lock (_lockObject)
            {
                var info = GetOrCreateReviewTypeInfo(entityType);

                if (info.Reviews.TryGetValue(id, out var review))
                    return review.ReviewableByStudents;

                return false;
            }
        }

        public void ApplyEvent(IEvent e)
        {
            switch (e)
            {
                case CreatedEvent ev:
                    if (ev.GetEntityType() == ResourceTypes.ExhibitReview || ev.GetEntityType() == ResourceTypes.ExhibitPageReview
                        || ev.GetEntityType() == ResourceTypes.RouteReview)
                    {
                        lock (_lockObject)
                        {
                            var resourceType = ev.GetEntityType();
                            var infoCreate = new ReviewTypeInfo();
                            switch (resourceType)
                            {
                                case ResourceType _ when resourceType == ResourceTypes.ExhibitReview:
                                    infoCreate = GetOrCreateReviewTypeInfo(ResourceTypes.Exhibit);
                                    break;

                                case ResourceType _ when resourceType == ResourceTypes.ExhibitPageReview:
                                    infoCreate = GetOrCreateReviewTypeInfo(ResourceTypes.ExhibitPage);
                                    break;

                                case ResourceType _ when resourceType == ResourceTypes.RouteReview:
                                    infoCreate = GetOrCreateReviewTypeInfo(ResourceTypes.Route);
                                    break;
                            }
                            infoCreate.MaximumId = Math.Max(infoCreate.MaximumId, ev.Id);
                        }
                    }
                    break;

                case PropertyChangedEvent ev:
                    if (ev.GetEntityType() == ResourceTypes.ExhibitReview || ev.GetEntityType() == ResourceTypes.ExhibitPageReview
                        || ev.GetEntityType() == ResourceTypes.RouteReview)
                    {
                        lock (_lockObject)
                        {
                            var resourceType = ev.GetEntityType();
                            var infoUpdate = new ReviewTypeInfo();
                            switch (resourceType)
                            {
                                case ResourceType _ when resourceType == ResourceTypes.ExhibitReview:
                                    infoUpdate = GetOrCreateReviewTypeInfo(ResourceTypes.Exhibit);
                                    break;

                                case ResourceType _ when resourceType == ResourceTypes.ExhibitPageReview:
                                    infoUpdate = GetOrCreateReviewTypeInfo(ResourceTypes.ExhibitPage);
                                    break;

                                case ResourceType _ when resourceType == ResourceTypes.RouteReview:
                                    infoUpdate = GetOrCreateReviewTypeInfo(ResourceTypes.Route);
                                    break;
                            }

                            if (!infoUpdate.Reviews.TryGetValue(ev.Id, out var review))
                            {
                                infoUpdate.Reviews.Add(ev.Id, new ReviewEntityInfo { UserId = ev.UserId, Timestamp = ev.Timestamp });
                                infoUpdate.Reviews.TryGetValue(ev.Id, out review);
                                
                            }

                            if (ev.PropertyName == nameof(Comment))
                            {
                                if (review != null)
                                    review.Comments.Add(ev.Value as Comment);
                            }
                            else
                            {
                                ev.ApplyTo(review);
                                infoUpdate.Reviews[ev.Id] = review;
                            }

                        }
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

            public bool Approved { get; private set; }

            public string Description { get; private set; }

            public int StudentsToApprove { get; private set; }

            public bool ReviewableByStudents { get; private set; }

            public DateTimeOffset Timestamp { get; set; }

            public List<string> Reviewers { get; private set; }

            public List<Comment> Comments { get; private set; } = new List<Comment>();
        }
    }
}
