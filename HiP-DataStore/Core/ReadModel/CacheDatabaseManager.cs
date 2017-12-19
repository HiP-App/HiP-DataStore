using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using PaderbornUniversity.SILab.Hip.DataStore.Model;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using PaderbornUniversity.SILab.Hip.DataStore.Utility;
using PaderbornUniversity.SILab.Hip.EventSourcing;
using PaderbornUniversity.SILab.Hip.EventSourcing.Events;
using PaderbornUniversity.SILab.Hip.EventSourcing.EventStoreLlp;
using PaderbornUniversity.SILab.Hip.EventSourcing.Mongo;
using System.Linq;
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
            EventStoreService eventStore,
            ILogger<CacheDatabaseManager> logger)
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
            if (ev is DeletedEvent baseEvent)
            {
                var entity = (baseEvent.GetEntityType(), baseEvent.Id);
                _db.ClearIncomingReferences(entity);
                _db.ClearOutgoingReferences(entity);
            }

            object oldValue = null;
            switch (ev)
            {
                case MediaFileUpdated e:


                    //var fileDocBson = e.ToBsonDocument();
                    //fileDocBson.Remove("_id");
                    //var bsonDoc = new BsonDocument("$set", fileDocBson);
                    //_db.GetCollection<MediaElement>(ResourceTypes.Media).UpdateOne(x => x.Id == e.Id, bsonDoc);
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
                    }
                    break;

                case PropertyChangedEvent e:
                    resourceType = e.GetEntityType();
                    switch (resourceType)
                    {
                        case ResourceType _ when resourceType == ResourceTypes.Exhibit:
                            var originalExhibit = _db.Get<Exhibit>((ResourceTypes.Exhibit, e.Id));
                            var exhibitArgs = originalExhibit.CreateExhibitArgs();
                            var propertyInfo = typeof(ExhibitArgs).GetProperty(e.PropertyName);
                            oldValue = propertyInfo.GetValue(exhibitArgs);
                            e.ApplyTo(exhibitArgs);
                            var updatedExhibit = new Exhibit(exhibitArgs)
                            {
                                Id = e.Id,
                                UserId = originalExhibit.UserId,
                                Timestamp = e.Timestamp
                            };
                            updatedExhibit.References.AddRange(originalExhibit.References);
                            updatedExhibit.Referencers.AddRange(originalExhibit.Referencers);
                            _db.Replace((ResourceTypes.Exhibit, e.Id), updatedExhibit);
                            break;

                        case ResourceType _ when resourceType == ResourceTypes.ExhibitPage:
                            var originalExhibitPage = _db.Get<ExhibitPage>((ResourceTypes.ExhibitPage, e.Id));
                            var pageArgs = originalExhibitPage.CreateExhibitPageArgs();
                            propertyInfo = typeof(ExhibitPageArgs2).GetProperty(e.PropertyName);
                            oldValue = propertyInfo.GetValue(pageArgs);
                            e.ApplyTo(pageArgs);
                            var updatedExhibitPage = new ExhibitPage(pageArgs)
                            {
                                Id = e.Id,
                                UserId = originalExhibitPage.UserId,
                                Timestamp = e.Timestamp
                            };
                            updatedExhibitPage.References.AddRange(originalExhibitPage.References);
                            updatedExhibitPage.Referencers.AddRange(originalExhibitPage.Referencers);
                            _db.Replace((ResourceTypes.ExhibitPage, e.Id), updatedExhibitPage);
                            break;

                        case ResourceType _ when resourceType == ResourceTypes.Media:
                            var originalMedium = _db.Get<MediaElement>((ResourceTypes.Media, e.Id));
                            var mediaArgs = originalMedium.CreateMediaArgs();
                            propertyInfo = typeof(MediaArgs).GetProperty(e.PropertyName);
                            oldValue = propertyInfo.GetValue(mediaArgs);
                            e.ApplyTo(mediaArgs);
                            var updatedMedium = new MediaElement(mediaArgs)
                            {
                                Id = e.Id,
                                UserId = originalMedium.UserId,
                                Timestamp = e.Timestamp
                            };
                            updatedMedium.File = originalMedium.File;
                            updatedMedium.References.AddRange(originalMedium.References);
                            updatedMedium.Referencers.AddRange(originalMedium.Referencers);
                            _db.Replace((ResourceTypes.Media, e.Id), updatedMedium);
                            break;

                        case ResourceType _ when resourceType == ResourceTypes.Route:
                            var originalRoute = _db.Get<Route>((ResourceTypes.Route, e.Id));
                            var routeArgs = originalRoute.CreateRouteArgs();
                            propertyInfo = typeof(RouteArgs).GetProperty(e.PropertyName);
                            oldValue = propertyInfo.GetValue(routeArgs);
                            e.ApplyTo(routeArgs);
                            var updatedRoute = new Route(routeArgs)
                            {
                                Id = e.Id,
                                UserId = originalRoute.UserId,
                                Timestamp = e.Timestamp
                            };
                            updatedRoute.References.AddRange(originalRoute.References);
                            updatedRoute.Referencers.AddRange(originalRoute.Referencers);
                            _db.Replace((ResourceTypes.Route, e.Id), updatedRoute);
                            break;

                        case ResourceType _ when resourceType == ResourceTypes.Tag:
                            var originalTag = _db.Get<Tag>((ResourceTypes.Tag, e.Id));
                            var tagArgs = originalTag.CreateTagArgs();
                            propertyInfo = typeof(TagArgs).GetProperty(e.PropertyName);
                            oldValue = propertyInfo.GetValue(tagArgs);
                            e.ApplyTo(tagArgs);
                            var updatedTag = new Tag(tagArgs)
                            {
                                Id = e.Id,
                                UserId = originalTag.UserId,
                                Timestamp = e.Timestamp
                            };
                            updatedTag.References.AddRange(originalTag.References);
                            updatedTag.Referencers.AddRange(originalTag.Referencers);
                            _db.Replace((ResourceTypes.Tag, e.Id), updatedTag);
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
                    }
                    break;
            }

            if (ev is PropertyChangedEvent propEvent)
            {
                var (addedReferences, removedReferences) = propEvent.GetReferenceDifferences(oldValue);

                foreach (var remove in removedReferences)
                {
                    _db.RemoveReference((propEvent.GetEntityType(), propEvent.Id), remove);
                }

                if (addedReferences.Any())
                    _db.AddReferences((propEvent.GetEntityType(), propEvent.Id), addedReferences);
            }
        }

        private void MarkDeleted(EntityId entity)
        {
            _db.Update<ContentBase>(entity, update => update.Set(o => o.Status, ContentStatus.Published));
        }


        /*
        private void ClearIncomingReferences(EntityId entity)
        {
            var currentReferencers = _db.GetCollection<dynamic>(entity.Type.Name)
                .Find(Builders<dynamic>.Filter.Eq("_id", entity.Id))
                .First()
                .Referencers;

            var filteredReferences = ((IEnumerable<dynamic>)currentReferencers).Distinct();

            foreach (var r in filteredReferences)
            {
                // Note: We must use the internal key "_id" here since we use dynamic objects
                var source = (ResourceType.ResourceTypeDictionary[(string)r.Collection], (int)r._id);
                RemoveReference(source, entity);
            }
        }


        private void ClearOutgoingReferences(EntityId entity)
        {
            var currentReferencers = _db.GetCollection<dynamic>(entity.Type.Name)
                .Find(Builders<dynamic>.Filter.Eq("_id", entity.Id))
                .First()
                .References;

            var filteredReferencers = ((IEnumerable<dynamic>)currentReferencers).Distinct();

            foreach (var r in filteredReferencers)
            {
                // Note: We must use the internal key "_id" here since we use dynamic objects
                var target = (ResourceType.ResourceTypeDictionary[(string)r.Collection], (int)r._id);
                RemoveReference(entity, target);
            }
        }

        private void AddReferences(EntityId source, IEnumerable<EntityId> targets)
        {
            // for each reference (source -> target)...

            // 1) create a new DocRef pointing to the target and add it to the source's references list
            var targetRefs = targets.Select(target => new DocRef<ContentBase>(target.Id, target.Type.Name));
            var update = Builders<ContentBase>.Update.AddToSetEach(nameof(ContentBase.References), targetRefs);
            _db.GetCollection<ContentBase>(source.Type.Name).UpdateOne(x => x.Id == source.Id, update);

            // 2) create a new DocRef pointing to the source and add it to the target's referencers list
            var sourceRef = new DocRef<ContentBase>(source.Id, source.Type.Name);
            var update2 = Builders<ContentBase>.Update.AddToSet(nameof(ContentBase.Referencers), sourceRef);
            foreach (var target in targets)
            {
                _db.GetCollection<ContentBase>(target.Type.Name).UpdateOne(x => x.Id == target.Id, update2);
            }
        }

        private void RemoveReference(EntityId source, EntityId target)
        {
            // 1) delete the DocRef pointing to the target from the source's references list
            var update = Builders<dynamic>.Update.PullFilter(
                nameof(ContentBase.References),
                Builders<dynamic>.Filter.And(
                    Builders<dynamic>.Filter.Eq(nameof(DocRefBase.Collection), target.Type.Name),
                    Builders<dynamic>.Filter.Eq("_id", target.Id)));

            _db.GetCollection<dynamic>(source.Type.Name).UpdateOne(
                Builders<dynamic>.Filter.Eq("_id", source.Id), update);

            // 2) delete the DocRef pointing to the source from the target's referencers list
            var update2 = Builders<dynamic>.Update.PullFilter(
                nameof(ContentBase.Referencers),
                Builders<dynamic>.Filter.And(
                    Builders<dynamic>.Filter.Eq(nameof(DocRefBase.Collection), source.Type.Name),
                    Builders<dynamic>.Filter.Eq("_id", source.Id)));

            _db.GetCollection<dynamic>(target.Type.Name).UpdateOne(
                Builders<dynamic>.Filter.Eq("_id", target.Id), update2);
        }
        */
    }
}
