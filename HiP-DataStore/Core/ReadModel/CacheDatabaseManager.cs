using EventStore.ClientAPI;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using PaderbornUniversity.SILab.Hip.DataStore.Model;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using PaderbornUniversity.SILab.Hip.DataStore.Utility;
using System;
using System.Linq;

namespace PaderbornUniversity.SILab.Hip.DataStore.Core.ReadModel
{
    /// <summary>
    /// Subscribes to EventStore events to keep the cache database up to date.
    /// </summary>
    public class CacheDatabaseManager
    {
        private readonly EventStoreClient _eventStore;
        private readonly IMongoDatabase _db;

        public IMongoDatabase Database => _db;

        public CacheDatabaseManager(EventStoreClient eventStore, IOptions<EndpointConfig> config)
        {
            // For now, the cache database is always created from scratch by replaying all events.
            // This also implies that, for now, the cache database always contains the entire data (not a subset).
            // In order to receive all the events, a Catch-Up Subscription is created.

            // 1) Open MongoDB connection and clear existing database
            var mongo = new MongoClient(config.Value.MongoDbHost);
            mongo.DropDatabase(config.Value.MongoDbName);
            _db = mongo.GetDatabase(config.Value.MongoDbName);

            // 2) Subscribe to EventStore to receive all past and future events
            _eventStore = eventStore;

            _eventStore.Connection.SubscribeToStreamFrom(
                EventStoreClient.DefaultStreamName,
                StreamPosition.Start,
                CatchUpSubscriptionSettings.Default,
                OnEventAppeared);
        }

        private void OnEventAppeared(EventStoreCatchUpSubscription subscription, ResolvedEvent resolvedEvent)
        {
            var ev = resolvedEvent.Event.ToIEvent();

            switch (ev)
            {
                case ExhibitCreated e:
                    var newExhibit = new Exhibit
                    {
                        Id = e.Id,
                        Name = e.Properties.Name,
                        Description = e.Properties.Description,
                        Image = { Id = e.Properties.Image },
                        Latitude = e.Properties.Latitude,
                        Longitude = e.Properties.Longitude,
                        Status = e.Properties.Status,
                        Tags = { e.Properties.Tags?.Select(id => (BsonValue)id) },
                        Timestamp = DateTimeOffset.Now
                    };

                    _db.GetCollection<Exhibit>(ResourceType.Exhibit.Name).InsertOne(newExhibit);
                    break;

                case ExhibitDeleted e:
                    _db.GetCollection<Exhibit>(ResourceType.Exhibit.Name).DeleteOne(x => x.Id == e.Id);
                    break;

                case RouteCreated e:
                    var newRoute = new Route
                    {
                        Id = e.Id,
                        Title = e.Properties.Title,
                        Description = e.Properties.Description,
                        Duration = e.Properties.Duration,
                        Distance = e.Properties.Distance,
                        Image = { Id = e.Properties.Image },
                        Audio = { Id = e.Properties.Audio },
                        Exhibits = { e.Properties.Exhibits?.Select(id => (BsonValue)id) },
                        Status = e.Properties.Status,
                        Tags = { e.Properties.Tags?.Select(id => (BsonValue)id) },
                        Timestamp = DateTimeOffset.Now
                    };

                    _db.GetCollection<Route>(ResourceType.Route.Name).InsertOne(newRoute);
                    break;

                case RouteDeleted e:
                    _db.GetCollection<Route>(ResourceType.Route.Name).DeleteOne(r => r.Id == e.Id);
                    break;


                case ReferenceAdded e:
                    var referencedEntity = _db.GetCollection<ContentBase>(e.SourceType.Name).AsQueryable()
                        .FirstOrDefault(o => o.Id == e.TargetId);

                    referencedEntity.Referencees.Add(new Model.DocRef<ContentBase>(e.SourceId, e.SourceType.Name));
                    break;

                case ReferenceRemoved e:
                    var referencedEntity2 = _db.GetCollection<ContentBase>(e.TargetType.Name).AsQueryable()
                        .FirstOrDefault(o => o.Id == e.TargetId);

                    var referenceToRemove = referencedEntity2.Referencees
                        .FirstOrDefault(r => r.Collection == e.SourceType.Name && r.Id == e.SourceId);

                    referencedEntity2.Referencees.Remove(referenceToRemove);
                    break;

                    // TODO: Handle further events
            }
        }
    }
}
