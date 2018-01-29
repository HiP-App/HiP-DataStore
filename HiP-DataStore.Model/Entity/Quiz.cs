using MongoDB.Bson.Serialization.Attributes;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using System.Collections.Generic;
using System.Linq;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Entity
{
    public class Quiz : ContentBase
    {
        public int ExhibitId { get; set; }

        public List<QuizQuestion> Questions { get; set; } = new List<QuizQuestion>();

        public Quiz() { }
        
        public Quiz(ExhibitQuizArgs args)
        {
            ExhibitId = args.ExhibitId.GetValueOrDefault();
            Status = args.Status;
            args.Questions?.ForEach(x => Questions.Add(new QuizQuestion(x)));
        }

        public ExhibitQuizArgs CreateQuizArgs()
        {
            var args = new ExhibitQuizArgs()
            {
                ExhibitId = ExhibitId,
                Questions = new List<ExhibitQuizQuestionArgs>(Questions.Select(x => x.CreateQuizQuestionArgs())),
                Status = Status
            };
            return args;
        }
    }

    public class QuizQuestion
    {
        public string Text { get; set; }

        public List<string> Options { get; set; }

        [BsonElement]
        public int? Image { get; private set; } 

        public QuizQuestion() { }

        public QuizQuestion(ExhibitQuizQuestionArgs args)
        {
            Text = args.Text;
            Options = args.Options;
            Image = args.Image;
        }

        public ExhibitQuizQuestionArgs CreateQuizQuestionArgs()
        {
            var args = new ExhibitQuizQuestionArgs()
            {
                Text = Text,
                Options = Options,
                Image = Image
            };
            return args;
        }             
    }
}
