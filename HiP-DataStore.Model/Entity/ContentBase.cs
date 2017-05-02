using System;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Entity
{
    /// <summary>
    /// Base class for routes, exhibits, pages, tags, media etc.
    /// </summary>
    public abstract class ContentBase
    {
        public ContentStatus Status { get; set; }

        /// <summary>
        /// The date and time of the last modification.
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }
    }
}
