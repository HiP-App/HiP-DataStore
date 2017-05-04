using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
    public class QueryArgs
    {
        [JsonProperty("exclude")]
        public List<int> ExcludedIds { get; set; }

        [JsonProperty("includeOnly")]
        public List<int> IncludedIds { get; set; }

        [Range(0, int.MaxValue)]
        public int Page { get; set; }

        [Range(1, int.MaxValue)]
        [DefaultValue(int.MaxValue)]
        public int PageSize { get; set; } = int.MaxValue;

        public virtual string OrderBy { get; set; }

        public string Query { get; set; }

        [DefaultValue(ContentStatus.Published)]
        public ContentStatus Status { get; set; } = ContentStatus.Published;

        public DateTimeOffset? Timestamp { get; set; }
    }
}
