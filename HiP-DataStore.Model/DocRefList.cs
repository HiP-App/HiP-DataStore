using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model
{
    /// <summary>
    /// A strongly-typed reference to multiple other documents in a Mongo database.
    /// </summary>
    /// <remarks>
    /// Unfortunately this type cannot implement <see cref="IEnumerable{BsonValue}"/> or any derived type
    /// because the BSON serializer then serializes objects of this type as array, forgetting about the fields
    /// 'Collection' and 'Database'.
    /// </remarks>
    public class DocRefList<T> : DocRefBase//, ICollection<BsonValue>
    {
        [BsonElement]
        public HashSet<BsonValue> Ids { get; private set; } = new HashSet<BsonValue>();

        public int Count => Ids.Count;

        public DocRefList()
        {
        }

        public DocRefList(string collection = null, string database = null) : base(collection, database)
        {
        }

        public DocRefList(IEnumerable<BsonValue> ids, string collection = null, string database = null) : base(collection, database)
        {
            if (ids == null)
                throw new ArgumentNullException(nameof(ids));

            Ids = new HashSet<BsonValue>(ids);
        }

        public bool Add(BsonValue id) => Ids.Add(id);

        public void Add(IEnumerable<BsonValue> ids)
        {
            foreach (var id in ids ?? Enumerable.Empty<BsonValue>())
                Ids.Add(id);
        }

        public bool Remove(BsonValue id) => Ids.Remove(id);

        public void Clear() => Ids.Clear();

        public bool Contains(BsonValue id) => Ids.Contains(id);

        public IEnumerator<BsonValue> GetEnumerator() => Ids.GetEnumerator();
    }
}
