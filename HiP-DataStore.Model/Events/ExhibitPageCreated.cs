using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using PaderbornUniversity.SILab.Hip.EventSourcing.Migrations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    // Version info: As a consequence of a flat page hierarchy, pages no longer belong to exactly one exhibit
    public class ExhibitPageCreated3 : ICreateEvent
    {
        public int Id { get; set; }

        public ExhibitPageArgs2 Properties { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public ResourceType GetEntityType() => ResourceType.ExhibitPage;

        public ContentStatus GetStatus() => Properties.Status;

        public IEnumerable<EntityId> GetReferences() => Properties?.GetReferences() ?? Enumerable.Empty<EntityId>();
    }

    [Obsolete]
    public class ExhibitPageCreated2 : ICreateEvent
    {
        public int Id { get; set; }

        public int ExhibitId { get; set; }

        public ExhibitPageArgs2 Properties { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public ResourceType GetEntityType() => ResourceType.ExhibitPage;

        public ContentStatus GetStatus() => Properties.Status;

        public IEnumerable<EntityId> GetReferences() => throw new NotSupportedException();
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

        public IEnumerable<EntityId> GetReferences() => throw new NotSupportedException();

        public ExhibitPageCreated2 Migrate() => new ExhibitPageCreated2
        {
            Id = Id,
            ExhibitId = ExhibitId,
            Timestamp = Timestamp,
            Properties = Properties.Migrate()
        };
    }
}
