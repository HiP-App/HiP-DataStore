using MongoDB.Bson.Serialization.Attributes;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Entity
{
    public class Route : ContentBase
    {
        public const string CollectionName = "routes";

        // TODO: What about waypoints?

        [BsonElement(nameof(Image))]
        private DocRef<MediaElement> _image = new DocRef<MediaElement>(MediaElement.CollectionName);

        [BsonElement(nameof(Audio))]
        private DocRef<MediaElement> _audio = new DocRef<MediaElement>(MediaElement.CollectionName);

        [BsonElement(nameof(Exhibits))]
        private DocRefList<Exhibit> _exhibits = new DocRefList<Exhibit>(Exhibit.CollectionName);

        [BsonElement(nameof(Tags))]
        private DocRefList<Tag> _tags = new DocRefList<Tag>(Tag.CollectionName);

        public string Title { get; set; }

        public string Description { get; set; }

        public int Duration { get; set; }

        public double Distance { get; set; }

        public DocRef<MediaElement> Image => _image;

        public DocRef<MediaElement> Audio => _audio;

        public DocRefList<Exhibit> Exhibits => _exhibits;

        public DocRefList<Tag> Tags => _tags;
    }
}
