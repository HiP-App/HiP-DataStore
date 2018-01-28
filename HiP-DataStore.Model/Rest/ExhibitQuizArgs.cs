using PaderbornUniversity.SILab.Hip.DataStore.Model.Utility;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
    public class ExhibitQuizArgs : IContentArgs
    {
        [Required]
        public int? ExhibitId { get; set; }

        [ListRange(Maximum = 10)]
        [Required]
        public List<ExhibitQuizQuestionArgs> Questions { get; set; }

        [AllowedStatuses]
        public ContentStatus Status { get; set; }
    }

    public class ExhibitQuizQuestionArgs
    {
        [Required]
        public string Text { get; set; }

        /// First option is by default the right answer
        [ListRange(Maximum = 4, Minimum = 4 ,ErrorMessage = "Number of options is restricted to 4")]
        [Required]
        public List<string> Options { get; set; }

        [Reference(nameof(ResourceTypes.Media))]
        public int? Image { get; set; }
    }
}
