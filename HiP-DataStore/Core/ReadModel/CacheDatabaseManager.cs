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
                var ev = resolvedEvent.Event.ToIEvent();

                switch (ev)
                {
                    case ExhibitCreated e:
                        var newExhibit = new Exhibit(e.Properties)
                        {
                            Id = e.Id,
                            Timestamp = e.Timestamp
                        };

                        _db.GetCollection<Exhibit>(ResourceType.Exhibit.Name).InsertOne(newExhibit);
                        break;

                    case ExhibitUpdated e:
                        var updatedExhibit = new Exhibit(e.Properties)
                        {
                            Id = e.Id,
                            Timestamp = e.Timestamp
                        };

                        _db.GetCollection<Exhibit>(ResourceType.Exhibit.Name).ReplaceOne(x => x.Id == e.Id, updatedExhibit);
                        break;

                    case ExhibitDeleted e:
                        _db.GetCollection<Exhibit>(ResourceType.Exhibit.Name).DeleteOne(x => x.Id == e.Id);
                        break;

                    case ExhibitPageCreated e:
                        var newPage = new ExhibitPage(e.Properties)
                        {
                            Id = e.Id,
                            Timestamp = e.Timestamp
                        };

                        _db.GetCollection<ExhibitPage>(ResourceType.ExhibitPage.Name).InsertOne(newPage);
                        break;

                    case ExhibitPageUpdated e:
                        var updatedPage = new ExhibitPage(e.Properties)
                        {
                            Id = e.Id,
                            Timestamp = e.Timestamp
                        };

                        _db.GetCollection<ExhibitPage>(ResourceType.ExhibitPage.Name).ReplaceOne(x => x.Id == e.Id, updatedPage);
                        break;

                    case ExhibitPageDeleted e:
                        _db.GetCollection<ExhibitPage>(ResourceType.ExhibitPage.Name).DeleteOne(x => x.Id == e.Id);
                        break;

                    case RouteCreated e:
                        var newRoute = new Route(e.Properties)
                        {
                            Id = e.Id,
                            Timestamp = e.Timestamp
                        };

                        _db.GetCollection<Route>(ResourceType.Route.Name).InsertOne(newRoute);
                        break;

                    case RouteUpdated e:
                        var updatedRoute = new Route(e.Properties)
                        {
                            Id = e.Id,
                            Timestamp = e.Timestamp
                        };

                        _db.GetCollection<Route>(ResourceType.Route.Name).ReplaceOne(r => r.Id == e.Id, updatedRoute);
                        break;

                    case RouteDeleted e:
                        _db.GetCollection<Route>(ResourceType.Route.Name).DeleteOne(r => r.Id == e.Id);
                        break;

                    case MediaCreated e:
                        var newMedia = new MediaElement(e.Properties)
                        {
                            Id = e.Id,
                            Timestamp = e.Timestamp
                        };

                        _db.GetCollection<MediaElement>(ResourceType.Media.Name).InsertOne(newMedia);
                        break;

                    case MediaUpdate e:
                        var updatedMedia = new MediaElement(e.Properties)
                        {
                            Id = e.Id,
                            Timestamp = e.Timestamp
                        };

                        _db.GetCollection<MediaElement>(ResourceType.Media.Name).ReplaceOne(m => m.Id == e.Id, updatedMedia);
                        break;

                    case MediaDeleted e:
                        _db.GetCollection<MediaElement>(ResourceType.Media.Name).DeleteOne(m => m.Id == e.Id);
                        break;
                        
                    case MediaFileUpdated e:
                        var fileDocBson = e.ToBsonDocument();
                        fileDocBson.Remove("Id");
                        var bsonDoc = new BsonDocument("$set", fileDocBson);
                        _db.GetCollection<MediaElement>(ResourceType.Media.Name).UpdateOne(x => x.Id == e.Id, bsonDoc);
                        break;

                    case TagCreated e:
                        var newTag = new Tag(e.Properties)
                        {
                            Id = e.Id,
                            Timestamp = e.Timestamp,
                        };

                        _db.GetCollection<Tag>(ResourceType.Tag.Name).InsertOne(newTag);
                        break;

                    case TagUpdated e:
                        var updatedTag = new Tag(e.Properties)
                        {
                            Id = e.Id,
                            Timestamp = e.Timestamp,
                        };

                        _db.GetCollection<Tag>(ResourceType.Tag.Name).ReplaceOne(x => x.Id == e.Id, updatedTag);
                        break;

                    case TagDeleted e:
                        _db.GetCollection<Tag>(ResourceType.Tag.Name).DeleteOne(x => x.Id == e.Id);
                        break;

                    case ReferenceAdded e:
                        // a reference (source -> target) was added, so we have to create a new DocRef pointing to the
                        // source and add it to the target's referencees list
                        var newReference = new DocRef<ContentBase>(e.SourceId, e.SourceType.Name);
                        var update = Builders<ContentBase>.Update.Push(nameof(ContentBase.Referencees), newReference);
                        _db.GetCollection<ContentBase>(e.TargetType.Name).UpdateOne(x => x.Id == e.TargetId, update);
                        break;

                    case ReferenceRemoved e:
                        // a reference (source -> target) was removed, so we have to delete the DocRef pointing to the
                        // source from the target's referencees list

                        // ladies and gentlemen, fasten your seatbelts and prepare for the
                        // ugly truth of the MongoDB API:
                        var update2 = Builders<dynamic>.Update.PullFilter(
                            nameof(ContentBase.Referencees),
                            Builders<dynamic>.Filter.And(
                                Builders<dynamic>.Filter.Eq(nameof(DocRefBase.Collection), e.SourceType.Name),
                                Builders<dynamic>.Filter.Eq("_id", e.SourceId)));

                        _db.GetCollection<dynamic>(e.TargetType.Name).UpdateOne(
                            Builders<dynamic>.Filter.Eq("_id", e.TargetId), update2);
                        break;
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning($"{nameof(CacheDatabaseManager)} could not process an event: {e}");
            }
        }
    }
}
