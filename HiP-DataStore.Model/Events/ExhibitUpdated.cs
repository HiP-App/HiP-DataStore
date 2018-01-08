using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using PaderbornUniversity.SILab.Hip.EventSourcing;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public class ExhibitUpdated : UserActivityBaseEvent, IUpdateEvent
    {
        public ExhibitArgs Properties { get; set; }

        public override ResourceType GetEntityType() => ResourceTypes.Exhibit;

        public ContentStatus GetStatus() => Properties.Status;
    }
}
