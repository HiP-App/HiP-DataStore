using System;
using Newtonsoft.Json;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public class RouteUpdated : IUpdateEvent
    {
        public int Id { get; set; }

        public RouteArgs Properties { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        [JsonIgnore]
        public ContentStatus Status => Properties.Status;

        [JsonIgnore]
        public ResourceType EntityType => ResourceType.Route;
    }
}
