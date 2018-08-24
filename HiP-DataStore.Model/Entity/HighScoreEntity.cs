using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Entity
{
    public class HighScoreEntity : ContentBase
    {
        public int ExhibitId { get; set; }
        public double HighScore { get; set; }

        public HighScoreEntity(ExhibitHighScoreArgs args)
        {
            ExhibitId = args.ExhibitId;
            HighScore = args.HighScore;
        }

        public ExhibitHighScoreArgs CreateExhibitHighscoreArgs()
        {
            var args = new ExhibitHighScoreArgs()
            {
                ExhibitId = this.ExhibitId,
                HighScore = this.HighScore
            };
            return args;
        }
    }
}
