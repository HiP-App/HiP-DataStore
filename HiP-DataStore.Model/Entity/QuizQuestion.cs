using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using System.Collections.Generic;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Entity
{
    public class QuizQuestion : ContentBase
    {
        public int ExhibitId { get; set; }
        public string Text { get; set; }

        public List<string> Options { get; set; }

        public int? Image { get; private set; }

        public QuizQuestion() { }

        public QuizQuestion(ExhibitQuizQuestionArgs args)
        {
            if (args.ExhibitId != null) ExhibitId = args.ExhibitId.Value;
            Status = args.Status;
            Text = args.Text;
            Options = args.Options;
            Image = args.Image;
        }

        public ExhibitQuizQuestionArgs CreateExhibitQuizQuestionArgs() => new ExhibitQuizQuestionArgs()
        {
            ExhibitId = ExhibitId,
            Text = Text,
            Options = Options,
            Image = Image,
            Status = Status
        };

    }
}
