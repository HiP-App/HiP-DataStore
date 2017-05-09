using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model
{

    /// <summary>
    /// A strongly-typed reference to another document in a Mongo database.
    /// </summary>
    /// <remarks>
    /// DocRefs are for internal use, they should not be exposed via the public REST interface.
    /// </remarks>
    public class DocRef<T> : DocRefBase
    {
        /// <summary>
        /// ID of the referenced object.
        /// </summary>
        public BsonValue Id { get; set; }

        public DocRef(string collection = null, string database = null) : base(collection, database)
        {
        }

        [BsonConstructor]
        public DocRef(BsonValue id, string collection = null, string database = null) : base(collection, database)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
        }

        public override string ToString() =>
            $"{Id} (collection: '{Collection ?? "<unspecified>"}', database: '{Database ?? "<unspecified>"}')";
    }
}
