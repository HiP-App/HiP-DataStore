using PaderbornUniversity.SILab.Hip.DataStore.Model.Utility;
using PaderbornUniversity.SILab.Hip.EventSourcing;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{

    public class ExhibitQuizQuestionRestArgs : IContentArgs
    {
        [AllowedStatuses]
        public ContentStatus Status { get; set; }

        [Required]
        public string Text { get; set; }

        /// First option is by default the right answer
        [ListRange(Maximum = 4, Minimum = 4, ErrorMessage = "Number of options is restricted to 4")]
        [Required]
        public List<string> Options { get; set; }

        public int? Image { get; set; }
    }

    public class ExhibitQuizQuestionArgs : IContentArgs
    {
        /// <summary>
        /// This needs to nullable so that the <see cref="EntityManager"/> can also track exhibit id 0 (otherwise the default value would be 0)
        /// </summary>
        public int? ExhibitId { get; set; }
        public ContentStatus Status { get; set; }

        public string Text { get; set; }
        public List<string> Options { get; set; }

        [Reference(nameof(ResourceTypes.Media))]
        public int? Image { get; set; }

        public ExhibitQuizQuestionArgs() { }
        public ExhibitQuizQuestionArgs(int? exhibitId, ExhibitQuizQuestionRestArgs args)
        {
            ExhibitId = exhibitId;
            Status = args.Status;
            Text = args.Text;
            Options = args.Options;
            Image = args.Image;
        }
    }


}
