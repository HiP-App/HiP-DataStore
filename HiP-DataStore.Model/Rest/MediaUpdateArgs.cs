using MongoDB.Bson.Serialization.Attributes;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
    public class MediaUpdateArgs
    {
        [BsonIgnoreIfNull]
        public string Title { get; set; }

        [BsonIgnoreIfNull]
        public string Description { get; set; }

        [BsonIgnoreIfNull]
        public MediaType? Type { get; set; }

        [BsonIgnoreIfNull]
        public ContentStatus? Status { get; set; }
    }
}
