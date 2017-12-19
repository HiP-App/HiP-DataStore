using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using PaderbornUniversity.SILab.Hip.EventSourcing;
using System.Collections.Generic;
using System.Linq;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public class ExhibitCreated : UserActivityBaseEvent, ICreateEvent
    {
        public ExhibitArgs Properties { get; set; }

        public override ResourceType GetEntityType() => ResourceTypes.Exhibit;

        public ContentStatus GetStatus() => Properties.Status;
    }
}
