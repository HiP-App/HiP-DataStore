using MongoDB.Bson;
using System;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model
{
    /// <summary>
    /// A strongly-typed reference to another document in a Mongo database.
    /// </summary>
    public struct DocRef<T>
    {
        /// <summary>
        /// ID of the referenced object.
        /// </summary>
        public BsonValue Id { get; set; }

        /// <summary>
        /// The name of the collection where the referenced document is. This property is optional:
        /// If not set, the referenced document is assumed to be in the same collection as the referencing document.
        /// </summary>
        public string Collection { get; set; }

        /// <summary>
        /// The name of the database where the referenced document is. This property is optional:
        /// If not set, the referenced document is assumed to be in the same database as the referencing document.
        /// </summary>
        public string Database { get; set; }

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
