using EventStore.ClientAPI;
using Microsoft.Extensions.Options;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using PaderbornUniversity.SILab.Hip.DataStore.Utility;
using System;
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

        public IEventStoreConnection Connection { get; }

        public EventStoreClient(IOptions<EndpointConfig> config)
        {
            var settings = ConnectionSettings.Create()
                .EnableVerboseLogging()
                .Build();

            Connection = EventStoreConnection.Create(settings, new Uri(config.Value.EventStoreHost));
            Connection.ConnectAsync().Wait();
        }

        public async Task AppendEventAsync(IEvent ev, Guid eventId)
        {
            if (ev == null)
                throw new ArgumentNullException(nameof(ev));

            var result = await Connection.AppendToStreamAsync(DefaultStreamName, ExpectedVersion.Any, ev.ToEventData(eventId));
        }
    }
}
