using PaderbornUniversity.SILab.Hip.DataStore.Model.Utility;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
    public class ExhibitQuizArgs : IContentArgs
    {
        [Required]
        public int ExhibitId { get; set; }

        /// TODO: add validation: Max 10 quesions is allowed
        [Required]
        public List<ExhibitQuizQuestionArgs> Questions { get; set; }

        [AllowedStatuses]
        public ContentStatus Status { get; set; }
    }

    public class ExhibitQuizQuestionArgs
    {
        [Required]
        public string Quistion { get; set; }

        /// TODO: add validation: Only for options is allowed
        /// First option is by default the right answer
        [Required]
        public List<string> Options { get; set; }

        [Reference(nameof(ResourceTypes.Media))]
        public int? Image { get; set; }
    }

}
