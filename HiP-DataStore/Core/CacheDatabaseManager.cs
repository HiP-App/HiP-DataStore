using EventStore.ClientAPI;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;

namespace PaderbornUniversity.SILab.Hip.DataStore.Core
{
    /// <summary>
    /// Subscribes to EventStore events to keep the cache database up to date.
    /// </summary>
    public class CacheDatabaseManager
    {
        private readonly EventStoreClient _eventStore;

        public CacheDatabaseManager(EventStoreClient eventStore)
        {
            _eventStore = eventStore;

            _eventStore.Connection
                .SubscribeToStreamAsync(EventStoreClient.DefaultStreamName, false, OnEventAppeared)
                .Wait();
        }

        private void OnEventAppeared(EventStoreSubscription subscription, ResolvedEvent resolvedEvent)
        {
            var ev = resolvedEvent.Event.ToIEvent();

            switch (ev)
            {
                case ExhibitCreated e:
                    // TODO: Apply event to MongoDB cache database
                    break;

                // TODO: Handle further events
            }
        }
    }
}
