using System;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    // Version info: Added ExhibitId, so that the ID of the deleted page can be removed from the
    // ordered list "Exhibit.Pages" of the corresponding exhibit
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
