using EventStore.ClientAPI;
using Newtonsoft.Json;
using PaderbornUniversity.SILab.Hip.EventSourcing;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace PaderbornUniversity.SILab.Hip.DataStore.Core
{
    public static class EventDataExtensions
    {
        private const string EventClrTypeHeader = "ClrType";
        private static readonly Dictionary<string, object> NoHeaders = new Dictionary<string, object>();

        /// <summary>
        /// Converts an <see cref="IEvent"/> object to EventStore's <see cref="EventData"/>.
        /// </summary>
        public static EventData ToEventData(this IEvent ev, Guid eventId, IDictionary<string, object> headers = null)
        {
            var type = ev.GetType();

            var eventHeaders = new Dictionary<string, object>(headers ?? NoHeaders)
            {
                { EventClrTypeHeader, type.AssemblyQualifiedName }
            };
            
            var data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(ev));
            var metadata = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(eventHeaders));

            return new EventData(eventId, type.Name, true, data, metadata);
        }

        /// <summary>
        /// Converts EventStore's <see cref="RecordedEvent"/> to an <see cref="IEvent"/> object.
        /// </summary>
        public static IEvent ToIEvent(this RecordedEvent ev)
        {
            if (!ev.IsJson)
                throw new ArgumentException("The event is not JSON-formatted");

            var metadata = Encoding.UTF8.GetString(ev.Metadata);
            var headers = JsonConvert.DeserializeObject<IDictionary<string, object>>(metadata);

            if (headers == null || !headers.TryGetValue(EventClrTypeHeader, out var typeNameEntry))
                throw new ArgumentException("Cannot resolve event type: Metadata does not specify the CLR type");

            var typeName = typeNameEntry.ToString();
            var type = Type.GetType(typeName);

            if (type == null)
                throw new ArgumentException($"Cannot resolve event type: '{typeName}'");

            if (!typeof(IEvent).IsAssignableFrom(type))
                throw new ArgumentException($"The event type does not implement {nameof(IEvent)}");

            var json = Encoding.UTF8.GetString(ev.Data);
            return (IEvent)JsonConvert.DeserializeObject(json, type);
        }
    }
}
