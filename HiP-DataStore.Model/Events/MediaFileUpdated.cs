using System;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public class MediaFileUpdated : IUpdateFileEvent
    {
        public int Id { get; set; }

        public string File { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public ResourceType GetEntityType() => ResourceType.Media;
    }
}
