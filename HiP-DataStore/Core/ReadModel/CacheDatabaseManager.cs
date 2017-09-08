using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using PaderbornUniversity.SILab.Hip.DataStore.Model;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using PaderbornUniversity.SILab.Hip.DataStore.Utility;
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
        private readonly EventStoreClient _eventStore;
        private readonly IMongoDatabase _db;
        private readonly ILogger<CacheDatabaseManager> _logger;

        public IMongoDatabase Database => _db;

        public CacheDatabaseManager(
            EventStoreClient eventStore,
            IOptions<EndpointConfig> config,
            ILogger<CacheDatabaseManager> logger)
        {
            // For now, the cache database is always created from scratch by replaying all events.
            // This also implies that, for now, the cache database always contains the entire data (not a subset).
            // In order to receive all the events, a Catch-Up Subscription is created.

            _logger = logger;

            // 1) Open MongoDB connection and clear existing database
            var mongo = new MongoClient(config.Value.MongoDbHost);
            mongo.DropDatabase(config.Value.MongoDbName);
            _db = mongo.GetDatabase(config.Value.MongoDbName);
            var uri = new Uri(config.Value.MongoDbHost);
            logger.LogInformation($"Connected to MongoDB cache database on '{uri.Host}', using database '{config.Value.MongoDbName}'");

            // 2) Subscribe to EventStore to receive all past and future events
            _eventStore = eventStore;

            _eventStore.Connection.SubscribeToStreamFrom(
                config.Value.EventStoreStream,
                null, // don't use StreamPosition.Start (see https://groups.google.com/forum/#!topic/event-store/8tpXJMNEMqI),
                CatchUpSubscriptionSettings.Default,
                OnEventAppeared);
        }

        private void OnEventAppeared(EventStoreCatchUpSubscription subscription, ResolvedEvent resolvedEvent)
        {
            try
            {
                // Note regarding migration:
                // Event types may change over time (properties get added/removed etc.)
                // Whenever an event has multiple versions, an event of an obsolete type should be transformed to an event
                // of the latest version, so that ApplyEvent(...) only has to deal with events of the current version.
                var ev = resolvedEvent.Event.ToIEvent().MigrateToLatestVersion();
                ApplyEvent(ev);
            }
            catch (Exception e)
            {
                _logger.LogWarning($"{nameof(CacheDatabaseManager)} could not process an event: {e}");
            }
        }

        private void ApplyEvent(IEvent ev)
        {
            if (ev is ICrudEvent crudEvent)
            {
                var entity = (crudEvent.GetEntityType(), crudEvent.Id);
                if (crudEvent is IDeleteEvent)
                {
                    ClearIncomingReferences(entity);
                    ClearOutgoingReferences(entity);
                }
                else if (crudEvent is IUpdateEvent)
                {
                    ClearOutgoingReferences(entity);
                }
            }

            switch (ev)
            {
                case ExhibitCreated e:
                    var newExhibit = new Exhibit(e.Properties)
                    {
                        Id = e.Id,
                        UserId = e.UserId,
                        Timestamp = e.Timestamp
                    };

                    _db.GetCollection<Exhibit>(ResourceType.Exhibit.Name).InsertOne(newExhibit);
                    break;

                case ExhibitUpdated e:
                    var originalExhibit = _db.GetCollection<Exhibit>(ResourceType.Exhibit.Name).AsQueryable().First(x => x.Id == e.Id);

                    var updatedExhibit = new Exhibit(e.Properties)
                    {
                        Id = e.Id,
                        UserId = e.UserId,
                        Timestamp = e.Timestamp
                    };

                    updatedExhibit.Referencers.AddRange(originalExhibit.Referencers);
                    _db.GetCollection<Exhibit>(ResourceType.Exhibit.Name).ReplaceOne(x => x.Id == e.Id, updatedExhibit);
                    break;

                case ExhibitDeleted e:
                    _db.GetCollection<Exhibit>(ResourceType.Exhibit.Name).DeleteOne(x => x.Id == e.Id);
                    break;

                case ExhibitPageCreated3 e:
                    var newPage = new ExhibitPage(e.Properties)
                    {
                        Id = e.Id,
                        UserId = e.UserId,
                        Timestamp = e.Timestamp
                    };

                    _db.GetCollection<ExhibitPage>(ResourceType.ExhibitPage.Name).InsertOne(newPage);
                    break;

                case ExhibitPageUpdated3 e:
                    var originalPage = _db.GetCollection<ExhibitPage>(ResourceType.ExhibitPage.Name).AsQueryable().First(x => x.Id == e.Id);
                    var updatedPage = new ExhibitPage(e.Properties)
                    {
                        Id = e.Id,
                        UserId = e.UserId,
                        Timestamp = e.Timestamp
                    };

                    updatedPage.Referencers.AddRange(originalPage.Referencers);
                    _db.GetCollection<ExhibitPage>(ResourceType.ExhibitPage.Name).ReplaceOne(x => x.Id == e.Id, updatedPage);
                    break;

                case ExhibitPageDeleted2 e:
                    _db.GetCollection<ExhibitPage>(ResourceType.ExhibitPage.Name).DeleteOne(x => x.Id == e.Id);
                    break;

                case RouteCreated e:
                    var newRoute = new Route(e.Properties)
                    {
                        Id = e.Id,
                        UserId = e.UserId,
                        Timestamp = e.Timestamp
                    };

                    _db.GetCollection<Route>(ResourceType.Route.Name).InsertOne(newRoute);
                    break;

                case RouteUpdated e:
                    var originalRoute = _db.GetCollection<Route>(ResourceType.Route.Name).AsQueryable().First(x => x.Id == e.Id);
                    var updatedRoute = new Route(e.Properties)
                    {
                        Id = e.Id,
                        UserId = e.UserId,
                        Timestamp = e.Timestamp
                    };

                    updatedRoute.Referencers.AddRange(originalRoute.Referencers);
                    _db.GetCollection<Route>(ResourceType.Route.Name).ReplaceOne(r => r.Id == e.Id, updatedRoute);
                    break;

                case RouteDeleted e:
                    _db.GetCollection<Route>(ResourceType.Route.Name).DeleteOne(r => r.Id == e.Id);
                    break;

                case MediaCreated e:
                    var newMedia = new MediaElement(e.Properties)
                    {
                        Id = e.Id,
                        UserId = e.UserId,
                        Timestamp = e.Timestamp
                    };

                    _db.GetCollection<MediaElement>(ResourceType.Media.Name).InsertOne(newMedia);
                    break;

                case MediaUpdate e:
                    var originalMedia = _db.GetCollection<MediaElement>(ResourceType.Media.Name).AsQueryable().First(x => x.Id == e.Id);
                    var updatedMedia = new MediaElement(e.Properties)
                    {
                        Id = e.Id,
                        UserId = e.UserId,
                        Timestamp = e.Timestamp
                    };

                    updatedMedia.Referencers.AddRange(originalMedia.Referencers);
                    updatedMedia.File = originalMedia.File;
                    _db.GetCollection<MediaElement>(ResourceType.Media.Name).ReplaceOne(m => m.Id == e.Id, updatedMedia);
                    break;

                case MediaDeleted e:
                    _db.GetCollection<MediaElement>(ResourceType.Media.Name).DeleteOne(m => m.Id == e.Id);
                    break;

                case MediaFileUpdated e:
                    var fileDocBson = e.ToBsonDocument();
                    fileDocBson.Remove("_id");
                    var bsonDoc = new BsonDocument("$set", fileDocBson);
                    _db.GetCollection<MediaElement>(ResourceType.Media.Name).UpdateOne(x => x.Id == e.Id, bsonDoc);
                    break;

                case TagCreated e:
                    var newTag = new Tag(e.Properties)
                    {
                        Id = e.Id,
                        UserId = e.UserId,
                        Timestamp = e.Timestamp,
                    };

                    _db.GetCollection<Tag>(ResourceType.Tag.Name).InsertOne(newTag);
                    break;

                case TagUpdated e:
                    var originalTag = _db.GetCollection<Tag>(ResourceType.Tag.Name).AsQueryable().First(x => x.Id == e.Id);
                    var updatedTag = new Tag(e.Properties)
                    {
                        Id = e.Id,
                        UserId = e.UserId,
                        Timestamp = e.Timestamp,
                    };

                    updatedTag.Referencers.AddRange(originalTag.Referencers);
                    _db.GetCollection<Tag>(ResourceType.Tag.Name).ReplaceOne(x => x.Id == e.Id, updatedTag);
                    break;

                case TagDeleted e:
                    _db.GetCollection<Tag>(ResourceType.Tag.Name).DeleteOne(x => x.Id == e.Id);
                    break;

                case ScoreAdded e:
                    var newScoreRecord = new ScoreRecord
                    {
                        Id = e.Id,
                        UserId = e.UserId,
                        Score = e.Score,
                        Timestamp = e.Timestamp
                    };
                    _db.GetCollection<ScoreRecord>(ResourceType.ScoreRecord.Name).InsertOne(newScoreRecord);
                    break;
            }

            if (ev is ICreateEvent createEvent)
                AddReferences((createEvent.GetEntityType(), createEvent.Id), createEvent.GetReferences());
            else if (ev is IUpdateEvent updateEvent)
                AddReferences((updateEvent.GetEntityType(), updateEvent.Id), updateEvent.GetReferences());
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
                var source = (new ResourceType(r.Collection), (int)r._id);
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
                var target = (new ResourceType(r.Collection), (int)r._id);
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
    }
}
