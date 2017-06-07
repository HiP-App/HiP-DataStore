using System.Runtime.Serialization;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model
{
    public enum ContentStatus
    {
        // Note:
        // [EnumMember(...)] only applies to JSON responses, not to requests. We are only using it to make the
        // enum values uppercase in the responses (to match the spec). In requests, upper-/lowercasing does not
        // matter, so the enum values are named 95% according to C# conventions, we only include the underscore
        // in "In_Review" so that we can use "IN_REVIEW" in requests.

        /// <summary>
        /// The content is an unpublished draft.
        /// </summary>
        [EnumMember(Value = "DRAFT")]
        Draft,

        /// <summary>
        /// The content needs to be reviewed.
        /// </summary>
        /// <remarks>
        /// [EnumMember(...)] only applies to JSON responses, not to requests.
        /// That's why 
        /// </remarks>
        [EnumMember(Value = "IN_REVIEW")]
        In_Review,

        /// <summary>
        /// The content is approved by a supervisor.
        /// </summary>
        [EnumMember(Value = "PUBLISHED")]
        Published,

        /// <summary>
        /// Any status. 'All' may only be used for requests (to indicate that content of any status should be
        /// included in the response) - it is not a valid status for content.
        /// </summary>
        [EnumMember(Value = "ALL")]
        All
    }
}
