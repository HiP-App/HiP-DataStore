using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using System;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
    public class ScoreResult
    {
        public int Score { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public ScoreResult() { }

        public ScoreResult(ScoreRecord score)
        {
            Score = score.Score;
            Timestamp = score.Timestamp;
        }
    }
}
