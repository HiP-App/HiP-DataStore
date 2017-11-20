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
        public List<int> Exclude { get; set; }

        /// <summary>
        /// If set, only these IDs are included in the response.
        /// If null, all IDs are included in the response.
        /// In both cases, the filter <see cref="Exclude"/> still applies.
        /// </summary>
        public List<int> IncludeOnly { get; set; }

        /// <summary>
        /// The page number. Defaults to one, which is the first page.
        /// </summary>
        [Range(1, int.MaxValue)]
        public int? Page { get; set; }

        /// <summary>
        /// Maximum number of entities in the response. The last page may have less entities,
        /// all other pages have exactly <see cref="PageSize"/> entities.
        /// </summary>
        /// <remarks>
        /// If <see cref="Page"/> is specified (!= null), the page size defaults to 10.
        /// Otherwise, it defaults to <see cref="int.MaxValue"/>, so that all items are returned.
        /// </remarks>
        [Range(1, int.MaxValue)]
        public int? PageSize { get; set; }

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
        public ContentStatus Status { get; set; }

        public DateTimeOffset? Timestamp { get; set; }
    }
}
