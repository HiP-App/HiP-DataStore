using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using System;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public class ExhibitPageUpdated2 : IUpdateEvent
    {
        public int Id { get; set; }

        public int ExhibitId { get; set; }

        public ExhibitPageArgs2 Properties { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public ResourceType GetEntityType() => ResourceType.ExhibitPage;

        public ContentStatus GetStatus() => Properties.Status;
    }

    [Obsolete]
    public class ExhibitPageUpdated : IUpdateEvent, IMigratable<ExhibitPageUpdated2>
    {
        public int Id { get; set; }

        public int ExhibitId { get; set; }

        public ExhibitPageArgs Properties { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public ResourceType GetEntityType() => ResourceType.ExhibitPage;

        public ContentStatus GetStatus() => Properties.Status;

        public ExhibitPageUpdated2 Migrate() => new ExhibitPageUpdated2
        {
            Id = Id,
            ExhibitId = ExhibitId,
            Timestamp = Timestamp,
            Properties = Properties.Migrate()
        };
    }
}
