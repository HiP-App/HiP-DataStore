using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace PaderbornUniversity.SILab.Hip.DataStore.Core
{
    public class HistorySummary
    {
        /// <summary>
        /// The time and date when the entity was created.
        /// If the entity is effectively non-existent (i.e. has never been created or has been deleted),
        /// no value is set.
        /// </summary>
        public DateTimeOffset? Created { get; set; }

        /// <summary>
        /// The time and date of the last creation, update or deletion.
        /// If the entity is effectively non-existent (i.e. has never been created or has been deleted),
        /// no value is set.
        /// </summary>
        public DateTimeOffset? LastModified { get; set; }

        /// <summary>
        /// The time and date when the entity was deleted.
        /// A value is only set if the entity has been created and deleted before, but not (yet) recreated.
        /// </summary>
        public DateTimeOffset? Deleted { get; set; }

        /// <summary>
        /// The name of the user who created the entity.
        /// </summary>
        public string Owner { get; set; }

        /// <summary>
        /// The ID of the user who created the entity.
        /// </summary>
        public string OwnerID { get; set; }

        /// <summary>
        /// A list of individual modifications (including creation and deletion).
        /// </summary>
        public IList<Change> Changes { get; } = new List<Change>();

        public class Change
        {
            public DateTimeOffset Timestamp { get; set; }
            public string Description { get; set; }
            public string UserId { get; set; }
            public string UserName { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string Property { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public object Value { get; set; }

            public Change(DateTimeOffset timestamp, string description, string userId,string userName, string property = null, object value = null)
            {
                Timestamp = timestamp;
                Description = description;
                UserId = userId;
                UserName = userName;
                Property = property;
                Value = value;
            }
        }
    }
}
