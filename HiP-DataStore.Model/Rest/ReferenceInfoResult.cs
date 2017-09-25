using System.Collections.Generic;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
    /// <summary>
    /// For a particular resource, a <see cref="ReferenceInfoResult"/> provides information about
    /// (1) which other resources it references (or "uses"), and
    /// (2) which other resources are referencing (or "using") it
    /// </summary>
    public class ReferenceInfoResult
    {
        /// <summary>
        /// The resources referenced by the current resource.
        /// </summary>
        public IReadOnlyCollection<ReferenceInfo> OutgoingReferences { get; set; }

        /// <summary>
        /// The resources referencing the current resource.
        /// </summary>
        public IReadOnlyCollection<ReferenceInfo> IncomingReferences { get; set; }

        public class ReferenceInfo
        {
            public string Type { get; set; }

            public IReadOnlyCollection<int> Ids { get; set; }
        }
    }
}
