using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using System.Collections.Generic;
using System.Linq;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public class TagCreated : UserActivityBaseEvent, ICreateEvent
    {
        public TagArgs Properties { get; set; }

        public override ResourceType GetEntityType() => ResourceType.Tag;

        public ContentStatus GetStatus() => Properties.Status;

        public IEnumerable<EntityId> GetReferences() => Properties?.GetReferences() ?? Enumerable.Empty<EntityId>();
    }
}
