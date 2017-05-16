using MongoDB.Bson.Serialization.Attributes;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model
{
    public struct ResourceType
    {
        public static readonly ResourceType Exhibit = new ResourceType("Exhibit");
        public static readonly ResourceType Route = new ResourceType("Route");
        public static readonly ResourceType Media = new ResourceType("Media");
        public static readonly ResourceType Tag = new ResourceType("Tag");

        /// <summary>
        /// This name is used in two ways:
        /// 1) as a "type"/"kind of resource" identifier in events
        /// 2) as the collection name in the MongoDB cache database
        /// </summary>
        [BsonElement]
        public string Name { get; }

        [BsonConstructor]
        public ResourceType(string name) => Name = name;
    }
}
