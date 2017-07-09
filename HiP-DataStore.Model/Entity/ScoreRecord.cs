using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using System;
using System.Collections.Generic;
using System.Text;

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
