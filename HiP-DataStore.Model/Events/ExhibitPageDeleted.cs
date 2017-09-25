using System;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    // Version info: As a consequence of a flat page hierarchy, pages no longer belong to exactly one exhibit
    public class ExhibitPageDeleted2 : UserActivityBaseEvent, IDeleteEvent
    {
        public override ResourceType GetEntityType() => ResourceType.ExhibitPage;
    }

    [Obsolete]
    public class ExhibitPageDeleted : IDeleteEvent
    {
        public int Id { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public int ExhibitId { get; set; }

        public ResourceType GetEntityType() => ResourceType.ExhibitPage;
    }
}
