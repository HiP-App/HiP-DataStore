
using System.ComponentModel.DataAnnotations;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
    public class ExhibitHighScoreArgs
    {
        [Required]
        public int ExhibitId { get; set; }
        [Required]
        [Range(0, double.MaxValue)]
        public double HighScore { get; set; }
    }
}
