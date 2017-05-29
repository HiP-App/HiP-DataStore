using EventStore.ClientAPI;
using Microsoft.Extensions.Options;
using PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using PaderbornUniversity.SILab.Hip.DataStore.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PaderbornUniversity.SILab.Hip.DataStore.Core
{
    /// <summary>
    /// Service that provides a connection to the EventStore. To be used with dependency injection.
    /// </summary>
    /// <remarks>
    /// "EventStoreConnection is thread-safe and it is recommended that only one instance per application is created."
    /// (http://docs.geteventstore.com/dotnet-api/4.0.0/connecting-to-a-server/)
    /// </remarks>
    public class EventStoreClient
    {
        public const string DefaultStreamName = "main-stream";

        private readonly IReadOnlyCollection<IDomainIndex> _indices;

        public IEventStoreConnection Connection { get; }

        public EventStoreClient(IEnumerable<IDomainIndex> indices, IOptions<EndpointConfig> config)
        {
            var settings = ConnectionSettings.Create()
                .EnableVerboseLogging()
                .Build();

            Connection = EventStoreConnection.Create(settings, new Uri(config.Value.EventStoreHost));
            Connection.ConnectAsync().Wait();

            _indices = indices.ToList();
            PopulateIndices();
        }

        public async Task AppendEventAsync(IEvent ev)
        {
            if (ev == null)
                throw new ArgumentNullException(nameof(ev));

            // forward event to indices so they can update their state
            foreach (var index in _indices)
                index.ApplyEvent(ev);

            // persist event in Event Store
            var eventId = Guid.NewGuid();
            await Connection.AppendToStreamAsync(DefaultStreamName, ExpectedVersion.Any, ev.ToEventData(eventId));
        }

        private void PopulateIndices()
        {
            const int pageSize = 4096; // only 4096 events can be retrieved in one call

            // read all events (from the beginning to the end) and apply them to the indices
            var start = 0;
            StreamEventsSlice readResult;

            do
            {
                readResult = Connection.ReadStreamEventsForwardAsync(DefaultStreamName, start, pageSize, false).Result;
                var events = readResult.Events.Select(e => e.Event.ToIEvent());

                foreach (var e in events)
                    foreach (var index in _indices)
                        index.ApplyEvent(e);

                start += pageSize;
            }
            while (!readResult.IsEndOfStream);
        }
    }
}
