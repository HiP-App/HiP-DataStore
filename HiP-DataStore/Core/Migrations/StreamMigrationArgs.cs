using System.Collections.Generic;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using EventStore.ClientAPI;
using System;

namespace PaderbornUniversity.SILab.Hip.DataStore.Core.Migrations
{
    public class StreamMigrationArgs : IStreamMigrationArgs
    {
        private readonly IEventStoreConnection _connection;
        private readonly string _streamName;
        private readonly List<EventData> _eventsToAppend = new List<EventData>();

        public IReadOnlyList<EventData> EventsToAppend => _eventsToAppend;

        public StreamMigrationArgs(IEventStoreConnection connection, string streamName)
        {
            _connection = connection;
            _streamName = streamName;
        }

        public void AppendEvent(IEvent ev) => _eventsToAppend.Add(ev.ToEventData(Guid.NewGuid()));

        public IAsyncEnumerator<IEvent> GetExistingEvents() =>
            new EventStoreStreamEnumerator(_connection, _streamName);
    }
}
