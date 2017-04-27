using EventStore.ClientAPI;
using MongoDB.Bson;
using MongoDB.Driver;
using PaderbornUniversity.SILab.Hip.DataStore.Model;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;

namespace PaderbornUniversity.SILab.Hip.DataStore.Core
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
                        Id = ObjectId.GenerateNewId(),
                        Name = e.Name,
                        Description = e.Description,
                        Latitude = e.Latitude,
                        Longitude = e.Longitude,
                        Image = new DocRef<MediaElement>(e.ImageId, MediaElement.CollectionName)
                    };

                    _db.GetCollection<Exhibit>(Exhibit.CollectionName).InsertOne(newExhibit);
                    break;

                    // TODO: Handle further events
            }
        }
    }
}
