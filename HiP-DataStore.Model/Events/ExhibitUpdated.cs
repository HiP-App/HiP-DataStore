using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using System;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public class ExhibitUpdated : IUpdateEvent
    {
        public int Id { get; set; }

        public ExhibitUpdateArgs Properties { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public ResourceType GetEntityType() => ResourceType.Exhibit;

        public ContentStatus GetStatus() => Properties.Status;
    }
}
