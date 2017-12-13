using MongoDB.Bson.Serialization.Attributes;
using System;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model
{
    public class ResourceType : IEquatable<ResourceType>
    {
        public static readonly ResourceType Exhibit = new ResourceType("Exhibit");
        public static readonly ResourceType ExhibitPage = new ResourceType("ExhibitPage");
        public static readonly ResourceType Route = new ResourceType("Route");
        public static readonly ResourceType Media = new ResourceType("Media");
        public static readonly ResourceType Tag = new ResourceType("Tag");
        public static readonly ResourceType ScoreRecord = new ResourceType("ScoreRecord");
        public static readonly ResourceType Rating = new ResourceType("Rating");
        public static readonly ResourceType Review = new ResourceType("Review");

        /// <summary>
        /// This name is used in two ways:
        /// 1) as a "type"/"kind of resource" identifier in events
        /// 2) as the collection name in the MongoDB cache database
        /// </summary>
        [BsonElement]
        public string Name { get; }

        [BsonConstructor]
        public ResourceType(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Name was null or empty", nameof(name));

            Name = name;
        }

        public override string ToString() => Name ?? "";

        public override int GetHashCode() => Name.GetHashCode();

        public override bool Equals(object obj) => obj is ResourceType other && Equals(other);

        public bool Equals(ResourceType other) => Name == other.Name;

        public static bool operator ==(ResourceType a, ResourceType b) => a?.Equals(b) ?? b == null;

        public static bool operator !=(ResourceType a, ResourceType b) => !(a == b);
    }
}