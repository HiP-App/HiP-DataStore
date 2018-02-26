﻿using PaderbornUniversity.SILab.Hip.DataStore.Model;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using PaderbornUniversity.SILab.Hip.EventSourcing;
using PaderbornUniversity.SILab.Hip.EventSourcing.Events;
using PaderbornUniversity.SILab.Hip.EventSourcing.EventStoreLlp;
using PaderbornUniversity.SILab.Hip.EventSourcing.Mongo;
using Tag = PaderbornUniversity.SILab.Hip.DataStore.Model.Entity.Tag;

namespace PaderbornUniversity.SILab.Hip.DataStore.Core.ReadModel
{
    /// <summary>
    /// Subscribes to EventStore events to keep the cache database up to date.
    /// </summary>
    public class CacheDatabaseManager
    {
        private readonly EventStoreService _eventStore;
        private readonly IMongoDbContext _db;

        public CacheDatabaseManager(
            IMongoDbContext db,
            EventStoreService eventStore)
        {
            // For now, the cache database is always created from scratch by replaying all events.
            // This also implies that, for now, the cache database always contains the entire data (not a subset).
            // In order to receive all the events, a Catch-Up Subscription is created.
            _db = db;

            // Subscribe to EventStore to receive all past and future events
            _eventStore = eventStore;
            _eventStore.EventStream.SubscribeCatchUp(ApplyEvent);
        }

