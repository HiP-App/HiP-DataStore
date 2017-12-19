using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using PaderbornUniversity.SILab.Hip.EventSourcing;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public class TagUpdated : UserActivityBaseEvent, IUpdateEvent
    {
        public TagArgs Properties { get; set; }

        public ContentStatus GetStatus() => Properties.Status;

        public override ResourceType GetEntityType() => ResourceTypes.Tag;
    }
}
