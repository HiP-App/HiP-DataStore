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

        public CacheDatabaseManager(EventStoreClient eventStore)
        {
            // Subscribe to EventStore events
            _eventStore = eventStore;
            _eventStore.Connection
                .SubscribeToStreamAsync(EventStoreClient.DefaultStreamName, false, OnEventAppeared)
                .Wait();

            // Open MongoDB database
            var mongo = new MongoClient("mongodb://localhost:27017");
            _db = mongo.GetDatabase("main");
        }

        private void OnEventAppeared(EventStoreSubscription subscription, ResolvedEvent resolvedEvent)
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
