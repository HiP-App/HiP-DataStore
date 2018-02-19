using MongoDB.Bson.Serialization.Attributes;
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
            Status = args.Status;
            Text = args.Text;
            Options = args.Options;
            Image = args.Image;
        }

        public QuizQuestion(QuizQuestion other)
        {
            Status = other.Status;
            Text = other.Text;
            Options = other.Options;
            Image = other.Image;
            ExhibitId = other.ExhibitId;
        }

    }
}
