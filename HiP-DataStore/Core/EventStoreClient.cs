using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel;
using PaderbornUniversity.SILab.Hip.DataStore.Utility;
using PaderbornUniversity.SILab.Hip.EventSourcing;
using PaderbornUniversity.SILab.Hip.EventSourcing.EventStoreLlp;
using PaderbornUniversity.SILab.Hip.EventSourcing.Migrations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public IEventStore Connection { get; }

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

            // Prevent accidentally working with a production database
            if (Debugger.IsAttached)
            {
                Debug.Assert(config.Value.EventStoreHost.Contains("localhost"),
                    "It looks like you are trying to connect to a production Event Store database. Are you sure you wish to continue?");
            }

            // Establish connection to Event Store
            var uri = new Uri(config.Value.EventStoreHost);
            Connection = EventStoreConnection.Create(settings, uri);
            Connection.ConnectAsync().Wait();

            logger.LogInformation($"Connected to Event Store on '{uri.Host}', using stream '{_streamName}'");

            // Update stream to the latest version
            var migrationResult = StreamMigrator.MigrateAsync(Connection, _streamName).Result;
            if (migrationResult.fromVersion != migrationResult.toVersion)
                logger.LogInformation($"Migrated stream '{_streamName}' from version '{migrationResult.fromVersion}' to version '{migrationResult.toVersion}'");

            // Setup IDomainIndex-indices
            _indices = indices.ToList();
            PopulateIndicesAsync().Wait();
        }

        /// <summary>
        /// Starts a new transaction. Append events to the transaction and eventually commit
        /// the transaction to persist the events to the event stream.
        /// </summary>
        public EventStoreClientTransaction BeginTransaction()
        {
            return new EventStoreClientTransaction(this);
        }

        /// <summary>
        /// Appends a single event to the event stream. If you need to append multiple events in one batch,
        /// either use <see cref="AppendEventsAsync(IEnumerable{IEvent})"/> or <see cref="BeginTransaction"/>
        /// and <see cref="EventStoreClientTransaction.CommitAsync"/> instead.
        /// </summary>
        public async Task AppendEventAsync(IEvent ev)
        {
            await AppendEventsAsync(new[] { ev });
        }

        public Task<WriteResult> AppendEventsAsync(IEnumerable<IEvent> events) =>
            AppendEventsAsync(events?.ToList());

        public async Task<WriteResult> AppendEventsAsync(IReadOnlyCollection<IEvent> events)
        {
            if (events == null)
                throw new ArgumentNullException(nameof(events));

            // persist events in Event Store
            var eventData = events.Select(ev => ev.ToEventData(Guid.NewGuid()));
            var result = await Connection.AppendToStreamAsync(_streamName, ExpectedVersion.Any, eventData);

            // forward events to indices so they can update their state
            foreach (var ev in events)
                foreach (var index in _indices)
                    index.ApplyEvent(ev);

            return result;
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
                {
                    try
                    {
                        index.ApplyEvent(events.Current);
                    }
                    catch (Exception e)
                    {
                        _logger.LogWarning($"Failed to populate index of type '{index.GetType().Name}' with event of type '{events.Current.GetType().Name}': {e}");
                    }
                }
            }

            _logger.LogInformation($"Populated indices with {totalCount} events");
        }

    }
}
