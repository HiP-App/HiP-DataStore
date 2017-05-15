using EventStore.ClientAPI;
using MongoDB.Bson;
using MongoDB.Driver;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
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

        public CacheDatabaseManager(EventStoreClient eventStore)
        {
            // For now, the cache database is always created from scratch by replaying all events.
            // This also implies that, for now, the cache database always contains the entire data (not a subset).
            // In order to receive all the events, a Catch-Up Subscription is created.

            // 1) Open MongoDB connection and clear existing database
            // TODO: Make the connection string (MongoUrl) and database name configurable
            var mongo = new MongoClient("mongodb://localhost:27017");
            mongo.DropDatabase("main");
            _db = mongo.GetDatabase("main");

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
                        Used = e.Properties.Used,
                        Status = e.Properties.Status,
                        Tags = { e.Properties.Tags?.Select(id => (BsonValue)id) },
                        Timestamp = DateTimeOffset.Now
                    };

                    _db.GetCollection<Exhibit>(Exhibit.CollectionName).InsertOne(newExhibit);
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

                    _db.GetCollection<Route>(Route.CollectionName).InsertOne(newRoute);
                    break;

                case MediaCreated e:
                    var newMedia = new MediaElement
                    {
                        Id = e.Id,
                        Title = e.Properties.Title,
                        Description = e.Properties.Description,
                        Type = e.Properties.Type,
                        Status = e.Properties.Status,
                        IsUsed = false,
                        File = "",
                        Timestamp = DateTimeOffset.Now,

                    };
                    _db.GetCollection<MediaElement>(MediaElement.CollectionName).InsertOne(newMedia);
                    break;

                case MediaDeleted e:
                    _db.GetCollection<MediaElement>(MediaElement.CollectionName).DeleteOne(x => x.Id == e.Id);
                    break;
                case MediaUpdate e:
               
                      var filter = Builders<MediaElement>.Filter.Eq(x => x.Id,e.Id);
                      var timestamp = new { Timestamp= e.Timestamp }.ToBsonDocument();
                      var bsonDoc = new BsonDocument("$set", e.Properties.ToBsonDocument().AddRange(timestamp));
 
                      _db.GetCollection<MediaElement>(MediaElement.CollectionName).UpdateOne(filter,bsonDoc);
                    break;
                case MediaFileUpdated e:
                       var fileDocBson = e.ToBsonDocument();

                      bsonDoc = new BsonDocument("$set",fileDocBson);
                      _db.GetCollection<MediaElement>(MediaElement.CollectionName).UpdateOne(x => x.Id == e.Id, bsonDoc);
                        break;

                    // TODO: Handle further events
            }
        }
    }
}
