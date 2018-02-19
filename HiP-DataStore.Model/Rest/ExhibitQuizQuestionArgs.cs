using PaderbornUniversity.SILab.Hip.DataStore.Model.Utility;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{

    public class ExhibitQuizQuestionArgs : IContentArgs
    {
        [AllowedStatuses]
        public ContentStatus Status { get; set; }

        [Required]
        public string Text { get; set; }

        /// First option is by default the right answer
        [ListRange(Maximum = 4, Minimum = 4, ErrorMessage = "Number of options is restricted to 4")]
        [Required]
        public List<string> Options { get; set; }

        [Reference(nameof(ResourceTypes.Media))]
        public int? Image { get; set; }


        public override bool Equals(object obj)
        {
            var question = obj as ExhibitQuizQuestionArgs;
            return question != null &&
                   Text == question.Text &&
                   Options.SequenceEqual(question.Options) &&
                   EqualityComparer<int?>.Default.Equals(Image, question.Image);
        }

        public override int GetHashCode()
        {
            var hashCode = -734600475;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Text);
            hashCode = hashCode * -1521134295 + EqualityComparer<List<string>>.Default.GetHashCode(Options);
            hashCode = hashCode * -1521134295 + EqualityComparer<int?>.Default.GetHashCode(Image);
            return hashCode;
        }
    }
}
