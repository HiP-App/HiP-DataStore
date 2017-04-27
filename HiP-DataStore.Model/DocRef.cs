using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model
{
    /// <summary>
    /// A strongly-typed reference to another document in a Mongo database.
    /// </summary>
    /// <remarks>
    /// This should really be a struct, but deserialization of structs is currently not supported by MongoDB.
    /// DocRefs are for internal use, they should not be exposed via the public REST interface.
    /// </remarks>
    public class DocRef<T>
    {
        /// <summary>
        /// ID of the referenced object.
        /// </summary>
        public BsonValue Id { get; }

        /// <summary>
        /// The name of the collection where the referenced document is. This property is optional:
        /// If not set, the referenced document is assumed to be in the same collection as the referencing document.
        /// </summary>
        public string Collection { get; }

        /// <summary>
        /// The name of the database where the referenced document is. This property is optional:
        /// If not set, the referenced document is assumed to be in the same database as the referencing document.
        /// </summary>
        public string Database { get; }

        [BsonConstructor]
        public DocRef(BsonValue id, string collection = null, string database = null)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Collection = collection;
            Database = database;
        }

        public override string ToString() =>
            $"{Id} (collection: '{Collection ?? "<unspecified>"}', database: '{Database ?? "<unspecified>"}')";
    }
}
