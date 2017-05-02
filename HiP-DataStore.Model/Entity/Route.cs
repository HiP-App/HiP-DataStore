using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Entity
{
    public class Route : ContentBase
    {
        public const string CollectionName = "routes";

        // TODO: What about waypoints?

        [BsonElement(nameof(Image))]
        private DocRef<Image> _image = new DocRef<Image>(Entity.Image.CollectionName);

        [BsonElement(nameof(Audio))]
        private DocRef<Audio> _audio = new DocRef<Audio>(Entity.Audio.CollectionName);

        [BsonElement(nameof(Exhibits))]
        private DocRefList<Exhibit> _exhibits = new DocRefList<Exhibit>(Exhibit.CollectionName);

        [BsonElement(nameof(Tags))]
        private DocRefList<Tag> _tags = new DocRefList<Tag>(Tag.CollectionName);

        public ObjectId Id { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public int Duration { get; set; }

        public double Distance { get; set; }

        public DocRef<Image> Image => _image;

        public DocRef<Audio> Audio => _audio;

        public DocRefList<Exhibit> Exhibits => _exhibits;

        public DocRefList<Tag> Tags => _tags;
    }
}
