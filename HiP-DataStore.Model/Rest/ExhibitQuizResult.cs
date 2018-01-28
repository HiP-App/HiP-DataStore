using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
    public class ExhibitQuizResult
    {
        public int Id { get; set; }

        public int ExhibitId { get; set; }

        public IReadOnlyCollection<ExhibitQuizQuestionResult> Questions { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ContentStatus Status { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public ExhibitQuizResult() { }

        public ExhibitQuizResult(Quiz quiz)
        {
            Id = quiz.Id;
            ExhibitId = quiz.ExhibitId;
            Questions = quiz.Questions.Select(x => new ExhibitQuizQuestionResult(x)).ToArray();
            Status = quiz.Status;
        }
    }

    public class ExhibitQuizQuestionResult
    {
        public string Text { get; set; }

        public IReadOnlyCollection<string> Options { get; set; }

        public int? Image { get; set; }

        public ExhibitQuizQuestionResult() { }

        public ExhibitQuizQuestionResult(QuizQuestion question)
        {
            Text = question.Text;
            Options = question.Options;
            Image = question.Image;
        }
    }
}
