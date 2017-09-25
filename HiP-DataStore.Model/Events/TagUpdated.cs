using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using System.Collections.Generic;
using System.Linq;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public class TagUpdated : UserActivityBaseEvent, IUpdateEvent
    {
        public TagArgs Properties { get; set; }

        public ContentStatus GetStatus() => Properties.Status;

        public override ResourceType GetEntityType() => ResourceType.Tag;

        public IEnumerable<EntityId> GetReferences() => Properties?.GetReferences() ?? Enumerable.Empty<EntityId>();
    }
}
