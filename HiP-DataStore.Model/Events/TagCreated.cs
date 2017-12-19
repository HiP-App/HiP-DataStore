using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using PaderbornUniversity.SILab.Hip.EventSourcing;
using System.Collections.Generic;
using System.Linq;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public class TagCreated : UserActivityBaseEvent, ICreateEvent
    {
        public TagArgs Properties { get; set; }

        public override ResourceType GetEntityType() => ResourceTypes.Tag;

        public ContentStatus GetStatus() => Properties.Status;
    }
}
