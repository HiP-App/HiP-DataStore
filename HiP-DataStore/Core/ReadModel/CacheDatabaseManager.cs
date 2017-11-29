using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private readonly IMongoDatabase _db;

        public IMongoDatabase Database => _db;

        public CacheDatabaseManager(
            EventStoreService eventStore,
            IOptions<EndpointConfig> config,
            ILogger<CacheDatabaseManager> logger)
        {
            // For now, the cache database is always created from scratch by replaying all events.
            // This also implies that, for now, the cache database always contains the entire data (not a subset).
            // In order to receive all the events, a Catch-Up Subscription is created.

            // 1) Open MongoDB connection and clear existing database
            var mongo = new MongoClient(config.Value.MongoDbHost);
            mongo.DropDatabase(config.Value.MongoDbName);
            _db = mongo.GetDatabase(config.Value.MongoDbName);
            var uri = new Uri(config.Value.MongoDbHost);
            logger.LogInformation($"Connected to MongoDB cache database on '{uri.Host}', using database '{config.Value.MongoDbName}'");

            // 2) Subscribe to EventStore to receive all past and future events
            _eventStore = eventStore;
            _eventStore.EventStream.SubscribeCatchUp(ApplyEvent);
        }

        private void ApplyEvent(IEvent ev)
        {
            if (ev is EventBase crudEvent)
            {
                var entity = (crudEvent.GetEntityType(), crudEvent.Id);
                if (crudEvent is DeletedEvent)
                {
                    ClearIncomingReferences(entity);
                    ClearOutgoingReferences(entity);
                }
                else if (crudEvent is PropertyChangedEvent)
                {
                    ClearOutgoingReferences(entity);
                }
            }

            switch (ev)
            {

                case MediaFileUpdated e:
                    var fileDocBson = e.ToBsonDocument();
                    fileDocBson.Remove("_id");
                    var bsonDoc = new BsonDocument("$set", fileDocBson);
                    _db.GetCollection<MediaElement>(ResourceTypes.Media.Name).UpdateOne(x => x.Id == e.Id, bsonDoc);
                    break;

                case ScoreAdded e:
                    var newScoreRecord = new ScoreRecord
                    {
                        Id = e.Id,
                        UserId = e.UserId,
                        Score = e.Score,
                        Timestamp = e.Timestamp
                    };
                    _db.GetCollection<ScoreRecord>(ResourceTypes.ScoreRecord.Name).InsertOne(newScoreRecord);
                    break;
                case CreatedEvent e:

                    var resourceType = e.GetEntityType();
                    switch (resourceType)
                    {
                        case ResourceType _ when resourceType == ResourceTypes.Exhibit:
                            var newExhibit2 = new Exhibit(new ExhibitArgs())
                            {
                                Id = e.Id,
                                UserId = e.UserId,
                                Timestamp = e.Timestamp
                            };
                            _db.GetCollection<Exhibit>(ResourceTypes.Exhibit.Name).InsertOne(newExhibit2);
                            break;
                        case ResourceType _ when resourceType == ResourceTypes.ExhibitPage:
                            var newExhibitPage = new ExhibitPage(new ExhibitPageArgs2())
                            {
                                Id = e.Id,
                                UserId = e.UserId,
                                Timestamp = e.Timestamp
                            };
                            _db.GetCollection<ExhibitPage>(ResourceTypes.ExhibitPage.Name).InsertOne(newExhibitPage);
                            break;

                        case ResourceType _ when resourceType == ResourceTypes.Media:
                            var newMedium = new MediaElement(new MediaArgs())
                            {
                                Id = e.Id,
                                UserId = e.UserId,
                                Timestamp = e.Timestamp
                            };
                            _db.GetCollection<MediaElement>(ResourceTypes.Media.Name).InsertOne(newMedium);
                            break;

                        case ResourceType _ when resourceType == ResourceTypes.Route:
                            var newRoute = new Route(new RouteArgs())
                            {
                                Id = e.Id,
                                UserId = e.UserId,
                                Timestamp = e.Timestamp
                            };
                            _db.GetCollection<Route>(ResourceTypes.Route.Name).InsertOne(newRoute);
                            break;
                        case ResourceType _ when resourceType == ResourceTypes.Tag:
                            var newTag = new Tag(new TagArgs())
                            {
                                Id = e.Id,
                                UserId = e.UserId,
                                Timestamp = e.Timestamp
                            };
                            _db.GetCollection<Tag>(ResourceTypes.Tag.Name).InsertOne(newTag);
                            break;
                    }
                    break;

                case PropertyChangedEvent e:
                    resourceType = e.GetEntityType();


                    switch (resourceType)
                    {
                        case ResourceType _ when resourceType == ResourceTypes.Exhibit:
                            var originalExhibit2 = _db.GetCollection<Exhibit>(ResourceTypes.Exhibit.Name).AsQueryable().First(x => x.Id == e.Id);
                            var exhibitArgs = originalExhibit2.CreateExhibitArgs();
                            var propertyInfo = typeof(ExhibitArgs).GetProperty(e.PropertyName);
                            propertyInfo.SetValue(exhibitArgs, e.Value);
                            var updatedExhibit2 = new Exhibit(exhibitArgs)
                            {
                                Id = e.Id,
                                UserId = e.UserId,
                                Timestamp = e.Timestamp
                            };
                            updatedExhibit2.Referencers.AddRange(originalExhibit2.Referencers);
                            _db.GetCollection<Exhibit>(ResourceTypes.Exhibit.Name).ReplaceOne(x => x.Id == e.Id, updatedExhibit2);
                            break;
                        case ResourceType _ when resourceType == ResourceTypes.ExhibitPage:
                            var originalExhibitPage = _db.GetCollection<ExhibitPage>(ResourceTypes.ExhibitPage.Name).AsQueryable().First(x => x.Id == e.Id);
                            var pageArgs = originalExhibitPage.CreateExhibitPageArgs();
                            propertyInfo = typeof(ExhibitPageArgs2).GetProperty(e.PropertyName);
                            propertyInfo.SetValue(pageArgs, e.Value);
                            var updatedExhibitPage = new ExhibitPage(pageArgs)
                            {
                                Id = e.Id,
                                UserId = e.UserId,
                                Timestamp = e.Timestamp
                            };
                            updatedExhibitPage.Referencers.AddRange(originalExhibitPage.Referencers);
                            _db.GetCollection<ExhibitPage>(ResourceTypes.ExhibitPage.Name).ReplaceOne(p => p.Id == e.Id, updatedExhibitPage);
                            break;

                        case ResourceType _ when resourceType == ResourceTypes.Media:
                            var originalMedium = _db.GetCollection<MediaElement>(ResourceTypes.Media.Name).AsQueryable().First(x => x.Id == e.Id);
                            var mediaArgs = originalMedium.CreateMediaArgs();
                            propertyInfo = typeof(MediaArgs).GetProperty(e.PropertyName);
                            propertyInfo.SetValue(mediaArgs, e.Value);
                            var updatedMedium = new MediaElement(mediaArgs)
                            {
                                Id = e.Id,
                                UserId = e.UserId,
                                Timestamp = e.Timestamp
                            };
                            updatedMedium.Referencers.AddRange(originalMedium.Referencers);
                            _db.GetCollection<MediaElement>(ResourceTypes.Media.Name).ReplaceOne(p => p.Id == e.Id, updatedMedium);
                            break;

                        case ResourceType _ when resourceType == ResourceTypes.Route:
                            var originalRoute = _db.GetCollection<Route>(ResourceTypes.Route.Name).AsQueryable().First(x => x.Id == e.Id);
                            var routeArgs = originalRoute.CreateRouteArgs();
                            propertyInfo = typeof(RouteArgs).GetProperty(e.PropertyName);
                            propertyInfo.SetValue(routeArgs, e.Value);
                            var updatedRoute = new Route(routeArgs)
                            {
                                Id = e.Id,
                                UserId = e.UserId,
                                Timestamp = e.Timestamp
                            };
                            updatedRoute.Referencers.AddRange(originalRoute.Referencers);
                            _db.GetCollection<Route>(ResourceTypes.Route.Name).ReplaceOne(p => p.Id == e.Id, updatedRoute);
                            break;

                        case ResourceType _ when resourceType == ResourceTypes.Tag:
                            var originalTag = _db.GetCollection<Tag>(ResourceTypes.Tag.Name).AsQueryable().First(x => x.Id == e.Id);
                            var tagArgs = originalTag.CreateTagArgs();
                            propertyInfo = typeof(TagArgs).GetProperty(e.PropertyName);
                            propertyInfo.SetValue(tagArgs, e.Value);
                            var updatedTag = new Tag(tagArgs)
                            {
                                Id = e.Id,
                                UserId = e.UserId,
                                Timestamp = e.Timestamp
                            };
                            updatedTag.Referencers.AddRange(originalTag.Referencers);
                            _db.GetCollection<Tag>(ResourceTypes.Tag.Name).ReplaceOne(p => p.Id == e.Id, updatedTag);
                            break;
                    }
                    break;

                case DeletedEvent e:
                    resourceType = e.GetEntityType();
                    switch (resourceType)
                    {
                        case ResourceType _ when resourceType == ResourceTypes.Exhibit:
                            MarkDeleted<Exhibit>(resourceType, e.Id);
                            break;
                        case ResourceType _ when resourceType == ResourceTypes.ExhibitPage:
                            MarkDeleted<ExhibitPage>(resourceType, e.Id);
                            break;

                        case ResourceType _ when resourceType == ResourceTypes.Media:
                            MarkDeleted<MediaElement>(resourceType, e.Id);
                            break;
                        case ResourceType _ when resourceType == ResourceTypes.Route:
                            MarkDeleted<Route>(resourceType, e.Id);
                            break;
                        case ResourceType _ when resourceType == ResourceTypes.Tag:
                            MarkDeleted<Tag>(resourceType, e.Id);
                            break;
                    }
                    break;
            }

            if (ev is PropertyChangedEvent propEvent)
            {
                var references = propEvent.GetReferences();
                if (references.Any())
                    AddReferences((propEvent.GetEntityType(), propEvent.Id), references);
            }

        }

        private void ClearIncomingReferences(EntityId entity)
        {
            var currentReferencers = _db.GetCollection<dynamic>(entity.Type.Name)
                .Find(Builders<dynamic>.Filter.Eq("_id", entity.Id))
                .First()
                .Referencers;

            foreach (var r in currentReferencers)
            {
                // Note: We must use the internal key "_id" here since we use dynamic objects
                var source = (ResourceType.ResourceTypeDictionary[(string)r.Collection], (int)r._id);
                RemoveReference(source, entity);
            }
        }

        private void ClearOutgoingReferences(EntityId entity)
        {
            var currentReferences = _db.GetCollection<dynamic>(entity.Type.Name)
                .Find(Builders<dynamic>.Filter.Eq("_id", entity.Id))
                .First()
                .References;

            foreach (var r in currentReferences)
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
            var update = Builders<ContentBase>.Update.PushEach(nameof(ContentBase.References), targetRefs);
            var result = _db.GetCollection<ContentBase>(source.Type.Name).UpdateOne(x => x.Id == source.Id, update);
            Debug.Assert(result.ModifiedCount == 1);

            // 2) create a new DocRef pointing to the source and add it to the target's referencers list
            var sourceRef = new DocRef<ContentBase>(source.Id, source.Type.Name);
            var update2 = Builders<ContentBase>.Update.Push(nameof(ContentBase.Referencers), sourceRef);
            foreach (var target in targets)
            {
                result = _db.GetCollection<ContentBase>(target.Type.Name).UpdateOne(x => x.Id == target.Id, update2);
                Debug.Assert(result.ModifiedCount == 1);
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

            var result = _db.GetCollection<dynamic>(source.Type.Name).UpdateOne(
                Builders<dynamic>.Filter.Eq("_id", source.Id), update);

            Debug.Assert(result.ModifiedCount == 1);

            // 2) delete the DocRef pointing to the source from the target's referencers list
            var update2 = Builders<dynamic>.Update.PullFilter(
                nameof(ContentBase.Referencers),
                Builders<dynamic>.Filter.And(
                    Builders<dynamic>.Filter.Eq(nameof(DocRefBase.Collection), source.Type.Name),
                    Builders<dynamic>.Filter.Eq("_id", source.Id)));

            var result2 = _db.GetCollection<dynamic>(target.Type.Name).UpdateOne(
                Builders<dynamic>.Filter.Eq("_id", target.Id), update2);

            Debug.Assert(result2.ModifiedCount == 1);
        }

        private void MarkDeleted<T>(ResourceType resourceType, int entityId) where T : ContentBase
        {
            var collection = _db.GetCollection<T>(resourceType.Name);
            var entity = collection.AsQueryable().First(x => x.Id == entityId);
            entity.Status = ContentStatus.Deleted;
            collection.ReplaceOne(x => x.Id == entityId, entity);
        }
    }
}
