using System;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Entity
{
    public class ScoreRecord
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int Score { get; set; }

        public DateTimeOffset Timestamp { get; set; }

    }
}
