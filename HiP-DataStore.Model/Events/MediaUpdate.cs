using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using System;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public  class MediaUpdate : IUpdateEvent
    {
        public int Id { get; set; }

        public MediaArgs Properties { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public ContentStatus GetStatus() => Properties.Status;

        public ResourceType GetEntityType() => ResourceType.Media;
    }
}