        private void ApplyEvent(IEvent ev)
        {
            switch (ev)
            {
                case MediaFileUpdated e:
                    _db.Update<MediaElement>((ResourceTypes.Media, e.Id), update =>
                    {
                        update.Set(m => m.File, e.File);
                        update.Set(m => m.Timestamp, e.Timestamp);
                    });
                    break;

                case ScoreAdded e:
                    var newScoreRecord = new ScoreRecord
                    {
                        Id = e.Id,
                        UserId = e.UserId,
                        Score = e.Score,
                        Timestamp = e.Timestamp
                    };
                    _db.Add(ResourceTypes.ScoreRecord, newScoreRecord);
                    break;

                case CreatedEvent e:
                    var resourceType = e.GetEntityType();
                    switch (resourceType)
                    {
                        case ResourceType _ when resourceType == ResourceTypes.Exhibit:
                            var newExhibit = new Exhibit(new ExhibitArgs())
                            {
                                Id = e.Id,
                                UserId = e.UserId,
                                Timestamp = e.Timestamp
                            };
                            _db.Add(ResourceTypes.Exhibit, newExhibit);
                            break;

                        case ResourceType _ when resourceType == ResourceTypes.ExhibitPage:
                            var newExhibitPage = new ExhibitPage(new ExhibitPageArgs2())
                            {
                                Id = e.Id,
                                UserId = e.UserId,
                                Timestamp = e.Timestamp
                            };
                            _db.Add(ResourceTypes.ExhibitPage, newExhibitPage);
                            break;

                        case ResourceType _ when resourceType == ResourceTypes.Media:
                            var newMedium = new MediaElement(new MediaArgs())
                            {
                                Id = e.Id,
                                UserId = e.UserId,
                                Timestamp = e.Timestamp
                            };
                            _db.Add(ResourceTypes.Media, newMedium);
                            break;

                        case ResourceType _ when resourceType == ResourceTypes.Route:
                            var newRoute = new Route(new RouteArgs())
                            {
                                Id = e.Id,
                                UserId = e.UserId,
                                Timestamp = e.Timestamp
                            };
                            _db.Add(ResourceTypes.Route, newRoute);
                            break;
                        case ResourceType _ when resourceType == ResourceTypes.Tag:
                            var newTag = new Tag(new TagArgs())
                            {
                                Id = e.Id,
                                UserId = e.UserId,
                                Timestamp = e.Timestamp
                            };
                            _db.Add(ResourceTypes.Tag, newTag);
                            break;
                        case ResourceType _ when resourceType == ResourceTypes.Quiz:
                            var newQuiz = new Quiz(new ExhibitQuizArgs())
                            {
                                Id = e.Id,
                                UserId = e.UserId,
                                Timestamp = e.Timestamp
                            };
                            _db.Add(ResourceTypes.Quiz, newQuiz);
                            break;

                        case ResourceType _ when resourceType == ResourceTypes.Review:
                            var newReview = new Review(new ReviewArgs())
                            {
                                Id = e.Id,
                                UserId = e.UserId,
                                Timestamp = e.Timestamp
                            };
                            _db.Add(ResourceTypes.Review, newReview);
                            break;

                        case ResourceType _ when resourceType == ResourceTypes.ReviewComment:
                            var newReviewComment = new ReviewComment(new ReviewCommentArgs())
                            {
                                Id = e.Id,
                                UserId = e.UserId,
                                Timestamp = e.Timestamp
                            };
                            _db.Add(ResourceTypes.ReviewComment, newReviewComment);
                            break;
                    }
                    break;

                case PropertyChangedEvent e:
                    resourceType = e.GetEntityType();
                    switch (resourceType)
                    {
                        case ResourceType _ when resourceType == ResourceTypes.Exhibit:
                            var originalExhibit = _db.Get<Exhibit>((ResourceTypes.Exhibit, e.Id));
                            var exhibitArgs = originalExhibit.CreateExhibitArgs();
                            e.ApplyTo(exhibitArgs);
                            var updatedExhibit = new Exhibit(exhibitArgs)
                            {
                                Id = e.Id,
                                UserId = originalExhibit.UserId,
                                Timestamp = e.Timestamp
                            };
                            _db.Replace((ResourceTypes.Exhibit, e.Id), updatedExhibit);
                            break;

                        case ResourceType _ when resourceType == ResourceTypes.ExhibitPage:
                            var originalExhibitPage = _db.Get<ExhibitPage>((ResourceTypes.ExhibitPage, e.Id));
                            var pageArgs = originalExhibitPage.CreateExhibitPageArgs();
                            e.ApplyTo(pageArgs);
                            var updatedExhibitPage = new ExhibitPage(pageArgs)
                            {
                                Id = e.Id,
                                UserId = originalExhibitPage.UserId,
                                Timestamp = e.Timestamp
                            };
                            _db.Replace((ResourceTypes.ExhibitPage, e.Id), updatedExhibitPage);
                            break;

                        case ResourceType _ when resourceType == ResourceTypes.Quiz:
                            var originalQuiz = _db.Get<Quiz>((ResourceTypes.Quiz, e.Id));
                            var quizArgs = originalQuiz.CreateQuizArgs();
                            e.ApplyTo(quizArgs);
                            var updatedQuiz = new Quiz(quizArgs)
                            {
                                Id = e.Id,
                                UserId = e.UserId,
                                Timestamp = e.Timestamp
                            };
                            updatedQuiz.References.AddRange(originalQuiz.References);
                            updatedQuiz.Referencers.AddRange(originalQuiz.Referencers);
                            _db.Replace((ResourceTypes.Quiz, e.Id), updatedQuiz);
                            break;

                        case ResourceType _ when resourceType == ResourceTypes.Media:
                            var originalMedium = _db.Get<MediaElement>((ResourceTypes.Media, e.Id));
                            var mediaArgs = originalMedium.CreateMediaArgs();
                            e.ApplyTo(mediaArgs);
                            var updatedMedium = new MediaElement(mediaArgs)
                            {
                                Id = e.Id,
                                UserId = originalMedium.UserId,
                                Timestamp = e.Timestamp
                            };
                            updatedMedium.File = originalMedium.File;
                            _db.Replace((ResourceTypes.Media, e.Id), updatedMedium);
                            break;

                        case ResourceType _ when resourceType == ResourceTypes.Route:
                            var originalRoute = _db.Get<Route>((ResourceTypes.Route, e.Id));
                            var routeArgs = originalRoute.CreateRouteArgs();
                            e.ApplyTo(routeArgs);
                            var updatedRoute = new Route(routeArgs)
                            {
                                Id = e.Id,
                                UserId = originalRoute.UserId,
                                Timestamp = e.Timestamp
                            };
                            _db.Replace((ResourceTypes.Route, e.Id), updatedRoute);
                            break;

                        case ResourceType _ when resourceType == ResourceTypes.Tag:
                            var originalTag = _db.Get<Tag>((ResourceTypes.Tag, e.Id));
                            var tagArgs = originalTag.CreateTagArgs();
                            e.ApplyTo(tagArgs);
                            var updatedTag = new Tag(tagArgs)
                            {
                                Id = e.Id,
                                UserId = originalTag.UserId,
                                Timestamp = e.Timestamp
                            };
                            _db.Replace((ResourceTypes.Tag, e.Id), updatedTag);
                            break;

                        case ResourceType _ when resourceType == ResourceTypes.Review:
                            var originalReview = _db.Get<Review>((ResourceTypes.Review, e.Id));
                            var reviewArgs = originalReview.CreateReviewArgs();
                            e.ApplyTo(reviewArgs);
                            var updatedReview = new Review(reviewArgs)
                            {
                                Id = e.Id,
                                UserId = originalReview.UserId,
                                Timestamp = e.Timestamp,
                            };
                            _db.Replace((ResourceTypes.Review, e.Id), updatedReview);
                            break;

                        case ResourceType _ when resourceType == ResourceTypes.ReviewComment:
                            var originalReviewComment = _db.Get<ReviewComment>((ResourceTypes.ReviewComment, e.Id));
                            var reviewCommentArgs = originalReviewComment.CreateReviewCommentArgs();
                            e.ApplyTo(reviewCommentArgs);
                            var updatedReviewComment = new ReviewComment(reviewCommentArgs)
                            {
                                Id = e.Id,
                                UserId = originalReviewComment.UserId,
                                Timestamp = e.Timestamp
                            };
                            _db.Replace((ResourceTypes.ReviewComment, e.Id), updatedReviewComment);
                            break;
                    }
                    break;

                case DeletedEvent e:
                    resourceType = e.GetEntityType();
                    switch (resourceType)
                    {
                        case ResourceType _ when resourceType == ResourceTypes.Exhibit:
                            MarkDeleted((resourceType, e.Id));
                            break;
                        case ResourceType _ when resourceType == ResourceTypes.ExhibitPage:
                            MarkDeleted((resourceType, e.Id));
                            break;
                        case ResourceType _ when resourceType == ResourceTypes.Media:
                            MarkDeleted((resourceType, e.Id));
                            break;
                        case ResourceType _ when resourceType == ResourceTypes.Route:
                            MarkDeleted((resourceType, e.Id));
                            break;
                        case ResourceType _ when resourceType == ResourceTypes.Tag:
                            MarkDeleted((resourceType, e.Id));
                            break;
                        case ResourceType _ when resourceType == ResourceTypes.Quiz:
                            MarkDeleted((resourceType, e.Id));
                            break;
                        case ResourceType _ when resourceType == ResourceTypes.Review:
                            MarkDeleted((resourceType, e.Id));
                            break;
                        case ResourceType _ when resourceType == ResourceTypes.ReviewComment:
                            MarkDeleted((resourceType, e.Id));
                            break;
                    }
                    break;
            }
        }

        private void MarkDeleted(EntityId entity)
        {
            _db.Update<ContentBase>(entity, update => update.Set(o => o.Status, ContentStatus.Deleted));
        }
    }
}
