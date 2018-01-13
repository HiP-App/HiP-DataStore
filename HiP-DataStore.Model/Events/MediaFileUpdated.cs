using PaderbornUniversity.SILab.Hip.EventSourcing;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public class MediaFileUpdated : UserActivityBaseEvent, IUpdateFileEvent
    {
        public string File { get; set; }

        public override ResourceType GetEntityType() => ResourceTypes.Media;
    }
}
