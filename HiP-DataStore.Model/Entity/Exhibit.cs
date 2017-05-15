using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Entity
{
    public class Exhibit : ContentBase
    {
        [BsonElement(nameof(Image))]
        private DocRef<MediaElement> _image = new DocRef<MediaElement>(ResourceType.Media.Name);

        [BsonElement(nameof(Pages))]
        private List<ExhibitPage> _pages = new List<ExhibitPage>();

        [BsonElement(nameof(Tags))]
        private DocRefList<Tag> _tags = new DocRefList<Tag>();

        public string Name { get; set; }

        public string Description { get; set; }

        public float Latitude { get; set; }

        public float Longitude { get; set; }

        public DocRef<MediaElement> Image => _image;

        public IList<ExhibitPage> Pages => _pages;

        public DocRefList<Tag> Tags => _tags;
    }
}
