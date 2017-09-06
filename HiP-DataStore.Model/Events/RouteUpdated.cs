using System;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using System.Collections.Generic;
using System.Linq;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public class RouteUpdated : IUpdateEvent
    {
        public int Id { get; set; }

        public RouteArgs Properties { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public ContentStatus GetStatus() => Properties.Status;

        public ResourceType GetEntityType() => ResourceType.Route;

        public IEnumerable<EntityId> GetReferences() => Properties?.GetReferences() ?? Enumerable.Empty<EntityId>();
    }
}
