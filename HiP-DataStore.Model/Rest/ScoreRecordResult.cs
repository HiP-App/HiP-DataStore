using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
    public class ScoreRecordResult : ScoreResult
    {
        public int UserId { get; set; }

        public ScoreRecordResult() { }

        public ScoreRecordResult(ScoreRecord score) : base(score)
        {
            this.UserId = score.UserId;
        }
    }
}
