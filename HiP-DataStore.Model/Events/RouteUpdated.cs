using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using PaderbornUniversity.SILab.Hip.EventSourcing;
using System.Collections.Generic;
using System.Linq;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public class RouteUpdated : UserActivityBaseEvent, IUpdateEvent
    {
        public RouteArgs Properties { get; set; }

        public ContentStatus GetStatus() => Properties.Status;

        public override ResourceType GetEntityType() => ResourceTypes.Route;

        public IEnumerable<EntityId> GetReferences() => Properties?.GetReferences() ?? Enumerable.Empty<EntityId>();
    }
}
