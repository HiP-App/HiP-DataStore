namespace PaderbornUniversity.SILab.Hip.DataStore.Model
{
    public enum ContentStatus
    {
        /// <summary>
        /// The content is an unpublished draft.
        /// </summary>
        Draft,

        /// <summary>
        /// The content needs to be reviewed.
        /// </summary>
        InReview,

        /// <summary>
        /// The content is approved by a supervisor.
        /// </summary>
        Published,

        /// <summary>
        /// Any status. 'All' may only be used for requests (to indicate that content of any status should be
        /// included in the response) - it is not a valid status for content.
        /// </summary>
        All
    }
}
