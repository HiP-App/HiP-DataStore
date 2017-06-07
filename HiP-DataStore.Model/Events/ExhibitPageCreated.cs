using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using System;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public class ExhibitPageCreated : ICreateEvent
    {
        public int Id { get; set; }

        public int ExhibitId { get; set; }

        public ExhibitPageArgs Properties { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public ResourceType GetEntityType() => ResourceType.ExhibitPage;

        public ContentStatus GetStatus() => Properties.Status;
    }
}
