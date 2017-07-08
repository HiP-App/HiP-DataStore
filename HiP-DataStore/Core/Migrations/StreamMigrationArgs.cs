using System.Collections.Generic;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using EventStore.ClientAPI;

namespace PaderbornUniversity.SILab.Hip.DataStore.Core.Migrations
{
    public class StreamMigrationArgs : IStreamMigrationArgs
    {
        private readonly IEventStoreConnection _connection;
        private readonly string _streamName;
        private readonly List<IEvent> _eventsToAppend = new List<IEvent>();

        public IReadOnlyList<IEvent> EventsToAppend => _eventsToAppend;

        public StreamMigrationArgs(IEventStoreConnection connection, string streamName)
        {
            _connection = connection;
            _streamName = streamName;
        }

        public void AppendEvent(IEvent ev) => _eventsToAppend.Add(ev);

        public IAsyncEnumerator<IEvent> GetExistingEvents() =>
            new EventStoreStreamEnumerator(_connection, _streamName);
    }
}
