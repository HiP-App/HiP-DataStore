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
        public IList<string> ExcludedIds { get; set; }

        [JsonProperty("includeOnly")]
        public IList<string> IncludedIds { get; set; }

        [Range(0, int.MaxValue)]
        public int Page { get; set; }

        [Range(1, int.MaxValue)]
        [DefaultValue(int.MaxValue)]
        public int PageSize { get; set; } = int.MaxValue;

        public virtual string OrderBy { get; set; }

        public string Query { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [DefaultValue(ContentStatus.Published)]
        public ContentStatus Status { get; set; } = ContentStatus.Published;

        public DateTimeOffset? Timestamp { get; set; }

        /// <summary>
        /// Checks if an ID should be included in the query result.
        /// This is the case if:
        /// 1) it is not explicitly excluded
        /// 2) it is explicitly included or <see cref="IncludedIds"/> is null
        /// 
        /// For example: IDs = [1,2,3,4,5], ExcludedIds = [1,2], IncludedIds = [2,3,4]
        /// => effectively included IDs = ([1,2,3,4,5] \ [1,2]) ∩ [2,3,4] = [3,4]
        /// </summary>
        public bool IncludesId(string id) =>
            (ExcludedIds == null || !ExcludedIds.Contains(id)) &&
            (IncludedIds == null || IncludedIds.Contains(id));

    }
}
