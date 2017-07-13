using System;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public class ScoreAdded : IEvent
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int Score { get; set; }

        public DateTimeOffset Timestamp { get; set; }

    }
}
