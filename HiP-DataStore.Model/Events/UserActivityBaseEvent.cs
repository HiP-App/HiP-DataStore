using System;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public abstract class UserActivityBaseEvent : IUserActivityEvent
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public abstract ResourceType GetEntityType();
    }
}
