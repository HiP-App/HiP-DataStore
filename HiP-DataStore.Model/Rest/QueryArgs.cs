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
        [JsonIgnore]
        public ContentStatus Status
        {
#pragma warning disable 0618
            get => InternalStatus ?? ContentStatus.Published;
            set => InternalStatus = value;
#pragma warning restore 0618
        }

        /// <remarks>
        /// Property 'InternalStatus' is listed by NSwag, but not by Swashbuckle (due to 'internal').
        /// Property 'Status' is listed by Swashbuckle, but not by NSwag (due to [JsonIgnore]).
        /// 
        /// We need a NULLABLE status parameter for NSwag because:
        /// 1) The status parameter shouldn't be required (clients shouldn't need to pass it, it defaults to PUBLISHED)
        /// 2) Since status is not required, the NSwag-generated C# client has "ContentStatus? status = null" in the
        ///    method signature, however if it weren't nullable here, the client would throw an exception if 'status == null'.
        ///    This is weird: The method signature states that status can be null, but passing null throws an exception.
        ///    
        /// Why don't we make Status nullable in general? We don't want the rest of the codebase to have to distinguish
        /// between 'Status == null' and 'Status == Published'.
        /// </remarks>
        [JsonProperty("status")]
        [DefaultValue(ContentStatus.Published)]
        [Obsolete("For internal use only. Use 'Status' instead.")]
        internal ContentStatus? InternalStatus { get; set; }

        public DateTimeOffset? Timestamp { get; set; }
    }
}
