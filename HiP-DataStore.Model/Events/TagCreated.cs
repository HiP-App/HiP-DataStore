using System;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public class TagCreated : ICreateEvent
    {
        public int Id { get; set; }

        public TagArgs Properties { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public ResourceType GetEntityType() => ResourceType.Tag;

        public ContentStatus GetStatus() => Properties.Status;
    }
}
