using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using System;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public class RouteCreated : ICreateEvent
    {
        public int Id { get; set; }

        public RouteArgs Properties { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public ResourceType GetEntityType() => ResourceType.Route;

        public ContentStatus GetStatus() => Properties.Status;
    }
}
