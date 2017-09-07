using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PaderbornUniversity.SILab.Hip.DataStore.Utility;
using PaderbornUniversity.SILab.Hip.EventSourcing;
using PaderbornUniversity.SILab.Hip.EventSourcing.Migrations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ESL = PaderbornUniversity.SILab.Hip.EventSourcing.EventStoreLlp;

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
        private readonly ILogger<EventStoreClient> _logger;
        private readonly string _streamName;
        private readonly IEventStore _store;
        private readonly InMemoryCache _cache;

        public IEventStream EventStream => _store.Streams[_streamName];

        public EventStoreClient(
            InMemoryCache cache,
            IOptions<EndpointConfig> config,
            ILogger<EventStoreClient> logger)
        {
            _logger = logger;
            _streamName = config.Value.EventStoreStream;

            var settings = ConnectionSettings.Create()
                .EnableVerboseLogging()
                .Build();

            // Prevent accidentally working with a production database
            if (Debugger.IsAttached)
            {
                Debug.Assert(config.Value.EventStoreHost.Contains("localhost"),
                    "It looks like you are trying to connect to a production Event Store database. Are you sure you wish to continue?");
            }

            // Establish connection to Event Store
            var uri = new Uri(config.Value.EventStoreHost);
            var connection = EventStoreConnection.Create(settings, uri);
            connection.ConnectAsync().Wait();
            
            _store = new ESL.EventStore(connection);

            logger.LogInformation($"Connected to Event Store on '{uri.Host}', using stream '{_streamName}'");

            // Update stream to the latest version
            var migrationResult = StreamMigrator.MigrateAsync(_store, _streamName).Result;
            if (migrationResult.fromVersion != migrationResult.toVersion)
                logger.LogInformation($"Migrated stream '{_streamName}' from version '{migrationResult.fromVersion}' to version '{migrationResult.toVersion}'");

            // Setup IDomainIndex-indices
            _cache = cache;
            PopulateIndicesAsync().Wait();
        }

        /// <summary>
        /// Starts a new transaction. Append events to the transaction and eventually commit
        /// the transaction to persist the events to the event stream.
        /// </summary>
        public EventStreamTransaction BeginTransaction()
        {
            return _store.Streams[_streamName].BeginTransaction();
        }

        /// <summary>
        /// Appends a single event to the event stream. If you need to append multiple events in one batch,
        /// either use <see cref="AppendEventsAsync(IEnumerable{IEvent})"/> or <see cref="BeginTransaction"/>
        /// and <see cref="EventStreamTransaction.Commit"/> instead.
        /// </summary>
        public async Task AppendEventAsync(IEvent ev)
        {
            await AppendEventsAsync(new[] { ev });
        }

        public Task AppendEventsAsync(IEnumerable<IEvent> events) =>
            AppendEventsAsync(events?.ToList());

        public async Task AppendEventsAsync(IReadOnlyCollection<IEvent> events)
        {
            if (events == null)
                throw new ArgumentNullException(nameof(events));

            // persist events in Event Store
            await _store.Streams[_streamName].AppendManyAsync(events);

            // forward events to indices so they can update their state
            foreach (var ev in events)
                _cache.ApplyEvent(ev);
        }

        private async Task PopulateIndicesAsync()
        {
            var events = _store.Streams[_streamName].GetEnumerator();
            var totalCount = 0;

            events.EventParsingFailed += (_, exception) =>
                _logger.LogWarning($"{nameof(EventStoreClient)} could not process an event: {exception}");

            while (await events.MoveNextAsync())
            {
                totalCount++;
                _cache.ApplyEvent(events.Current);
            }

            _logger.LogInformation($"Populated indices with {totalCount} events");
        }

    }
}
