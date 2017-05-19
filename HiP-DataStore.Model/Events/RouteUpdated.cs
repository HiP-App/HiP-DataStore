using System;
using Newtonsoft.Json;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public class RouteUpdated : IUpdateEvent
    {
        public int Id { get; set; }

        public RouteArgs Properties { get; set; }

        // TODO: Timestamp should be moved to ICrudEvent, since every create/update/delete should have a timestamp
        // (timestamp is currently missing in *Created-events and determined through the cache DB which is not nice)
        public DateTimeOffset Timestamp { get; set; }

        [JsonIgnore]
        public ContentStatus Status => Properties.Status;

        [JsonIgnore]
        public ResourceType EntityType => ResourceType.Route;
    }
}
