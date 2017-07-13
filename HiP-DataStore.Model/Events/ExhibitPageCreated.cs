using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using System;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public class ExhibitPageCreated2 : ICreateEvent
    {
        public int Id { get; set; }

        public int ExhibitId { get; set; }

        public ExhibitPageArgs2 Properties { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public ResourceType GetEntityType() => ResourceType.ExhibitPage;

        public ContentStatus GetStatus() => Properties.Status;
    }

    [Obsolete]
    public class ExhibitPageCreated : ICreateEvent, IMigratable<ExhibitPageCreated2>
    {
        public int Id { get; set; }

        public int ExhibitId { get; set; }

        public ExhibitPageArgs Properties { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public ResourceType GetEntityType() => ResourceType.ExhibitPage;

        public ContentStatus GetStatus() => Properties.Status;

        public ExhibitPageCreated2 Migrate() => new ExhibitPageCreated2
        {
            Id = Id,
            ExhibitId = ExhibitId,
            Timestamp = Timestamp,
            Properties = Properties.Migrate()
        };
    }
}
