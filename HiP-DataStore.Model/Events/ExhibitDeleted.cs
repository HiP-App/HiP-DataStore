using System;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public class ExhibitDeleted : IDeleteEvent
    {
        public int Id { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public ResourceType GetEntityType() => ResourceType.Exhibit;
    }
}
