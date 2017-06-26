using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using System;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public class MediaCreated : ICreateEvent
    {
        public int Id { get; set; }

        public MediaArgs Properties { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public ResourceType GetEntityType() => ResourceType.Media;

        public ContentStatus GetStatus() => Properties.Status;
    }
}
