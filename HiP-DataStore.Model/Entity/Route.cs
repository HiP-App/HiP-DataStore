using MongoDB.Bson.Serialization.Attributes;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Entity
{
    public class Route : ContentBase
    {
        // TODO: What about waypoints?

        [BsonElement(nameof(Image))]
        private DocRef<MediaElement> _image = new DocRef<MediaElement>(ResourceType.Media.Name);

        [BsonElement(nameof(Audio))]
        private DocRef<MediaElement> _audio = new DocRef<MediaElement>(ResourceType.Media.Name);

        [BsonElement(nameof(Exhibits))]
        private DocRefList<Exhibit> _exhibits = new DocRefList<Exhibit>(ResourceType.Exhibit.Name);

        [BsonElement(nameof(Tags))]
        private DocRefList<Tag> _tags = new DocRefList<Tag>(ResourceType.Tag.Name);

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
