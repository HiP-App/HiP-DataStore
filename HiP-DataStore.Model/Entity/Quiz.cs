using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Converters;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using PaderbornUniversity.SILab.Hip.EventSourcing.Mongo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Entity
{
    public class Quiz : ContentBase
    {
        public int ExhibitId { get; set; }

        public List<QuizQuestion> Questions { get; set; } = new List<QuizQuestion>();

        public Quiz() { }
        
        public Quiz(ExhibitQuizArgs args)
        {
            ExhibitId = args.ExhibitId;
            Status = args.Status;
            args.Questions.ForEach(x => Questions.Add(new QuizQuestion(x)));
        }

        public ExhibitQuizArgs CreateQuizArgs()
        {
            var args = new ExhibitQuizArgs()
            {
                ExhibitId = ExhibitId,
                Questions = new List<ExhibitQuizQuestionArgs>(this.Questions.Select(x => x.CreateQuizQuestionArgs())),
                Status = Status
            };
            return args;
        }

    }

    public class QuizQuestion
    {
        public string Question { get; set; }

        public List<string> Options { get; set; }

        [BsonElement]
        public DocRef<MediaElement> Image { get; private set; } =
            new DocRef<MediaElement>(ResourceTypes.Media.Name);

        public QuizQuestion() { }

        public QuizQuestion(ExhibitQuizQuestionArgs args)
        {
            Question = args.Quistion;
            Options = args.Options;
            Image.Id = args.Image;
        }

        public ExhibitQuizQuestionArgs CreateQuizQuestionArgs()
        {
            var args = new ExhibitQuizQuestionArgs()
            {
                Quistion = Question,
                Options = Options,
                Image = Image.Id.AsNullableInt32
            };
            return args;
        }
    }
}
