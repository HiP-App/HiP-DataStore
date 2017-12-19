using PaderbornUniversity.SILab.Hip.EventSourcing;
using System;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Entity
{
    public class ScoreRecord : IEntity<int>
    {
        public int Id { get; set; }

        public string UserId { get; set; }

        public int Score { get; set; }

        public DateTimeOffset Timestamp { get; set; }

    }
}
