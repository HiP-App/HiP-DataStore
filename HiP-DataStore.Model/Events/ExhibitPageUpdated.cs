using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using PaderbornUniversity.SILab.Hip.EventSourcing;
using PaderbornUniversity.SILab.Hip.EventSourcing.Migrations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    // Version info: As a consequence of a flat page hierarchy, pages no longer belong to exactly one exhibit
    public class ExhibitPageUpdated3 : UserActivityBaseEvent, IUpdateEvent
    {
        public ExhibitPageArgs2 Properties { get; set; }

        public override ResourceType GetEntityType() => ResourceTypes.ExhibitPage;

        public ContentStatus GetStatus() => Properties.Status;
    }

    [Obsolete]
    public class ExhibitPageUpdated2 : IUpdateEvent
    {
        public int Id { get; set; }

        public int ExhibitId { get; set; }

        public ExhibitPageArgs2 Properties { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public ResourceType GetEntityType() => ResourceTypes.ExhibitPage;

        public ContentStatus GetStatus() => Properties.Status;
    }

    [Obsolete]
    public class ExhibitPageUpdated : IUpdateEvent, IMigratable<ExhibitPageUpdated2>
    {
        public int Id { get; set; }

        public int ExhibitId { get; set; }

        public ExhibitPageArgs Properties { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public ResourceType GetEntityType() => ResourceTypes.ExhibitPage;

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
