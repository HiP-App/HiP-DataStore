using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
    public class QueryArgs
    {
        /// <summary>
        /// IDs excluded from the response.
        /// </summary>
        [JsonProperty("exclude")]
        public List<int> ExcludedIds { get; set; }

        /// <summary>
        /// If set, only these IDs are included in the response.
        /// If null, all IDs are included in the response.
        /// In both cases, the filter <see cref="ExcludedIds"/> still applies.
        /// </summary>
        [JsonProperty("includeOnly")]
        public List<int> IncludedIds { get; set; }

        /// <summary>
        /// The page number. Defaults to zero, which is the first page.
        /// </summary>
        [Range(0, int.MaxValue)]
        public int Page { get; set; }

        /// <summary>
        /// Maximum number of entities in the response. The last page may have less entities,
        /// all other pages have exactly <see cref="PageSize"/> entities.
        /// </summary>
        [Range(1, int.MaxValue)]
        [DefaultValue(10)]
        public int PageSize { get; set; } = 10;

        /// <summary>
        /// The field to order the response data by.
        /// </summary>
        public string OrderBy { get; set; }

        /// <summary>
        /// Restricts the reponse results to the objects containing the queried string in a text field
        /// (title/name, description).
        /// </summary>
        public string Query { get; set; }

        /// <summary>
        /// Restricts the reponse to objects with the given status or specifies that all objects irrespective
        /// of their status shall be returned. Defaults to <see cref="ContentStatus.Published"/>.
        /// </summary>
        [DefaultValue(ContentStatus.Published)]
        public ContentStatus Status { get; set; } = ContentStatus.Published;

        public DateTimeOffset? Timestamp { get; set; }
    }
}
