using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
    public class ExhibitQuizQuestionResult
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public ContentStatus Status { get; set; }

        public DateTimeOffset Timestamp { get; set; }
        public string Text { get; set; }

        public int Id { get; set; }

        public int ExhibitId { get; set; }

        public IReadOnlyCollection<string> Options { get; set; }

        public int? Image { get; set; }

        public ExhibitQuizQuestionResult() { }

        public ExhibitQuizQuestionResult(QuizQuestion question)
        {
            Id = question.Id;
            ExhibitId = question.ExhibitId;
            Status = question.Status;
            Text = question.Text;
            Options = question.Options;
            Image = question.Image;
            Timestamp = question.Timestamp;
        }
    }
}
