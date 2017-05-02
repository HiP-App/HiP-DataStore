using MongoDB.Bson;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Entity
{
    public class Tag : ContentBase
    {
        public const string CollectionName = "tags";

        public ObjectId Id { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public DocRef<Image> Image { get; set; }
    }
}
