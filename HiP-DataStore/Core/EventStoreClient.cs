using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PaderbornUniversity.SILab.Hip.DataStore.Core.Migrations;
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
        private readonly IReadOnlyCollection<IDomainIndex> _indices;
        private readonly ILogger<EventStoreClient> _logger;
        private readonly string _streamName;

        public IEventStoreConnection Connection { get; }

        public EventStoreClient(
            IEnumerable<IDomainIndex> indices,
            IOptions<EndpointConfig> config,
            ILogger<EventStoreClient> logger)
        {
            _logger = logger;
            _streamName = config.Value.EventStoreStream;

            var settings = ConnectionSettings.Create()
                .EnableVerboseLogging()
                .Build();

            // Establish connection to Event Store
            Connection = EventStoreConnection.Create(settings, new Uri(config.Value.EventStoreHost));
            Connection.ConnectAsync().Wait();

            logger.LogInformation($"Connected to Event Store, using stream '{_streamName}'");

            // Update stream to the latest version
            var migrationResult = StreamMigrator.MigrateAsync(Connection, _streamName).Result;
            if (migrationResult.fromVersion != migrationResult.toVersion)
                logger.LogInformation($"Migrated stream '{_streamName}' from version '{migrationResult.fromVersion}' to version '{migrationResult.toVersion}'");

            // Setup IDomainIndex-indices
            _indices = indices.ToList();
            PopulateIndicesAsync().Wait();
        }

        public async Task AppendEventAsync(IEvent ev)
        {
            if (ev == null)
                throw new ArgumentNullException(nameof(ev));

            if (ev is IMigratable<IEvent>)
                throw new ArgumentException(
                    $"The event to be appended is an instance of the obsolete event type '{ev.GetType().Name}'. " +
                    "Only events of up-to-date event types should be emitted.");

            // forward event to indices so they can update their state
            foreach (var index in _indices)
                index.ApplyEvent(ev);

            // persist event in Event Store
            var eventId = Guid.NewGuid();
            await Connection.AppendToStreamAsync(_streamName, ExpectedVersion.Any, ev.ToEventData(eventId));
        }

        public async Task AppendEventsAsync(IEnumerable<IEvent> events)
        {
            foreach (var ev in events)
                await AppendEventAsync(ev);
        }

        private async Task PopulateIndicesAsync()
        {
            var events = new EventStoreStreamEnumerator(Connection, _streamName);
            var totalCount = 0;

            events.EventParsingFailed += (_, exception) =>
                _logger.LogWarning($"{nameof(EventStoreClient)} could not process an event: {exception}");

            while (await events.MoveNextAsync())
            {
                totalCount++;

                foreach (var index in _indices)
                    index.ApplyEvent(events.Current);
            }

            _logger.LogInformation($"Populated indices with {totalCount} events");
        }
    }
}
