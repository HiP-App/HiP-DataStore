using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using PaderbornUniversity.SILab.Hip.EventSourcing;
using System.Collections.Generic;
using System.Linq;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public class MediaCreated : UserActivityBaseEvent, ICreateEvent
    {
        public MediaArgs Properties { get; set; }

        public override ResourceType GetEntityType() => ResourceType.Media;

        public ContentStatus GetStatus() => Properties.Status;

        public IEnumerable<EntityId> GetReferences() => Enumerable.Empty<EntityId>();
    }
}
