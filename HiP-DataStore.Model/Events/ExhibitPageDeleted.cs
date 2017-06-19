using System;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public class ExhibitPageDeleted2 : IDeleteEvent
    {
        public int Id { get; set; }

        public int ExhibitId { get; set; }

        public ResourceType GetEntityType() => ResourceType.ExhibitPage;
    }

    [Obsolete]
    public class ExhibitPageDeleted : IDeleteEvent, IMigratable<ExhibitPageDeleted2>
    {
        public int Id { get; set; }

        public ResourceType GetEntityType() => ResourceType.ExhibitPage;

        public ExhibitPageDeleted2 Migrate() => new ExhibitPageDeleted2
        {
            Id = Id,
            ExhibitId = -1
        };
    }
}
