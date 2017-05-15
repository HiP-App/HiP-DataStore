using MongoDB.Bson.Serialization.Attributes;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Entity
{
    public class Tag : ContentBase
    {
        public const string CollectionName = "tags";

        [BsonElement(nameof(Image))]
        private DocRef<MediaElement> _image = new DocRef<MediaElement>(MediaElement.CollectionName);

        public string Title { get; set; }

        public string Description { get; set; }

        public DocRef<MediaElement> Image => _image;
    }
}
