using EventStore.ClientAPI;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using System;
using System.Net;
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
        public static readonly IPEndPoint LocalhostEndpoint = new IPEndPoint(IPAddress.Loopback, 1113);

        public IEventStoreConnection Connection { get; }

        public EventStoreClient()
        {
            // TODO: Inject app settings (so that the endpoint can be configured through appsettings.development.json)
            var settings = ConnectionSettings.Create()
                .EnableVerboseLogging()
                .Build();

            Connection = EventStoreConnection.Create(settings, LocalhostEndpoint);
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
