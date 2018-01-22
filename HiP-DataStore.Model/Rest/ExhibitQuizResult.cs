using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;

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

        public bool Used { get; set; }
    }

    public class ExhibitQuizQuestionResult
    {
        public string Question { get; set; }

        public IReadOnlyCollection<string> Options { get; set; }

        public int? Image { get; set; }
    }
}
