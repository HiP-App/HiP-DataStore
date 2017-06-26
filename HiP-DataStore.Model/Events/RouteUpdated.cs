using System;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public class RouteUpdated : IUpdateEvent
    {
        public int Id { get; set; }

        public RouteArgs Properties { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public ContentStatus GetStatus() => Properties.Status;

        public ResourceType GetEntityType() => ResourceType.Route;
    }
}
